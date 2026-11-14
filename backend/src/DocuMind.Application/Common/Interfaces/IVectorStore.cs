using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Vector store abstraction for Qdrant collection lifecycle, point upsert, and similarity search.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Creates or validates the collection with the required vector dimension and cosine distance metric.
    /// </summary>
    Task EnsureCollectionAsync(string collectionName, ulong vectorSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts points into the collection with vector payloads.
    /// </summary>
    Task UpsertVectorsAsync(
        string collectionName,
        IReadOnlyList<(Guid PointId, ReadOnlyMemory<float> Vector, VectorPointPayload Payload)> points,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for nearest neighbor vectors optionally filtered by documentId.
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        ReadOnlyMemory<float> queryVector,
        Guid? documentIdFilter = null,
        int limit = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all vector points associated with a specific documentId.
    /// </summary>
    Task DeleteDocumentVectorsAsync(string collectionName, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks vector database connectivity.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
