using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.TextChunking;

/// <summary>
/// Slices page-extracted texts into semantic, overlapping segments while preserving exact PDF page citations.
/// </summary>
public class TextChunker : ITextChunker
{
    private readonly ILogger<TextChunker> _logger;

    public TextChunker(ILogger<TextChunker> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<TextChunk> CreateChunks(IReadOnlyList<PdfPageText> pages, ChunkingOptions? options = null)
    {
        if (pages == null || pages.Count == 0)
            return Array.Empty<TextChunk>();

        var opts = options ?? new ChunkingOptions();
        opts.Validate();

        var chunks = new List<TextChunk>();
        var sequentialChunkIndex = 0;

        foreach (var page in pages)
        {
            if (page.IsEmpty)
                continue;

            var pageChunks = ChunkPageText(page.Text, page.PageNumber, ref sequentialChunkIndex, opts);
            chunks.AddRange(pageChunks);
        }

        _logger.LogInformation(
            "Created {TotalChunks} chunks across {PageCount} pages with target size {TargetSize} and overlap {Overlap}",
            chunks.Count,
            pages.Count,
            opts.TargetChunkSize,
            opts.OverlapSize);

        return chunks;
    }

    private static List<TextChunk> ChunkPageText(string text, int pageNumber, ref int globalIndex, ChunkingOptions options)
    {
        var result = new List<TextChunk>();
        var cleanText = text.Trim();

        if (string.IsNullOrWhiteSpace(cleanText))
            return result;

        // If page text is within target chunk size, keep it as a single chunk
        if (cleanText.Length <= options.TargetChunkSize)
        {
            result.Add(new TextChunk(globalIndex++, pageNumber, cleanText));
            return result;
        }

        var start = 0;
        var textLength = cleanText.Length;

        while (start < textLength)
        {
            var remaining = textLength - start;

            if (remaining <= options.TargetChunkSize)
            {
                var finalSegment = cleanText.Substring(start).Trim();
                if (finalSegment.Length >= options.MinChunkSize || result.Count == 0)
                {
                    result.Add(new TextChunk(globalIndex++, pageNumber, finalSegment));
                }
                break;
            }

            var proposedEnd = start + options.TargetChunkSize;
            var bestCut = FindBestBreakPoint(cleanText, start, proposedEnd);

            var chunkContent = cleanText.Substring(start, bestCut - start).Trim();
            if (chunkContent.Length >= options.MinChunkSize)
            {
                result.Add(new TextChunk(globalIndex++, pageNumber, chunkContent));
            }

            // Calculate next start with overlap, ensuring strictly positive forward progress
            var nextStart = Math.Max(start + 1, bestCut - options.OverlapSize);

            // Avoid getting stuck if overlap keeps us at the same point
            if (nextStart <= start)
            {
                nextStart = bestCut;
            }

            start = nextStart;
        }

        return result;
    }

    private static int FindBestBreakPoint(string text, int start, int preferredEnd)
    {
        if (preferredEnd >= text.Length)
            return text.Length;

        var searchWindowStart = Math.Max(start + (int)((preferredEnd - start) * 0.75), start + 1);

        // 1. Look for sentence terminators (. ! ? \n\n)
        for (var i = preferredEnd; i >= searchWindowStart; i--)
        {
            var c = text[i];
            if (c == '.' || c == '!' || c == '?' || c == '\n')
            {
                // Break immediately after the punctuation mark or newline
                return i + 1;
            }
        }

        // 2. Fall back to whitespace boundary
        for (var i = preferredEnd; i >= searchWindowStart; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        // 3. Last resort: cut exactly at preferred end
        return preferredEnd;
    }
}
