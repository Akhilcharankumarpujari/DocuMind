using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Manages conversation threads and chat message persistence.
/// </summary>
public interface IConversationService
{
    Task<ChatConversation> GetOrCreateConversationAsync(
        Guid documentId,
        Guid? conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationDto>> GetConversationsByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<ConversationDto?> GetConversationByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<ChatMessage> AddMessageAsync(
        Guid conversationId,
        MessageRole role,
        string content,
        string? sourcesJson = null,
        CancellationToken cancellationToken = default);
}
