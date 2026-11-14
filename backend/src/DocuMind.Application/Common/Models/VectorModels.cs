namespace DocuMind.Application.Common.Models;

/// <summary>
/// Structured metadata payload attached to every point in Qdrant.
/// </summary>
public class VectorPointPayload
{
    public Guid DocumentId { get; set; }
    public Guid ChunkId { get; set; }
    public int ChunkIndex { get; set; }
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }

    public VectorPointPayload()
    {
    }

    public VectorPointPayload(Guid documentId, Guid chunkId, int chunkIndex, int pageNumber, string content, int tokenCount)
    {
        DocumentId = documentId;
        ChunkId = chunkId;
        ChunkIndex = chunkIndex;
        PageNumber = pageNumber;
        Content = content;
        TokenCount = tokenCount;
    }
}

/// <summary>
/// Result returned from a vector similarity search in Qdrant.
/// </summary>
public class VectorSearchResult
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public float Score { get; set; }
}
