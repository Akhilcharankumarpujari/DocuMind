using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace DocuMind.Infrastructure.Pdf;

/// <summary>
/// Extracts page-by-page text from PDF documents using UglyToad.PdfPig.
/// </summary>
public class PdfPigTextExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfPigTextExtractor> _logger;

    public PdfPigTextExtractor(ILogger<PdfPigTextExtractor> logger)
    {
        _logger = logger;
    }

    public Task<PdfExtractionResult> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        if (!pdfStream.CanRead)
            throw new ArgumentException("The provided PDF stream cannot be read.", nameof(pdfStream));

        try
        {
            // Rewind stream if seekable
            if (pdfStream.CanSeek)
            {
                pdfStream.Seek(0, SeekOrigin.Begin);
            }

            cancellationToken.ThrowIfCancellationRequested();

            using var document = PdfDocument.Open(pdfStream, new ParsingOptions { ClipPaths = false });
            var pages = new List<PdfPageText>(document.NumberOfPages);

            for (var i = 1; i <= document.NumberOfPages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var page = document.GetPage(i);
                    var text = page.Text ?? string.Empty;

                    // Clean and normalize whitespace
                    var normalizedText = NormalizeText(text);

                    pages.Add(new PdfPageText(i, normalizedText));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to extract text from page {PageNumber}. Marking page as empty.", i);
                    pages.Add(new PdfPageText(i, string.Empty));
                }
            }

            _logger.LogInformation(
                "Extracted {PageCount} pages from PDF. Non-empty pages: {NonEmptyCount}",
                pages.Count,
                pages.Count(p => !p.IsEmpty));

            return Task.FromResult(new PdfExtractionResult
            {
                Pages = pages
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The provided stream is not a valid PDF or is corrupted.");
            throw new DomainException($"The file is corrupted or is not a valid PDF format: {ex.Message}", ex);
        }
    }

    public async Task<PdfExtractionResult> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new EntityNotFoundException("PdfFile", filePath);

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return await ExtractTextAsync(stream, cancellationToken);
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Replace consecutive tabs/spaces with a single space, preserving newlines
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(line => string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim())))
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, lines);
    }
}
