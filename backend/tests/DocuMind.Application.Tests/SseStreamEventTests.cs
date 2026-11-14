using System.Text.Json;
using DocuMind.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace DocuMind.Application.Tests;

public class SseStreamEventTests
{
    [Fact]
    public void CreateToken_ShouldProduceValidTokenEvent()
    {
        // Act
        var tokenEvent = SseStreamEvent.CreateToken("Hello world");
        var sseMessage = tokenEvent.ToSseMessage();

        // Assert
        tokenEvent.Type.Should().Be("token");
        tokenEvent.Content.Should().Be("Hello world");
        sseMessage.Should().StartWith("data: ");
        sseMessage.Should().EndWith("\n\n");

        var json = sseMessage.Substring(6).Trim();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("type").GetString().Should().Be("token");
        doc.RootElement.GetProperty("content").GetString().Should().Be("Hello world");
    }

    [Fact]
    public void CreateSources_ShouldProduceValidSourcesEvent()
    {
        // Arrange
        var sources = new List<SourceCitation>
        {
            new() { Page = 2, Snippet = "DocuMind architecture overview", Score = 0.94 }
        };

        // Act
        var sseEvent = SseStreamEvent.CreateSources(sources);
        var sseMessage = sseEvent.ToSseMessage();

        // Assert
        sseEvent.Type.Should().Be("sources");
        sseEvent.Sources.Should().HaveCount(1);
        sseMessage.Should().Contain("\"page\":2");
        sseMessage.Should().Contain("\"score\":0.94");
    }

    [Fact]
    public void CreateDone_ShouldProduceDoneEvent()
    {
        // Act
        var doneEvent = SseStreamEvent.CreateDone();
        var sseMessage = doneEvent.ToSseMessage();

        // Assert
        doneEvent.Type.Should().Be("done");
        sseMessage.Should().Contain("\"type\":\"done\"");
    }
}
