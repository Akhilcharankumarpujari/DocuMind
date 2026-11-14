using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuMind.Application.Common.Models;

/// <summary>
/// Server-Sent Events contract tailored to DocuMind chat streaming requirements.
/// Matches frontend reader expectations in chatService.js.
/// </summary>
public class SseStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    [JsonPropertyName("sources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SourceCitation>? Sources { get; set; }

    [JsonPropertyName("answer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Answer { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    public static SseStreamEvent CreateToken(string content) => new()
    {
        Type = "token",
        Content = content
    };

    public static SseStreamEvent CreateSources(IReadOnlyList<SourceCitation> sources) => new()
    {
        Type = "sources",
        Sources = sources
    };

    public static SseStreamEvent CreateDone(string? finalAnswer = null) => new()
    {
        Type = "done",
        Answer = finalAnswer
    };

    public static SseStreamEvent CreateError(string errorMessage) => new()
    {
        Type = "error",
        Error = errorMessage
    };

    /// <summary>
    /// Serializes event into standard SSE line: data: {json}\n\n
    /// </summary>
    public string ToSseMessage()
    {
        var json = JsonSerializer.Serialize(this, SseJsonContext.DefaultOptions);
        return $"data: {json}\n\n";
    }
}

public class SourceCitation
{
    [JsonPropertyName("documentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? DocumentId { get; set; }

    [JsonPropertyName("chunkId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ChunkId { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public double Score { get; set; }

    public SourceCitation()
    {
    }

    public SourceCitation(int page, string snippet, double score, Guid? documentId = null, Guid? chunkId = null)
    {
        Page = page;
        Snippet = snippet;
        Score = score;
        DocumentId = documentId;
        ChunkId = chunkId;
    }
}

public static class SseJsonContext
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
