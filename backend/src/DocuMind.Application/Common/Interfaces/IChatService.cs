using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Orchestrates grounded RAG question-answering with Azure OpenAI and Server-Sent Events,
/// strictly enforcing user ownership on all chat completions and streams.
/// </summary>
public interface IChatService
{
    Task<AskQuestionResponse> AskQuestionAsync(
        Guid documentId,
        string question,
        Guid userId,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<SseStreamEvent> StreamQuestionAsync(
        Guid documentId,
        string question,
        Guid userId,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default);
}
