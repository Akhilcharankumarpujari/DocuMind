namespace DocuMind.Application.Common.Models;

/// <summary>
/// Status report returned following the ingestion and vector indexing pipeline.
/// </summary>
public class DocumentProcessingResult
{
    public Guid DocumentId { get; set; }
    public bool IsSuccess { get; set; }
    public int ExtractedPages { get; set; }
    public int TotalChunks { get; set; }
    public int TotalTokens { get; set; }
    public string? ErrorMessage { get; set; }

    public static DocumentProcessingResult Success(Guid documentId, int pages, int chunks, int tokens) => new()
    {
        DocumentId = documentId,
        IsSuccess = true,
        ExtractedPages = pages,
        TotalChunks = chunks,
        TotalTokens = tokens
    };

    public static DocumentProcessingResult Failure(Guid documentId, string errorMessage) => new()
    {
        DocumentId = documentId,
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
