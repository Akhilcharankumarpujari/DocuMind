using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Exceptions;
using DocuMind.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Services;

/// <summary>
/// Orchestrates end-to-end document text extraction, intelligent chunking, vector embedding generation,
/// Qdrant indexing, and MySQL relational persistence.
/// </summary>
public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IApplicationDbContext _context;
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly QdrantOptions _qdrantOptions;
    private readonly ChunkingOptions _chunkingOptions;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        IApplicationDbContext context,
        IPdfTextExtractor pdfExtractor,
        ITextChunker chunker,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IOptions<QdrantOptions> qdrantOptions,
        IOptions<ChunkingOptions> chunkingOptions,
        ILogger<DocumentProcessingService> logger)
    {
        _context = context;
        _pdfExtractor = pdfExtractor;
        _chunker = chunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _qdrantOptions = qdrantOptions.Value;
        _chunkingOptions = chunkingOptions.Value;
        _logger = logger;
    }

    public async Task<DocumentProcessingResult> ProcessDocumentAsync(
        Guid documentId,
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        _logger.LogInformation("Starting ingestion processing for document '{DocumentId}'", documentId);

        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document == null)
        {
            throw new EntityNotFoundException(nameof(Document), documentId);
        }

        try
        {
            // 1. Transition state to Processing
            document.MarkAsProcessing();
            await _context.SaveChangesAsync(cancellationToken);

            // 2. Extract Text Page-by-Page
            _logger.LogDebug("Extracting text from PDF stream for document '{DocumentId}'", documentId);
            var extraction = await _pdfExtractor.ExtractTextAsync(pdfStream, cancellationToken);

            if (!extraction.HasExtractableText)
            {
                const string emptyDocError = "The PDF file contains no extractable text.";
                _logger.LogWarning("Document '{DocumentId}' extraction yielded no text.", documentId);
                document.MarkAsFailed(emptyDocError);
                await _context.SaveChangesAsync(cancellationToken);
                return DocumentProcessingResult.Failure(documentId, emptyDocError);
            }

            // 3. Chunk text with page attribution
            _logger.LogDebug("Chunking text for document '{DocumentId}' across {PageCount} pages", documentId, extraction.PageCount);
            var chunks = _chunker.CreateChunks(extraction.Pages, _chunkingOptions);

            if (chunks.Count == 0)
            {
                const string noChunksError = "Failed to produce text segments from document.";
                document.MarkAsFailed(noChunksError);
                await _context.SaveChangesAsync(cancellationToken);
                return DocumentProcessingResult.Failure(documentId, noChunksError);
            }

            // 4. Generate Embeddings & Index in Qdrant (if embedding provider configured)
            var pointsToUpsert = new List<(Guid PointId, ReadOnlyMemory<float> Vector, VectorPointPayload Payload)>();
            var totalTokens = 0;

            // Map domain entities
            var domainChunks = new List<DocumentChunk>(chunks.Count);

            if (_embeddingService.IsConfigured)
            {
                _logger.LogInformation("Generating embeddings for {Count} chunks using Azure OpenAI", chunks.Count);

                await _vectorStore.EnsureCollectionAsync(_qdrantOptions.CollectionName, (ulong)_embeddingService.Dimension, cancellationToken);
                await _vectorStore.DeleteDocumentVectorsAsync(_qdrantOptions.CollectionName, documentId, cancellationToken);

                var chunkTexts = chunks.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);

                for (var i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    var embedding = embeddings[i];
                    var pointId = Guid.NewGuid();

                    var payload = new VectorPointPayload(
                        documentId: documentId,
                        chunkId: pointId,
                        chunkIndex: chunk.ChunkIndex,
                        pageNumber: chunk.PageNumber,
                        content: chunk.Content,
                        tokenCount: chunk.TokenCount);

                    pointsToUpsert.Add((pointId, embedding, payload));

                    var entityChunk = new DocumentChunk(
                        documentId: documentId,
                        pageNumber: chunk.PageNumber,
                        chunkIndex: chunk.ChunkIndex,
                        content: chunk.Content,
                        tokenCount: chunk.TokenCount,
                        qdrantPointId: pointId);

                    entityChunk.Id = pointId;
                    domainChunks.Add(entityChunk);
                    totalTokens += chunk.TokenCount;
                }

                await _vectorStore.UpsertVectorsAsync(_qdrantOptions.CollectionName, pointsToUpsert, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Embedding service is not configured. Chunks will be persisted in database without Qdrant vectors.");

                foreach (var chunk in chunks)
                {
                    var chunkId = Guid.NewGuid();
                    var entityChunk = new DocumentChunk(
                        documentId: documentId,
                        pageNumber: chunk.PageNumber,
                        chunkIndex: chunk.ChunkIndex,
                        content: chunk.Content,
                        tokenCount: chunk.TokenCount,
                        qdrantPointId: null);

                    entityChunk.Id = chunkId;
                    domainChunks.Add(entityChunk);
                    totalTokens += chunk.TokenCount;
                }
            }

            // 5. Persist Chunks in MySQL (Idempotent: replace any prior chunks for this document)
            var existingChunks = await _context.DocumentChunks.Where(c => c.DocumentId == documentId).ToListAsync(cancellationToken);
            if (existingChunks.Count > 0)
            {
                _context.DocumentChunks.RemoveRange(existingChunks);
            }

            await _context.DocumentChunks.AddRangeAsync(domainChunks, cancellationToken);

            // 6. Transition state to Ready
            document.MarkAsReady(extraction.PageCount);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Document '{DocumentId}' successfully processed: {Pages} pages, {Chunks} chunks, {Tokens} tokens.",
                documentId,
                extraction.PageCount,
                domainChunks.Count,
                totalTokens);

            return DocumentProcessingResult.Success(documentId, extraction.PageCount, domainChunks.Count, totalTokens);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Document processing failed for document '{DocumentId}'", documentId);
            document.MarkAsFailed(ex.Message);

            try
            {
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to persist document Failed state to database.");
            }

            return DocumentProcessingResult.Failure(documentId, ex.Message);
        }
    }
}
