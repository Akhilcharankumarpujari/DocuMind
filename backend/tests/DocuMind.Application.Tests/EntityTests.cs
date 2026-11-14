using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DocuMind.Application.Tests;

public class EntityTests
{
    [Fact]
    public void Document_Creation_SetsExpectedPropertiesAndDefaults()
    {
        // Arrange
        const string fileName = "Architecture_Overview.pdf";
        const long size = 2 * 1024 * 1024; // 2 MB
        const string storagePath = "uploads/architecture.pdf";

        // Act
        var doc = new Document(fileName, size, storagePath);

        // Assert
        doc.Id.Should().NotBeEmpty();
        doc.FileName.Should().Be(fileName);
        doc.FileSizeBytes.Should().Be(size);
        doc.StoragePath.Should().Be(storagePath);
        doc.ContentType.Should().Be("application/pdf");
        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.PageCount.Should().Be(0);
        doc.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        doc.UpdatedAtUtc.Should().BeNull();
        doc.Chunks.Should().BeEmpty();
        doc.Conversations.Should().BeEmpty();
    }

    [Fact]
    public void Document_StateTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var doc = new Document("report.pdf", 1024, "uploads/report.pdf");

        // Act & Assert 1: Processing
        doc.MarkAsProcessing();
        doc.Status.Should().Be(DocumentStatus.Processing);
        doc.UpdatedAtUtc.Should().NotBeNull();

        // Act & Assert 2: Ready
        doc.MarkAsReady(15, "Executive summary of the report");
        doc.Status.Should().Be(DocumentStatus.Ready);
        doc.PageCount.Should().Be(15);
        doc.Summary.Should().Be("Executive summary of the report");

        // Act & Assert 3: Failed
        doc.MarkAsFailed("Corruption detected in PDF stream");
        doc.Status.Should().Be(DocumentStatus.Failed);
        doc.ErrorMessage.Should().Be("Corruption detected in PDF stream");
    }

    [Fact]
    public void Document_Validation_ShouldEnforceConstraints()
    {
        // Empty filename
        var actEmpty = () => new Document("", 100, "uploads/test.pdf");
        actEmpty.Should().Throw<ArgumentException>();

        // Zero or negative size
        var actZeroSize = () => new Document("test.pdf", 0, "uploads/test.pdf");
        actZeroSize.Should().Throw<ArgumentOutOfRangeException>();

        // Size > 20 MB
        var actTooLarge = () => new Document("large.pdf", 25 * 1024 * 1024, "uploads/large.pdf");
        actTooLarge.Should().Throw<ArgumentOutOfRangeException>();

        // Negative page count in MarkAsReady
        var doc = new Document("test.pdf", 100, "uploads/test.pdf");
        var actNegativePages = () => doc.MarkAsReady(-1);
        actNegativePages.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DocumentChunk_PreservesPageNumberAndChunkIndex()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var qdrantId = Guid.NewGuid();

        // Act
        var chunk = new DocumentChunk(docId, pageNumber: 4, chunkIndex: 12, content: "Neural architecture search snippet", tokenCount: 65, qdrantPointId: qdrantId);

        // Assert
        chunk.Id.Should().NotBeEmpty();
        chunk.DocumentId.Should().Be(docId);
        chunk.PageNumber.Should().Be(4);
        chunk.ChunkIndex.Should().Be(12);
        chunk.Content.Should().Be("Neural architecture search snippet");
        chunk.TokenCount.Should().Be(65);
        chunk.QdrantPointId.Should().Be(qdrantId);
    }

    [Fact]
    public void DocumentChunk_InvalidInputs_ShouldThrow()
    {
        var docId = Guid.NewGuid();

        // Invalid page (< 1)
        var actPage = () => new DocumentChunk(docId, 0, 0, "Valid content");
        actPage.Should().Throw<ArgumentOutOfRangeException>();

        // Invalid chunk index (< 0)
        var actIndex = () => new DocumentChunk(docId, 1, -1, "Valid content");
        actIndex.Should().Throw<ArgumentOutOfRangeException>();

        // Empty content
        var actContent = () => new DocumentChunk(docId, 1, 0, "");
        actContent.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChatConversation_And_ChatMessage_Relationships_ShouldBeConsistent()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var conv = new ChatConversation(docId, "Discussion about Methodology");

        // Act
        var userMsg = conv.AddMessage(MessageRole.User, "What dataset was used?");
        var assistantMsg = conv.AddMessage(
            MessageRole.Assistant,
            "The authors evaluated on ImageNet and COCO datasets.",
            "[{\"page\":5,\"snippet\":\"Dataset overview\",\"score\":0.91}]");

        // Assert
        conv.DocumentId.Should().Be(docId);
        conv.Title.Should().Be("Discussion about Methodology");
        conv.Messages.Should().HaveCount(2);

        userMsg.Role.Should().Be(MessageRole.User);
        userMsg.Content.Should().Be("What dataset was used?");
        userMsg.ConversationId.Should().Be(conv.Id);

        assistantMsg.Role.Should().Be(MessageRole.Assistant);
        assistantMsg.SourcesJson.Should().Contain("\"page\":5");
        assistantMsg.ConversationId.Should().Be(conv.Id);
    }

    [Theory]
    [InlineData(DocumentStatus.Pending, 0)]
    [InlineData(DocumentStatus.Processing, 1)]
    [InlineData(DocumentStatus.Ready, 2)]
    [InlineData(DocumentStatus.Failed, 3)]
    public void DocumentStatus_EnumValues_ShouldMatchSpecification(DocumentStatus status, int expectedValue)
    {
        ((int)status).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(MessageRole.User, 0)]
    [InlineData(MessageRole.Assistant, 1)]
    [InlineData(MessageRole.System, 2)]
    public void MessageRole_EnumValues_ShouldMatchSpecification(MessageRole role, int expectedValue)
    {
        ((int)role).Should().Be(expectedValue);
    }
}
