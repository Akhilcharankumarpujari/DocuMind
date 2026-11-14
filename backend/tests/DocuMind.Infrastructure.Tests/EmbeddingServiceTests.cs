using DocuMind.Application.Common.Interfaces;
using DocuMind.Infrastructure.Ai;
using DocuMind.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class EmbeddingServiceTests
{
    [Fact]
    public void AzureOpenAiOptions_IsConfigured_ShouldReturnFalse_WhenApiKeyOrEndpointMissing()
    {
        // Arrange
        var missingKey = new AzureOpenAiOptions
        {
            Endpoint = "https://my-resource.openai.azure.com/",
            ApiKey = "",
            EmbeddingDeploymentName = "text-embedding-3-small"
        };

        var missingEndpoint = new AzureOpenAiOptions
        {
            Endpoint = "",
            ApiKey = "sample-key",
            EmbeddingDeploymentName = "text-embedding-3-small"
        };

        var complete = new AzureOpenAiOptions
        {
            Endpoint = "https://my-resource.openai.azure.com/",
            ApiKey = "sample-key",
            EmbeddingDeploymentName = "text-embedding-3-small",
            EmbeddingDimensions = 1536
        };

        // Assert
        missingKey.IsConfigured.Should().BeFalse();
        missingEndpoint.IsConfigured.Should().BeFalse();
        complete.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var unconfiguredOptions = Options.Create(new AzureOpenAiOptions
        {
            Endpoint = "",
            ApiKey = "",
            EmbeddingDeploymentName = "text-embedding-3-small"
        });

        var service = new AzureOpenAiEmbeddingService(unconfiguredOptions, NullLogger<AzureOpenAiEmbeddingService>.Instance);

        // Act
        var act = async () => await service.GenerateEmbeddingAsync("Test input text");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new AzureOpenAiOptions
        {
            Endpoint = "https://my-resource.openai.azure.com/",
            ApiKey = "dummy-key",
            EmbeddingDeploymentName = "text-embedding-3-small"
        });

        var service = new AzureOpenAiEmbeddingService(options, NullLogger<AzureOpenAiEmbeddingService>.Instance);

        // Act
        var act = async () => await service.GenerateEmbeddingAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Text cannot be empty*");
    }

    [Fact]
    public async Task MockEmbeddingService_ShouldReturnExpectedDimensions()
    {
        // Arrange
        var mockEmbedding = new Mock<IEmbeddingService>();
        var sampleVector = new float[1536];
        Array.Fill(sampleVector, 0.05f);

        mockEmbedding.Setup(e => e.Dimension).Returns(1536);
        mockEmbedding.Setup(e => e.IsConfigured).Returns(true);
        mockEmbedding.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleVector);

        // Act
        var vector = await mockEmbedding.Object.GenerateEmbeddingAsync("Document analysis sample");

        // Assert
        mockEmbedding.Object.Dimension.Should().Be(1536);
        vector.Length.Should().Be(1536);
    }
}
