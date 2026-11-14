namespace DocuMind.Application.Common.Models;

/// <summary>
/// Result from a document-scoped RAG retrieval operation.
/// </summary>
public class RagRetrievalResult
{
    public Guid DocumentId { get; set; }
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<SourceCitation> Citations { get; set; } = Array.Empty<SourceCitation>();
    public string ContextText { get; set; } = string.Empty;
    public int RetrievedChunkCount => Citations.Count;
    public bool HasContext => Citations.Count > 0 && !string.IsNullOrWhiteSpace(ContextText);

    public RagRetrievalResult()
    {
    }

    public RagRetrievalResult(Guid documentId, string query, IReadOnlyList<SourceCitation> citations, string contextText)
    {
        DocumentId = documentId;
        Query = query;
        Citations = citations ?? Array.Empty<SourceCitation>();
        ContextText = contextText ?? string.Empty;
    }
}
