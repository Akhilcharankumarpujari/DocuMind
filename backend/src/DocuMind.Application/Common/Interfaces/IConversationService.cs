using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Manages user-scoped conversation threads and chat message persistence.
/// </summary>
public interface IConversationService
{
    Task<ChatConversation> GetOrCreateConversationAsync(
        Guid documentId,
        Guid? conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationDto>> GetConversationsByDocumentAsync(
        Guid documentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ConversationDto?> GetConversationByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ChatMessage> AddMessageAsync(
        Guid conversationId,
        MessageRole role,
        string content,
        string? sourcesJson = null,
        CancellationToken cancellationToken = default);
}
