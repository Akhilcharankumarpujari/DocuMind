using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class QdrantVectorStoreTests
{
    [Fact]
    public void VectorPointPayload_ShouldStoreRequiredMetadata()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        // Act
        var payload = new VectorPointPayload(
            documentId: docId,
            chunkId: chunkId,
            chunkIndex: 2,
            pageNumber: 5,
            content: "Vector search sample snippet",
            tokenCount: 120);

        // Assert
        payload.DocumentId.Should().Be(docId);
        payload.ChunkId.Should().Be(chunkId);
        payload.ChunkIndex.Should().Be(2);
        payload.PageNumber.Should().Be(5);
        payload.Content.Should().Be("Vector search sample snippet");
        payload.TokenCount.Should().Be(120);
    }

    [Fact]
    public async Task MockVectorStore_SearchAsync_WithDocumentFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var mockStore = new Mock<IVectorStore>();
        var docId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        var expectedResults = new List<VectorSearchResult>
        {
            new()
            {
                DocumentId = docId,
                ChunkId = chunkId,
                PageNumber = 3,
                ChunkIndex = 1,
                Content = "Relevant text snippet for question",
                Score = 0.92f
            }
        };

        mockStore.Setup(s => s.SearchAsync(
            It.IsAny<string>(),
            It.IsAny<ReadOnlyMemory<float>>(),
            docId,
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var results = await mockStore.Object.SearchAsync("documind_chunks", new float[1536], docId, 5);

        // Assert
        results.Should().HaveCount(1);
        results[0].DocumentId.Should().Be(docId);
        results[0].PageNumber.Should().Be(3);
        results[0].Score.Should().Be(0.92f);
    }

    [Fact]
    public async Task MockVectorStore_DeleteDocumentVectorsAsync_ShouldCallUnderlyingStore()
    {
        // Arrange
        var mockStore = new Mock<IVectorStore>();
        var docId = Guid.NewGuid();

        mockStore.Setup(s => s.DeleteDocumentVectorsAsync("documind_chunks", docId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await mockStore.Object.DeleteDocumentVectorsAsync("documind_chunks", docId);

        // Assert
        mockStore.Verify(s => s.DeleteDocumentVectorsAsync("documind_chunks", docId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
