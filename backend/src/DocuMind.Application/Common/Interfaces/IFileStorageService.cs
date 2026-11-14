namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Manages physical or blob file storage for uploaded document files.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default);
}
