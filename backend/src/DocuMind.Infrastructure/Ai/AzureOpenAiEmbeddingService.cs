using System.ClientModel;
using Azure.AI.OpenAI;
using DocuMind.Application.Common.Interfaces;
using DocuMind.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace DocuMind.Infrastructure.Ai;

/// <summary>
/// Generates text embeddings using Azure OpenAI / Azure AI Foundry embedding models.
/// </summary>
public class AzureOpenAiEmbeddingService : IEmbeddingService
{
    private readonly AzureOpenAiOptions _options;
    private readonly ILogger<AzureOpenAiEmbeddingService> _logger;
    private readonly EmbeddingClient? _embeddingClient;

    public int Dimension => _options.EmbeddingDimensions;
    public bool IsConfigured => _options.IsConfigured;

    public AzureOpenAiEmbeddingService(IOptions<AzureOpenAiOptions> options, ILogger<AzureOpenAiEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.IsConfigured)
        {
            try
            {
                var azureClient = new AzureOpenAIClient(
                    new Uri(_options.Endpoint),
                    new ApiKeyCredential(_options.ApiKey));

                _embeddingClient = azureClient.GetEmbeddingClient(_options.EmbeddingDeploymentName);
                _logger.LogInformation("Azure OpenAI EmbeddingClient initialized for deployment '{Deployment}'", _options.EmbeddingDeploymentName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize AzureOpenAIClient with provided endpoint '{Endpoint}'", _options.Endpoint);
                _embeddingClient = null;
            }
        }
        else
        {
            _logger.LogInformation("Azure OpenAI embedding credentials not fully configured. Real embeddings will be disabled until deployment is created.");
        }
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty for embedding generation.", nameof(text));

        EnsureConfigured();

        try
        {
            var response = await _embeddingClient!.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            var vector = response.Value.ToFloats();

            if (_options.EmbeddingDimensions > 0 && vector.Length != _options.EmbeddingDimensions)
            {
                _logger.LogWarning(
                    "Generated embedding dimension {ActualDim} does not match configured dimension {ConfiguredDim}",
                    vector.Length,
                    _options.EmbeddingDimensions);
            }

            return vector;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while generating embedding for text.");
            throw;
        }
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
            return Array.Empty<ReadOnlyMemory<float>>();

        EnsureConfigured();

        try
        {
            var response = await _embeddingClient!.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);
            var result = new List<ReadOnlyMemory<float>>(response.Value.Count);

            foreach (var item in response.Value)
            {
                result.Add(item.ToFloats());
            }

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while generating batch embeddings for {Count} texts.", texts.Count);
            throw;
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured || _embeddingClient == null)
        {
            throw new InvalidOperationException(
                "Azure OpenAI embedding deployment is not configured or missing credentials. " +
                "Please configure AzureOpenAi:Endpoint, AzureOpenAi:ApiKey, and AzureOpenAi:EmbeddingDeploymentName.");
        }
    }
}
