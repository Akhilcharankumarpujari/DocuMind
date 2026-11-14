using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Exceptions;
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

public class RagRetrievalServiceTests
{
    private readonly DocuMindDbContext _dbContext;
    private readonly Mock<IEmbeddingService> _embeddingMock;
    private readonly Mock<IVectorStore> _vectorStoreMock;
    private readonly IOptions<QdrantOptions> _qdrantOptions;

    public RagRetrievalServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseInMemoryDatabase(databaseName: $"DocuMind_RagTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new DocuMindDbContext(dbOptions);
        _embeddingMock = new Mock<IEmbeddingService>();
        _vectorStoreMock = new Mock<IVectorStore>();
        _qdrantOptions = Options.Create(new QdrantOptions { CollectionName = "documind-chunks" });

        _embeddingMock
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);
    }

    [Fact]
    public async Task RetrieveContextAsync_ValidQuestion_ShouldReturnFilteredCitationsAndContext()
    {
        // Arrange
        var doc = new Document("TestDoc.pdf", 1024, "C:\\test\\test.pdf");
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        var searchHits = new List<VectorSearchResult>
        {
            new()
            {
                DocumentId = doc.Id,
                ChunkId = Guid.NewGuid(),
                ChunkIndex = 0,
                PageNumber = 1,
                Content = "DocuMind uses Qdrant for semantic search and PdfPig for extraction.",
                Score = 0.892f
            },
            new()
            {
                DocumentId = doc.Id,
                ChunkId = Guid.NewGuid(),
                ChunkIndex = 1,
                PageNumber = 2,
                Content = "Grounded responses include original PDF page citations.",
                Score = 0.845f
            }
        };

        _vectorStoreMock
            .Setup(v => v.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<float>>(),
                doc.Id,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchHits);

        var service = new RagRetrievalService(
            _embeddingMock.Object,
            _vectorStoreMock.Object,
            _dbContext,
            _qdrantOptions,
            NullLogger<RagRetrievalService>.Instance);

        // Act
        var result = await service.RetrieveContextAsync(doc.Id, "How does DocuMind work?", topK: 5);

        // Assert
        result.Should().NotBeNull();
        result.DocumentId.Should().Be(doc.Id);
        result.HasContext.Should().BeTrue();
        result.Citations.Should().HaveCount(2);

        result.Citations[0].Page.Should().Be(1);
        result.Citations[0].Snippet.Should().Contain("DocuMind uses Qdrant");
        result.Citations[0].Score.Should().Be(0.892);

        result.Citations[1].Page.Should().Be(2);
        result.Citations[1].Snippet.Should().Contain("Grounded responses include");

        result.ContextText.Should().Contain("[Page 1]");
        result.ContextText.Should().Contain("[Page 2]");
    }

    [Fact]
    public async Task RetrieveContextAsync_EmptySearchResults_ShouldReturnEmptyContext()
    {
        // Arrange
        var doc = new Document("Empty.pdf", 512, "C:\\test\\empty.pdf");
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        _vectorStoreMock
            .Setup(v => v.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<float>>(),
                doc.Id,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        var service = new RagRetrievalService(
            _embeddingMock.Object,
            _vectorStoreMock.Object,
            _dbContext,
            _qdrantOptions,
            NullLogger<RagRetrievalService>.Instance);

        // Act
        var result = await service.RetrieveContextAsync(doc.Id, "Unrelated question", topK: 5);

        // Assert
        result.Should().NotBeNull();
        result.HasContext.Should().BeFalse();
        result.Citations.Should().BeEmpty();
        result.ContextText.Should().BeEmpty();
    }

    [Fact]
    public async Task RetrieveContextAsync_NonExistentDocument_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var service = new RagRetrievalService(
            _embeddingMock.Object,
            _vectorStoreMock.Object,
            _dbContext,
            _qdrantOptions,
            NullLogger<RagRetrievalService>.Instance);

        // Act
        var act = async () => await service.RetrieveContextAsync(missingId, "Hello");

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task RetrieveContextAsync_EmptyQuestion_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new RagRetrievalService(
            _embeddingMock.Object,
            _vectorStoreMock.Object,
            _dbContext,
            _qdrantOptions,
            NullLogger<RagRetrievalService>.Instance);

        // Act
        var act = async () => await service.RetrieveContextAsync(Guid.NewGuid(), "   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
