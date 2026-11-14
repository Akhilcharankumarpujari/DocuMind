using DocuMind.Infrastructure.Configuration;
using FluentAssertions;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class ConfigurationOptionsTests
{
    [Fact]
    public void QdrantOptions_Defaults_ShouldBeProperlyInitialized()
    {
        // Act
        var options = new QdrantOptions();

        // Assert
        options.Endpoint.Should().Be("http://localhost:6333");
        options.CollectionName.Should().Be("documind_chunks");
        options.VectorSize.Should().Be(1536);
    }

    [Fact]
    public void DatabaseOptions_Defaults_ShouldBeProperlyInitialized()
    {
        // Act
        var options = new DatabaseOptions();

        // Assert
        options.MaxRetryCount.Should().Be(3);
        options.CommandTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void AzureOpenAiOptions_Defaults_ShouldBeProperlyInitialized()
    {
        // Act
        var options = new AzureOpenAiOptions();

        // Assert
        options.ChatDeploymentName.Should().Be("gpt-4.1-mini");
        options.EmbeddingDeploymentName.Should().Be("text-embedding-3-small");
    }
}
