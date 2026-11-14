using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.Ai;
using DocuMind.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class AzureOpenAiChatServiceTests
{
    private readonly Mock<IRagRetrievalService> _ragMock;
    private readonly Mock<IConversationService> _convMock;

    public AzureOpenAiChatServiceTests()
    {
        _ragMock = new Mock<IRagRetrievalService>();
        _convMock = new Mock<IConversationService>();

        _convMock
            .Setup(c => c.GetOrCreateConversationAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatConversation(Guid.NewGuid(), "Test Chat"));

        _convMock
            .Setup(c => c.AddMessageAsync(It.IsAny<Guid>(), It.IsAny<MessageRole>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid convId, MessageRole role, string content, string? sources, CancellationToken ct) =>
                new ChatMessage(convId, role, content, sources));
    }

    [Fact]
    public async Task AskQuestionAsync_UnconfiguredService_ReturnsGracefulFallback()
    {
        // Arrange
        var unconfiguredOptions = Options.Create(new AzureOpenAiOptions
        {
            Endpoint = "",
            ApiKey = ""
        });

        _ragMock
            .Setup(r => r.RetrieveContextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagRetrievalResult(Guid.NewGuid(), "What is DocuMind?", Array.Empty<SourceCitation>(), ""));

        var service = new AzureOpenAiChatService(
            _ragMock.Object,
            _convMock.Object,
            unconfiguredOptions,
            NullLogger<AzureOpenAiChatService>.Instance);

        // Act
        var response = await service.AskQuestionAsync(Guid.NewGuid(), "What is DocuMind?");

        // Assert
        response.Should().NotBeNull();
        response.Answer.Should().Contain("Azure OpenAI chat service is not configured");
    }

    [Fact]
    public async Task StreamQuestionAsync_EmitsSourcesBeforeTokensAndDone()
    {
        // Arrange
        var unconfiguredOptions = Options.Create(new AzureOpenAiOptions
        {
            Endpoint = "",
            ApiKey = ""
        });

        var citations = new List<SourceCitation>
        {
            new(page: 1, snippet: "DocuMind is an AI document assistant", score: 0.95)
        };

        _ragMock
            .Setup(r => r.RetrieveContextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagRetrievalResult(Guid.NewGuid(), "Summarize", citations, "Context on page 1"));

        var service = new AzureOpenAiChatService(
            _ragMock.Object,
            _convMock.Object,
            unconfiguredOptions,
            NullLogger<AzureOpenAiChatService>.Instance);

        // Act
        var events = new List<SseStreamEvent>();
        await foreach (var sseEvent in service.StreamQuestionAsync(Guid.NewGuid(), "Summarize"))
        {
            events.Add(sseEvent);
        }

        // Assert
        events.Should().NotBeEmpty();

        // 1. First event must be sources
        events[0].Type.Should().Be("sources");
        events[0].Sources.Should().NotBeNull();
        events[0].Sources.Should().HaveCount(1);
        events[0].Sources![0].Page.Should().Be(1);

        // 2. Middle events are tokens
        events.Any(e => e.Type == "token").Should().BeTrue();

        // 3. Final event must be done
        events.Last().Type.Should().Be("done");
    }
}
