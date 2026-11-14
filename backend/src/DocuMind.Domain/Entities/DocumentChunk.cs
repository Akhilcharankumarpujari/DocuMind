using DocuMind.Domain.Common;

namespace DocuMind.Domain.Entities;

/// <summary>
/// Represents a segmented text unit from an extracted document page, indexed for vector similarity search.
/// </summary>
public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    /// <summary>
    /// 1-based page number where this chunk originated in the source PDF.
    /// Preserved for precise RAG source citations in the chat UI.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 0-based sequential ordering of the chunk across the document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Raw text content of the chunk.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Approximate token count computed during chunking.
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Unique point identifier assigned when indexed into the Qdrant vector collection.
    /// </summary>
    public Guid? QdrantPointId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DocumentChunk()
    {
    }

    public DocumentChunk(Guid documentId, int pageNumber, int chunkIndex, string content, int tokenCount = 0, Guid? qdrantPointId = null)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be 1 or greater.");

        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), "Chunk index cannot be negative.");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Chunk content cannot be empty.", nameof(content));

        DocumentId = documentId;
        PageNumber = pageNumber;
        ChunkIndex = chunkIndex;
        Content = content;
        TokenCount = tokenCount;
        QdrantPointId = qdrantPointId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
