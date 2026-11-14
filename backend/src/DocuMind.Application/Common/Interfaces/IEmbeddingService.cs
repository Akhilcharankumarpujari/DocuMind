namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Generates dense vector representations for texts using Azure OpenAI embedding models.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// The expected vector dimensionality produced by the configured embedding model.
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// Indicates whether the embedding credentials and deployment are configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Generates an embedding for a single text.
    /// </summary>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for a batch of texts.
    /// </summary>
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
