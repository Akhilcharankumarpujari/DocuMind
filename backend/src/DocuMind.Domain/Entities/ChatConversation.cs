using DocuMind.Domain.Common;
using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

/// <summary>
/// Represents an interactive RAG chat session associated with a specific uploaded document.
/// </summary>
public class ChatConversation : BaseEntity, IAuditableEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string Title { get; set; } = "New Conversation";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public ChatConversation()
    {
    }

    public ChatConversation(Guid documentId, string title = "New Conversation")
    {
        DocumentId = documentId;
        Title = string.IsNullOrWhiteSpace(title) ? "New Conversation" : title;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public ChatMessage AddMessage(MessageRole role, string content, string? sourcesJson = null)
    {
        var message = new ChatMessage(Id, role, content, sourcesJson);
        Messages.Add(message);
        UpdatedAtUtc = DateTime.UtcNow;
        return message;
    }
}
