using DocuMind.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Manages local physical disk storage for uploaded document files.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger, string? storageRoot = null)
    {
        _logger = logger;
        _storageRoot = string.IsNullOrWhiteSpace(storageRoot)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads")
            : storageRoot;

        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
            _logger.LogInformation("Created storage directory at {Path}", _storageRoot);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        var safeExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var fullPath = Path.Combine(_storageRoot, uniqueFileName);

        if (fileStream.CanSeek)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
        }

        await using (var destinationStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await fileStream.CopyToAsync(destinationStream, cancellationToken);
        }

        _logger.LogInformation("Saved file {FileName} to storage at {FullPath}", fileName, fullPath);
        return fullPath;
    }

    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath)) return Task.CompletedTask;

        try
        {
            if (File.Exists(storagePath))
            {
                File.Delete(storagePath);
                _logger.LogInformation("Deleted storage file at {Path}", storagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete storage file at {Path}", storagePath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || !File.Exists(storagePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        var stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }
}
