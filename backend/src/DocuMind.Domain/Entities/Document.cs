using DocuMind.Domain.Common;
using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

/// <summary>
/// Core aggregate root representing an ingested document in DocuMind.
/// </summary>
public class Document : BaseEntity, IAuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }
    public int PageCount { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? FileHash { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? Summary { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public ICollection<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();

    public Document()
    {
    }

    public Document(string fileName, long fileSizeBytes, string storagePath, string contentType = "application/pdf", string? fileHash = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        if (fileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be greater than zero.");

        if (fileSizeBytes > 20 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size cannot exceed 20 MB.");

        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty.", nameof(storagePath));

        FileName = fileName;
        FileSizeBytes = fileSizeBytes;
        StoragePath = storagePath;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType;
        FileHash = fileHash;
        Status = DocumentStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = DocumentStatus.Processing;
        ErrorMessage = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsReady(int pageCount, string? summary = null)
    {
        if (pageCount < 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount), "Page count cannot be negative.");

        PageCount = pageCount;
        Summary = summary;
        Status = DocumentStatus.Ready;
        ErrorMessage = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown processing failure." : errorMessage;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public DocumentChunk AddChunk(int pageNumber, int chunkIndex, string content, int tokenCount = 0, Guid? qdrantPointId = null)
    {
        var chunk = new DocumentChunk(Id, pageNumber, chunkIndex, content, tokenCount, qdrantPointId);
        Chunks.Add(chunk);
        UpdatedAtUtc = DateTime.UtcNow;
        return chunk;
    }
}
