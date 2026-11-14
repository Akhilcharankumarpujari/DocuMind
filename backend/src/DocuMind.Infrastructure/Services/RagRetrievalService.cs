using System.Text;
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
/// Retrieves document-specific context chunks via semantic vector search in Qdrant.
/// </summary>
public class RagRetrievalService : IRagRetrievalService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IApplicationDbContext _dbContext;
    private readonly QdrantOptions _qdrantOptions;
    private readonly ILogger<RagRetrievalService> _logger;

    public RagRetrievalService(
        IEmbeddingService _embeddingService,
        IVectorStore _vectorStore,
        IApplicationDbContext dbContext,
        IOptions<QdrantOptions> qdrantOptions,
        ILogger<RagRetrievalService> logger)
    {
        this._embeddingService = _embeddingService;
        this._vectorStore = _vectorStore;
        _dbContext = dbContext;
        _qdrantOptions = qdrantOptions.Value;
        _logger = logger;
    }

    public async Task<RagRetrievalResult> RetrieveContextAsync(
        Guid documentId,
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question cannot be empty.", nameof(question));

        // 1. Verify document exists in MySQL
        var document = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document == null)
            throw new EntityNotFoundException(nameof(Document), documentId);

        _logger.LogInformation("Initiating RAG retrieval for Document {DocumentId} ('{FileName}'). Question: '{Question}', TopK: {TopK}",
            documentId, document.FileName, question, topK);

        // 2. Generate embedding for user question
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question, cancellationToken);

        // 3. Search Qdrant with documentId filter
        var collectionName = string.IsNullOrWhiteSpace(_qdrantOptions.CollectionName) ? "documind-chunks" : _qdrantOptions.CollectionName;

        var searchResults = await _vectorStore.SearchAsync(
            collectionName: collectionName,
            queryVector: questionEmbedding,
            documentIdFilter: documentId,
            limit: Math.Max(1, topK),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Qdrant returned {Count} chunks for Document {DocumentId}", searchResults.Count, documentId);

        if (searchResults.Count == 0)
        {
            _logger.LogWarning("No chunks matched Document {DocumentId} for query '{Question}'", documentId, question);
            return new RagRetrievalResult(documentId, question, Array.Empty<SourceCitation>(), string.Empty);
        }

        // 4. Reconstruct source citations and build grounded context
        var citations = new List<SourceCitation>(searchResults.Count);
        var contextBuilder = new StringBuilder();

        foreach (var hit in searchResults)
        {
            // Truncate snippet for citation display if long
            var snippet = hit.Content.Length > 200
                ? hit.Content[..200] + "..."
                : hit.Content;

            citations.Add(new SourceCitation(
                page: hit.PageNumber,
                snippet: snippet.Trim(),
                score: Math.Round(hit.Score, 4),
                documentId: documentId,
                chunkId: hit.ChunkId));

            contextBuilder.AppendLine($"--- SOURCE CHUNK [Page {hit.PageNumber}] (Relevance Score: {hit.Score:F3}) ---");
            contextBuilder.AppendLine(hit.Content.Trim());
            contextBuilder.AppendLine();
        }

        return new RagRetrievalResult(
            documentId,
            question,
            citations,
            contextBuilder.ToString());
    }
}
