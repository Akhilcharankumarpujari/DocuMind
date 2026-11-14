using DocuMind.Domain.Common;
using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

/// <summary>
/// Represents a single conversation turn between user and AI assistant.
/// </summary>
public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;

    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized metadata citations referencing source chunks and pages for this response.
    /// </summary>
    public string? SourcesJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ChatMessage()
    {
    }

    public ChatMessage(Guid conversationId, MessageRole role, string content, string? sourcesJson = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty.", nameof(content));

        ConversationId = conversationId;
        Role = role;
        Content = content;
        SourcesJson = sourcesJson;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
