using System.Text.Json;
using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Services;

/// <summary>
/// Manages conversation threads and chat messages in MySQL.
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(IApplicationDbContext context, ILogger<ConversationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChatConversation> GetOrCreateConversationAsync(
        Guid documentId,
        Guid? conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify document exists and belongs to the authenticated user
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId, cancellationToken);
        if (document == null)
            throw new EntityNotFoundException(nameof(Document), documentId);

        // 2. If conversationId is specified, fetch and validate document and user association
        if (conversationId.HasValue && conversationId.Value != Guid.Empty)
        {
            var existing = await _context.ChatConversations
                .Include(c => c.Document)
                .Include(c => c.Messages.OrderBy(m => m.CreatedAtUtc))
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value, cancellationToken);

            if (existing != null)
            {
                if (existing.DocumentId != documentId || existing.Document?.UserId != userId)
                {
                    _logger.LogWarning("Access denied: Conversation {ConvId} does not belong to Document {DocId} or User {UserId}",
                        existing.Id, documentId, userId);
                    throw new DomainException("Conversation does not belong to the specified document or user.");
                }

                return existing;
            }
        }

        // 3. Create a new conversation thread
        var newConversation = new ChatConversation(documentId, "Chat with document");
        _context.ChatConversations.Add(newConversation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new ChatConversation {ConversationId} for Document {DocumentId} and User {UserId}",
            newConversation.Id, documentId, userId);

        return newConversation;
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsByDocumentAsync(
        Guid documentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify document ownership
        var isOwner = await _context.Documents.AnyAsync(d => d.Id == documentId && d.UserId == userId, cancellationToken);
        if (!isOwner)
            return Array.Empty<ConversationDto>();

        var conversations = await _context.ChatConversations
            .Where(c => c.DocumentId == documentId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return conversations.Select(MapToDto).ToList();
    }

    public async Task<ConversationDto?> GetConversationByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.ChatConversations
            .Include(c => c.Document)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAtUtc))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.Document != null && c.Document.UserId == userId, cancellationToken);

        return conversation == null ? null : MapToDto(conversation);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversationExists = await _context.ChatConversations
            .Include(c => c.Document)
            .AnyAsync(c => c.Id == conversationId && c.Document != null && c.Document.UserId == userId, cancellationToken);

        if (!conversationExists)
            throw new EntityNotFoundException(nameof(ChatConversation), conversationId);

        var messages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return messages.Select(MapMessageToDto).ToList();
    }

    public async Task<ChatMessage> AddMessageAsync(
        Guid conversationId,
        MessageRole role,
        string content,
        string? sourcesJson = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
            throw new EntityNotFoundException(nameof(ChatConversation), conversationId);

        var message = conversation.AddMessage(role, content, sourcesJson);
        await _context.SaveChangesAsync(cancellationToken);

        return message;
    }

    private static ConversationDto MapToDto(ChatConversation entity)
    {
        return new ConversationDto(
            entity.Id,
            entity.DocumentId,
            entity.Title,
            entity.CreatedAtUtc,
            entity.Messages.Select(MapMessageToDto).ToList());
    }

    private static ChatMessageDto MapMessageToDto(ChatMessage msg)
    {
        var sources = new List<SourceCitation>();
        if (!string.IsNullOrWhiteSpace(msg.SourcesJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<SourceCitation>>(msg.SourcesJson, SseJsonContext.DefaultOptions);
                if (parsed != null) sources = parsed;
            }
            catch
            {
                // Ignore parse failures on legacy/unstructured source json
            }
        }

        return new ChatMessageDto(
            msg.Id,
            msg.Role == MessageRole.User ? "user" : "assistant",
            msg.Content,
            sources,
            msg.CreatedAtUtc);
    }
}
