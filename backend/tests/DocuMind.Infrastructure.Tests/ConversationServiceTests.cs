using System.Text.Json;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Domain.Exceptions;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class ConversationServiceTests
{
    private readonly DocuMindDbContext _dbContext;
    private readonly ConversationService _service;

    public ConversationServiceTests()
    {
        var options = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseInMemoryDatabase(databaseName: $"DocuMind_ConvTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new DocuMindDbContext(options);
        _service = new ConversationService(_dbContext, NullLogger<ConversationService>.Instance);
    }

    [Fact]
    public async Task GetOrCreateConversationAsync_NewThread_ShouldPersistAndAssociateWithDocument()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doc = new Document("Paper.pdf", 2048, "C:\\test\\paper.pdf", userId: userId);
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        // Act
        var conversation = await _service.GetOrCreateConversationAsync(doc.Id, conversationId: null, userId: userId);

        // Assert
        conversation.Should().NotBeNull();
        conversation.DocumentId.Should().Be(doc.Id);

        var persisted = await _dbContext.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversation.Id);
        persisted.Should().NotBeNull();
        persisted!.DocumentId.Should().Be(doc.Id);
    }

    [Fact]
    public async Task GetOrCreateConversationAsync_CrossDocumentAccess_ShouldThrowDomainException()
    {
        // Arrange: Conversation belongs to DocA, but requested under DocB
        var userId = Guid.NewGuid();
        var docA = new Document("DocA.pdf", 1024, "C:\\test\\a.pdf", userId: userId);
        var docB = new Document("DocB.pdf", 1024, "C:\\test\\b.pdf", userId: userId);
        _dbContext.Documents.AddRange(docA, docB);
        await _dbContext.SaveChangesAsync();

        var convA = new ChatConversation(docA.Id, "DocA Chat");
        _dbContext.ChatConversations.Add(convA);
        await _dbContext.SaveChangesAsync();

        // Act: Try accessing convA with docB.Id
        var act = async () => await _service.GetOrCreateConversationAsync(docB.Id, convA.Id, userId: userId);

        // Assert: Must reject cross-document conversation contamination
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*does not belong to the specified document*");
    }

    [Fact]
    public async Task AddMessageAsync_UserAndAssistantMessages_ShouldPreserveChronologicalOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doc = new Document("Report.pdf", 1024, "C:\\test\\report.pdf", userId: userId);
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        var conv = await _service.GetOrCreateConversationAsync(doc.Id, null, userId);

        // Act: Add user question and assistant answer
        var userMsg = await _service.AddMessageAsync(conv.Id, MessageRole.User, "What is the budget?");
        var sources = new List<SourceCitation> { new(page: 4, snippet: "Total budget is $5M", score: 0.92) };
        var sourcesJson = JsonSerializer.Serialize(sources, SseJsonContext.DefaultOptions);
        var assistantMsg = await _service.AddMessageAsync(conv.Id, MessageRole.Assistant, "The total budget is $5M [Page 4].", sourcesJson);

        // Assert
        var messages = await _service.GetMessagesAsync(conv.Id, userId);
        messages.Should().HaveCount(2);

        messages[0].Role.Should().Be("user");
        messages[0].Content.Should().Be("What is the budget?");

        messages[1].Role.Should().Be("assistant");
        messages[1].Content.Should().Be("The total budget is $5M [Page 4].");
        messages[1].Sources.Should().HaveCount(1);
        messages[1].Sources[0].Page.Should().Be(4);
    }
}
