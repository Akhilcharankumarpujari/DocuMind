using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Orchestrates user-scoped document uploading, processing triggers, status queries, and deletion.
/// </summary>
public interface IDocumentService
{
    Task<DocumentDto> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetAllDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetDocumentByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DocumentStatusDto?> GetDocumentStatusAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Stream?> GetDocumentFileAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}
