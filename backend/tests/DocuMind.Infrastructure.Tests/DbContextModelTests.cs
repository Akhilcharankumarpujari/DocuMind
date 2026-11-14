using DocuMind.Application.Common.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class DbContextModelTests
{
    private static DocuMindDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseMySql("Server=localhost;Port=3308;Database=test_schema;Uid=root;Pwd=root;",
                      new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new DocuMindDbContext(options);
    }

    [Fact]
    public void Model_ShouldContainExpectedEntitiesAndTables()
    {
        // Arrange
        using var context = CreateTestContext();
        var model = context.Model;

        // Assert
        var docEntity = model.FindEntityType(typeof(Document));
        docEntity.Should().NotBeNull();
        docEntity!.GetTableName().Should().Be("documents");

        var chunkEntity = model.FindEntityType(typeof(DocumentChunk));
        chunkEntity.Should().NotBeNull();
        chunkEntity!.GetTableName().Should().Be("document_chunks");

        var convEntity = model.FindEntityType(typeof(ChatConversation));
        convEntity.Should().NotBeNull();
        convEntity!.GetTableName().Should().Be("chat_conversations");

        var msgEntity = model.FindEntityType(typeof(ChatMessage));
        msgEntity.Should().NotBeNull();
        msgEntity!.GetTableName().Should().Be("chat_messages");
    }

    [Fact]
    public void Model_ShouldConfigurePrimaryKeysProperly()
    {
        // Arrange
        using var context = CreateTestContext();
        var model = context.Model;

        // Assert
        model.FindEntityType(typeof(Document))!.FindPrimaryKey()!.Properties.Select(p => p.Name).Should().Contain("Id");
        model.FindEntityType(typeof(DocumentChunk))!.FindPrimaryKey()!.Properties.Select(p => p.Name).Should().Contain("Id");
        model.FindEntityType(typeof(ChatConversation))!.FindPrimaryKey()!.Properties.Select(p => p.Name).Should().Contain("Id");
        model.FindEntityType(typeof(ChatMessage))!.FindPrimaryKey()!.Properties.Select(p => p.Name).Should().Contain("Id");
    }

    [Fact]
    public void Model_ShouldConfigureForeignKeys_WithCascadeDelete()
    {
        // Arrange
        using var context = CreateTestContext();
        var model = context.Model;

        // Assert Document -> DocumentChunk
        var chunkFk = model.FindEntityType(typeof(DocumentChunk))!
            .GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Document));
        chunkFk.Should().NotBeNull();
        chunkFk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        // Assert Document -> ChatConversation
        var convFk = model.FindEntityType(typeof(ChatConversation))!
            .GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Document));
        convFk.Should().NotBeNull();
        convFk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        // Assert ChatConversation -> ChatMessage
        var msgFk = model.FindEntityType(typeof(ChatMessage))!
            .GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ChatConversation));
        msgFk.Should().NotBeNull();
        msgFk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_ShouldConfigureRequiredIndexes()
    {
        // Arrange
        using var context = CreateTestContext();
        var model = context.Model;

        // Document Indexes
        var docIndexes = model.FindEntityType(typeof(Document))!.GetIndexes();
        docIndexes.Should().Contain(idx => idx.Properties.Any(p => p.Name == "FileName"));
        docIndexes.Should().Contain(idx => idx.Properties.Any(p => p.Name == "Status"));
        docIndexes.Should().Contain(idx => idx.Properties.Any(p => p.Name == "CreatedAtUtc"));

        // DocumentChunk Composite Indexes
        var chunkIndexes = model.FindEntityType(typeof(DocumentChunk))!.GetIndexes();
        var uniqueSequenceIdx = chunkIndexes.FirstOrDefault(idx => idx.IsUnique &&
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "DocumentId", "ChunkIndex" }));
        uniqueSequenceIdx.Should().NotBeNull();

        var pageIdx = chunkIndexes.FirstOrDefault(idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "DocumentId", "PageNumber" }));
        pageIdx.Should().NotBeNull();

        // ChatConversation Composite Index
        var convIndexes = model.FindEntityType(typeof(ChatConversation))!.GetIndexes();
        var convDateIdx = convIndexes.FirstOrDefault(idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "DocumentId", "CreatedAtUtc" }));
        convDateIdx.Should().NotBeNull();

        // ChatMessage Composite Index
        var msgIndexes = model.FindEntityType(typeof(ChatMessage))!.GetIndexes();
        var msgDateIdx = msgIndexes.FirstOrDefault(idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "ConversationId", "CreatedAtUtc" }));
        msgDateIdx.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldAutomaticallySetTimestamps()
    {
        // Arrange
        var fixedTime = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
        var mockTimeProvider = new TestDateTimeProvider(fixedTime);

        var options = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseInMemoryDatabase("DocuMind_Timestamp_Test_Db")
            .Options;

        using var context = new DocuMindDbContext(options, mockTimeProvider);

        var doc = new Document
        {
            FileName = "Paper.pdf",
            FileSizeBytes = 1000,
            StoragePath = "uploads/paper.pdf",
            ContentType = "application/pdf"
        };

        // Act
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        // Assert
        doc.CreatedAtUtc.Should().Be(fixedTime);
        doc.UpdatedAtUtc.Should().BeNull();

        // Act: update
        var updatedTime = fixedTime.AddMinutes(10);
        mockTimeProvider.CurrentTime = updatedTime;
        doc.MarkAsReady(10, "Summary text");
        await context.SaveChangesAsync();

        // Assert update timestamp
        doc.UpdatedAtUtc.Should().Be(updatedTime);
    }

    private class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTime CurrentTime { get; set; }
        public DateTime UtcNow => CurrentTime;

        public TestDateTimeProvider(DateTime initialTime)
        {
            CurrentTime = initialTime;
        }
    }
}
