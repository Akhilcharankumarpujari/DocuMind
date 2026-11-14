namespace DocuMind.Infrastructure.Configuration;

public class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAi";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeploymentName { get; set; } = "gpt-4.1-mini";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; set; } = 1536;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) &&
                                !string.IsNullOrWhiteSpace(ApiKey) &&
                                !string.IsNullOrWhiteSpace(EmbeddingDeploymentName);
}
