using System.Text.Json.Serialization;

namespace DocuMind.Application.Common.Models;

public record AskQuestionRequest(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("conversationId")] Guid? ConversationId = null
);

public record AskQuestionResponse(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("sources")] IReadOnlyList<SourceCitation> Sources,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("messageId")] Guid MessageId
);

public record ConversationDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("documentId")] Guid DocumentId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessageDto> Messages
);

public record ChatMessageDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("sources")] IReadOnlyList<SourceCitation> Sources,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc
);
