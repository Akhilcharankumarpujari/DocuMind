using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.Configuration;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class DocumentProcessingServiceTests
{
    private static DocuMindDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new DocuMindDbContext(options);
    }

    [Fact]
    public async Task ProcessDocumentAsync_SuccessfulExtractionAndIndexing_ShouldMarkDocumentReady()
    {
        // Arrange
        var dbName = "ProcTest_" + Guid.NewGuid();
        using var context = CreateInMemoryContext(dbName);

        var doc = new Document("AI_Research.pdf", 1024, "uploads/ai_research.pdf");
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        var mockExtractor = new Mock<IPdfTextExtractor>();
        mockExtractor.Setup(e => e.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfExtractionResult
            {
                Pages = new List<PdfPageText>
                {
                    new(1, "Page 1 intro"),
                    new(2, "Page 2 analysis")
                }
            });

        var mockChunker = new Mock<ITextChunker>();
        mockChunker.Setup(c => c.CreateChunks(It.IsAny<IReadOnlyList<PdfPageText>>(), It.IsAny<ChunkingOptions>()))
            .Returns(new List<TextChunk>
            {
                new(0, 1, "Page 1 intro", 5),
                new(1, 2, "Page 2 analysis", 5)
            });

        var mockEmbedding = new Mock<IEmbeddingService>();
        mockEmbedding.Setup(e => e.IsConfigured).Returns(true);
        mockEmbedding.Setup(e => e.Dimension).Returns(1536);
        mockEmbedding.Setup(e => e.GenerateEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadOnlyMemory<float>>
            {
                new float[1536],
                new float[1536]
            });

        var mockVectorStore = new Mock<IVectorStore>();

        var qdrantOptions = Options.Create(new QdrantOptions { CollectionName = "documind_chunks" });
        var chunkingOptions = Options.Create(new ChunkingOptions());

        var service = new DocumentProcessingService(
            context,
            mockExtractor.Object,
            mockChunker.Object,
            mockEmbedding.Object,
            mockVectorStore.Object,
            qdrantOptions,
            chunkingOptions,
            NullLogger<DocumentProcessingService>.Instance);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.ProcessDocumentAsync(doc.Id, stream);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExtractedPages.Should().Be(2);
        result.TotalChunks.Should().Be(2);

        var updatedDoc = await context.Documents.FindAsync(doc.Id);
        updatedDoc!.Status.Should().Be(DocumentStatus.Ready);
        updatedDoc.PageCount.Should().Be(2);

        var persistedChunks = await context.DocumentChunks.Where(c => c.DocumentId == doc.Id).ToListAsync();
        persistedChunks.Should().HaveCount(2);

        mockVectorStore.Verify(v => v.UpsertVectorsAsync(
            "documind_chunks",
            It.IsAny<IReadOnlyList<(Guid PointId, ReadOnlyMemory<float> Vector, VectorPointPayload Payload)>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenPdfHasNoExtractableText_ShouldMarkDocumentFailed()
    {
        // Arrange
        var dbName = "ProcTest_Empty_" + Guid.NewGuid();
        using var context = CreateInMemoryContext(dbName);

        var doc = new Document("Empty.pdf", 512, "uploads/empty.pdf");
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        var mockExtractor = new Mock<IPdfTextExtractor>();
        mockExtractor.Setup(e => e.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfExtractionResult
            {
                Pages = new List<PdfPageText>
                {
                    new(1, ""), // empty
                    new(2, "")  // empty
                }
            });

        var mockChunker = new Mock<ITextChunker>();
        var mockEmbedding = new Mock<IEmbeddingService>();
        var mockVectorStore = new Mock<IVectorStore>();

        var service = new DocumentProcessingService(
            context,
            mockExtractor.Object,
            mockChunker.Object,
            mockEmbedding.Object,
            mockVectorStore.Object,
            Options.Create(new QdrantOptions()),
            Options.Create(new ChunkingOptions()),
            NullLogger<DocumentProcessingService>.Instance);

        using var stream = new MemoryStream(new byte[] { 1, 2 });

        // Act
        var result = await service.ProcessDocumentAsync(doc.Id, stream);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no extractable text");

        var updatedDoc = await context.Documents.FindAsync(doc.Id);
        updatedDoc!.Status.Should().Be(DocumentStatus.Failed);
        updatedDoc.ErrorMessage.Should().Contain("no extractable text");
    }

    [Fact]
    public async Task ProcessDocumentAsync_Retry_ShouldReplaceExistingChunksIdempotently()
    {
        // Arrange
        var dbName = "ProcTest_Retry_" + Guid.NewGuid();
        using var context = CreateInMemoryContext(dbName);

        var doc = new Document("Retry.pdf", 1024, "uploads/retry.pdf");
        context.Documents.Add(doc);

        // Simulate previous failed attempt that left 1 chunk
        var oldChunk = new DocumentChunk(doc.Id, 1, 0, "Old stale chunk content", 10);
        context.DocumentChunks.Add(oldChunk);
        await context.SaveChangesAsync();

        var mockExtractor = new Mock<IPdfTextExtractor>();
        mockExtractor.Setup(e => e.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfExtractionResult
            {
                Pages = new List<PdfPageText> { new(1, "New fresh content") }
            });

        var mockChunker = new Mock<ITextChunker>();
        mockChunker.Setup(c => c.CreateChunks(It.IsAny<IReadOnlyList<PdfPageText>>(), It.IsAny<ChunkingOptions>()))
            .Returns(new List<TextChunk> { new(0, 1, "New fresh content", 5) });

        var mockEmbedding = new Mock<IEmbeddingService>();
        mockEmbedding.Setup(e => e.IsConfigured).Returns(false); // Unconfigured fallback

        var mockVectorStore = new Mock<IVectorStore>();

        var service = new DocumentProcessingService(
            context,
            mockExtractor.Object,
            mockChunker.Object,
            mockEmbedding.Object,
            mockVectorStore.Object,
            Options.Create(new QdrantOptions()),
            Options.Create(new ChunkingOptions()),
            NullLogger<DocumentProcessingService>.Instance);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.ProcessDocumentAsync(doc.Id, stream);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var chunks = await context.DocumentChunks.Where(c => c.DocumentId == doc.Id).ToListAsync();
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be("New fresh content");
        chunks[0].Id.Should().NotBe(oldChunk.Id);
    }
}
