namespace DocuMind.Infrastructure.Configuration;

public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Endpoint { get; set; } = "http://localhost:6333";
    public string? ApiKey { get; set; }
    public string CollectionName { get; set; } = "documind_chunks";
    public ulong VectorSize { get; set; } = 1536;
}
