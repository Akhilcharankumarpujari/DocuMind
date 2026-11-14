using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Splits page-extracted texts into overlapping semantic chunks with sequential indexing and page attribution.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Generates chunks preserving page numbers and assigning sequential indexes from 0.
    /// </summary>
    IReadOnlyList<TextChunk> CreateChunks(IReadOnlyList<PdfPageText> pages, ChunkingOptions? options = null);
}
