using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Orchestrates the document ingestion pipeline: extraction -> chunking -> embedding -> vector upsert -> SQL persistence.
/// </summary>
public interface IDocumentProcessingService
{
    Task<DocumentProcessingResult> ProcessDocumentAsync(Guid documentId, Stream pdfStream, CancellationToken cancellationToken = default);
}
