namespace DocuMind.Application.Common.Models;

/// <summary>
/// A segmented unit of text bound to a specific document page and sequential index.
/// </summary>
public class TextChunk
{
    public int ChunkIndex { get; set; }
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }

    public TextChunk()
    {
    }

    public TextChunk(int chunkIndex, int pageNumber, string content, int tokenCount = 0)
    {
        ChunkIndex = chunkIndex;
        PageNumber = pageNumber;
        Content = content;
        TokenCount = tokenCount > 0 ? tokenCount : ApproximateTokenCount(content);
    }

    /// <summary>
    /// Heuristic approximation of token count (~4 characters per token).
    /// </summary>
    public static int ApproximateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}

/// <summary>
/// Configuration parameters governing text segment slicing.
/// </summary>
public class ChunkingOptions
{
    public const string SectionName = "Chunking";

    /// <summary>
    /// Desired character count per chunk (~1000 characters).
    /// </summary>
    public int TargetChunkSize { get; set; } = 1000;

    /// <summary>
    /// Sliding overlap in characters between sequential chunks (~200 characters).
    /// </summary>
    public int OverlapSize { get; set; } = 200;

    /// <summary>
    /// Minimum character length for a chunk to be retained.
    /// </summary>
    public int MinChunkSize { get; set; } = 50;

    public void Validate()
    {
        if (TargetChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(TargetChunkSize), "Target chunk size must be greater than zero.");

        if (OverlapSize < 0)
            throw new ArgumentOutOfRangeException(nameof(OverlapSize), "Overlap size cannot be negative.");

        if (OverlapSize >= TargetChunkSize)
            throw new ArgumentOutOfRangeException(nameof(OverlapSize), "Overlap size must be less than target chunk size.");

        if (MinChunkSize < 0 || MinChunkSize > TargetChunkSize)
            throw new ArgumentOutOfRangeException(nameof(MinChunkSize), "Min chunk size must be positive and less than target chunk size.");
    }
}
