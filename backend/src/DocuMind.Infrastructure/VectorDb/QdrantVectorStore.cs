using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace DocuMind.Infrastructure.VectorDb;

/// <summary>
/// Qdrant vector database implementation managing collections, point upserts, and filtered vector searches.
/// </summary>
public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(IOptions<QdrantOptions> options, ILogger<QdrantVectorStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint) ? "http://localhost:6333" : _options.Endpoint;
        var uri = new Uri(endpoint);
        var grpcPort = uri.Port > 0 ? (uri.Port == 6333 ? 6334 : uri.Port) : 6334;

        _client = new QdrantClient(
            host: uri.Host,
            port: grpcPort,
            https: uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase),
            apiKey: string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey);

        _logger.LogInformation("QdrantClient initialized for endpoint {Endpoint}", endpoint);
    }

    public async Task EnsureCollectionAsync(string collectionName, ulong vectorSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            throw new ArgumentException("Collection name cannot be empty.", nameof(collectionName));

        try
        {
            var exists = await _client.CollectionExistsAsync(collectionName, cancellationToken);
            if (!exists)
            {
                _logger.LogInformation("Creating Qdrant collection '{Collection}' with vector dimension {Dimension}", collectionName, vectorSize);

                await _client.CreateCollectionAsync(
                    collectionName,
                    new VectorParams
                    {
                        Size = vectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                // Create a payload keyword index on documentId to accelerate filtered searches
                await _client.CreatePayloadIndexAsync(
                    collectionName,
                    fieldName: "documentId",
                    schemaType: PayloadSchemaType.Keyword,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Qdrant collection '{Collection}' created successfully.", collectionName);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to ensure Qdrant collection '{Collection}'.", collectionName);
            throw;
        }
    }

    public async Task UpsertVectorsAsync(
        string collectionName,
        IReadOnlyList<(Guid PointId, ReadOnlyMemory<float> Vector, VectorPointPayload Payload)> points,
        CancellationToken cancellationToken = default)
    {
        if (points == null || points.Count == 0)
            return;

        try
        {
            var pointStructs = points.Select(p => new PointStruct
            {
                Id = new PointId { Uuid = p.PointId.ToString() },
                Vectors = p.Vector.ToArray(),
                Payload =
                {
                    ["documentId"] = p.Payload.DocumentId.ToString(),
                    ["chunkId"] = p.Payload.ChunkId.ToString(),
                    ["chunkIndex"] = p.Payload.ChunkIndex,
                    ["pageNumber"] = p.Payload.PageNumber,
                    ["content"] = p.Payload.Content,
                    ["tokenCount"] = p.Payload.TokenCount
                }
            }).ToList();

            await _client.UpsertAsync(collectionName, pointStructs, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully upserted {Count} points into Qdrant collection '{Collection}'", points.Count, collectionName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to upsert {Count} points into Qdrant collection '{Collection}'.", points.Count, collectionName);
            throw;
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        ReadOnlyMemory<float> queryVector,
        Guid? documentIdFilter = null,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Filter? filter = null;
            if (documentIdFilter.HasValue)
            {
                filter = Conditions.MatchKeyword("documentId", documentIdFilter.Value.ToString());
            }

            var scoredPoints = await _client.QueryAsync(
                collectionName,
                query: queryVector.ToArray(),
                filter: filter,
                limit: (ulong)Math.Max(1, limit),
                cancellationToken: cancellationToken);

            var results = new List<VectorSearchResult>();

            foreach (var point in scoredPoints)
            {
                var payload = point.Payload;

                var chunkId = payload.TryGetValue("chunkId", out var chunkIdVal) && Guid.TryParse(chunkIdVal.StringValue, out var cId)
                    ? cId
                    : (point.Id.HasUuid && Guid.TryParse(point.Id.Uuid, out var idGuid) ? idGuid : Guid.Empty);

                var docId = payload.TryGetValue("documentId", out var docIdVal) && Guid.TryParse(docIdVal.StringValue, out var dId)
                    ? dId
                    : (documentIdFilter ?? Guid.Empty);

                var pageNumber = payload.TryGetValue("pageNumber", out var pageVal) ? (int)pageVal.IntegerValue : 1;
                var chunkIndex = payload.TryGetValue("chunkIndex", out var indexVal) ? (int)indexVal.IntegerValue : 0;
                var content = payload.TryGetValue("content", out var contentVal) ? contentVal.StringValue : string.Empty;

                results.Add(new VectorSearchResult
                {
                    ChunkId = chunkId,
                    DocumentId = docId,
                    PageNumber = pageNumber,
                    ChunkIndex = chunkIndex,
                    Content = content,
                    Score = point.Score
                });
            }

            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred during similarity search in Qdrant collection '{Collection}'.", collectionName);
            throw;
        }
    }

    public async Task DeleteDocumentVectorsAsync(string collectionName, Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Conditions.MatchKeyword("documentId", documentId.ToString());
            await _client.DeleteAsync(collectionName, filter, cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted vectors for document '{DocumentId}' from Qdrant collection '{Collection}'.", documentId, collectionName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to delete vectors for document '{DocumentId}' in Qdrant collection '{Collection}'.", documentId, collectionName);
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _client.ListCollectionsAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Qdrant health check probe failed.");
            return false;
        }
    }
}
