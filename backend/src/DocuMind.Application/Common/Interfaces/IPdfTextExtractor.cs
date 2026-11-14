using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Extracts page-delimited textual content from PDF files.
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts text page by page from an open PDF stream.
    /// Preserves original 1-based page numbers.
    /// </summary>
    Task<PdfExtractionResult> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts text page by page from a file path.
    /// </summary>
    Task<PdfExtractionResult> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
}
