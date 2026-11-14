using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Orchestrates document uploading, processing triggers, status queries, and deletion.
/// </summary>
public interface IDocumentService
{
    Task<DocumentDto> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentStatusDto?> GetDocumentStatusAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Stream?> GetDocumentFileAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
}
