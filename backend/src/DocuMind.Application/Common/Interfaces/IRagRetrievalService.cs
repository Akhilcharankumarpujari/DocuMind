using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Retrieves document-specific context chunks from the vector database for RAG answering,
/// enforcing user document ownership before any vector query is executed.
/// </summary>
public interface IRagRetrievalService
{
    Task<RagRetrievalResult> RetrieveContextAsync(
        Guid documentId,
        string question,
        Guid userId,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
