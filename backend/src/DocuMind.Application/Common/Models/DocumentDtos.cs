using System.Text.Json.Serialization;

namespace DocuMind.Application.Common.Models;

public record DocumentDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("pages")] int Pages,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("updated")] string Updated,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage = null
);

public record DocumentStatusDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("pages")] int Pages,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage = null
);
