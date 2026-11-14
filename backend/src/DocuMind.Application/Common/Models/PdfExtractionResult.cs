namespace DocuMind.Application.Common.Models;

/// <summary>
/// Text extracted from a single PDF page with page number preservation.
/// </summary>
public class PdfPageText
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public int WordCount => string.IsNullOrWhiteSpace(Text)
        ? 0
        : Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);

    public PdfPageText()
    {
    }

    public PdfPageText(int pageNumber, string text)
    {
        PageNumber = pageNumber;
        Text = text ?? string.Empty;
    }
}

/// <summary>
/// Aggregated extraction result across all pages of a PDF document.
/// </summary>
public class PdfExtractionResult
{
    public IReadOnlyList<PdfPageText> Pages { get; set; } = Array.Empty<PdfPageText>();
    public int PageCount => Pages.Count;
    public bool HasExtractableText => Pages.Any(p => !p.IsEmpty);
    public long TotalCharacters => Pages.Sum(p => (long)p.Text.Length);
    public long TotalWords => Pages.Sum(p => (long)p.WordCount);

    public string FullText => string.Join(Environment.NewLine + Environment.NewLine,
        Pages.Where(p => !p.IsEmpty).Select(p => $"--- Page {p.PageNumber} ---{Environment.NewLine}{p.Text}"));

    public static PdfExtractionResult Empty() => new()
    {
        Pages = Array.Empty<PdfPageText>()
    };
}
