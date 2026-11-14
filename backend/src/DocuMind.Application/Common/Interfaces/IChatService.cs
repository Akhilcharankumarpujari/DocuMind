using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Orchestrates grounded RAG question-answering with Azure OpenAI and Server-Sent Events.
/// </summary>
public interface IChatService
{
    Task<AskQuestionResponse> AskQuestionAsync(
        Guid documentId,
        string question,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<SseStreamEvent> StreamQuestionAsync(
        Guid documentId,
        string question,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default);
}
