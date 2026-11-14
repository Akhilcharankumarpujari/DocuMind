using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Retrieves document-specific context chunks from the vector database for RAG answering.
/// </summary>
public interface IRagRetrievalService
{
    Task<RagRetrievalResult> RetrieveContextAsync(
        Guid documentId,
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
