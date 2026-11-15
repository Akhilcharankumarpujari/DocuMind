using System.Security.Cryptography;
using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Exceptions;
using DocuMind.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Services;

/// <summary>
/// Orchestrates document upload validation, file storage, ingestion pipeline triggers, and cascade deletion.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IDocumentProcessingService _processingService;
    private readonly IVectorStore _vectorStore;
    private readonly QdrantOptions _qdrantOptions;
    private readonly ILogger<DocumentService> _logger;

    private static readonly byte[] PdfMagicNumber = "%PDF"u8.ToArray();
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public DocumentService(
        IApplicationDbContext context,
        IFileStorageService fileStorage,
        IDocumentProcessingService processingService,
        IVectorStore vectorStore,
        IOptions<QdrantOptions> qdrantOptions,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _processingService = processingService;
        _vectorStore = vectorStore;
        _qdrantOptions = qdrantOptions.Value;
        _logger = logger;
    }

    public async Task<DocumentDto> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID must be valid.", nameof(userId));

        // 1. Validation
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only PDF files are supported.", nameof(fileName));

        if (fileSize <= 0)
            throw new ArgumentException("The uploaded file is empty.", nameof(fileSize));

        if (fileSize > MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds the 20 MB limit. Actual: {fileSize / 1024 / 1024.0:F1} MB", nameof(fileSize));

        // 2. Validate PDF signature (magic number %PDF)
        if (fileStream.CanSeek)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
        }

        var header = new byte[4];
        var bytesRead = await fileStream.ReadAsync(header.AsMemory(0, 4), cancellationToken);
        if (bytesRead < 4 || !header.SequenceEqual(PdfMagicNumber))
        {
            throw new ArgumentException("The file signature is invalid. The file is not a valid PDF document.", nameof(fileStream));
        }

        fileStream.Seek(0, SeekOrigin.Begin);

        // 3. Compute file hash
        string? fileHash = null;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
            fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        fileStream.Seek(0, SeekOrigin.Begin);

        // 4. Save file to disk
        var storagePath = await _fileStorage.SaveFileAsync(fileStream, fileName, cancellationToken);

        // 5. Persist document in DB with owner UserId
        var document = new Document(
            fileName: fileName,
            fileSizeBytes: fileSize,
            storagePath: storagePath,
            contentType: contentType,
            fileHash: fileHash,
            userId: userId);

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} created in MySQL for User {UserId}. Initiating ingestion pipeline...", document.Id, userId);

        // 6. Trigger Phase 3 ingestion pipeline (Extract -> Chunk -> Embed -> Qdrant -> MySQL)
        try
        {
            fileStream.Seek(0, SeekOrigin.Begin);
            await _processingService.ProcessDocumentAsync(document.Id, fileStream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background processing failed for document {DocumentId}.", document.Id);
            // Document status is set to Failed by the processing service
        }

        // 7. Reload document with updated status and page count
        var refreshed = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == document.Id, cancellationToken) ?? document;

        return MapToDto(refreshed);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetAllDocumentsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return documents.Select(MapToDto).ToList();
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var doc = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);

        return doc == null ? null : MapToDto(doc);
    }

    public async Task<DocumentStatusDto?> GetDocumentStatusAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var doc = await _context.Documents
            .AsNoTracking()
            .Where(d => d.Id == id && d.UserId == userId)
            .Select(d => new { d.Id, d.Status, d.PageCount, d.ErrorMessage })
            .FirstOrDefaultAsync(cancellationToken);

        return doc == null ? null : new DocumentStatusDto(
            doc.Id.ToString(),
            doc.Status.ToString(),
            doc.PageCount,
            doc.ErrorMessage);
    }

    public async Task<Stream?> GetDocumentFileAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var doc = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);

        if (doc == null || string.IsNullOrWhiteSpace(doc.StoragePath))
            return null;

        return await _fileStorage.GetFileAsync(doc.StoragePath, cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);

        if (document == null)
            throw new EntityNotFoundException(nameof(Document), id);

        _logger.LogInformation("Beginning deletion of Document {DocumentId} and related resources...", id);

        // 1. Delete vectors from Qdrant
        try
        {
            var collectionName = string.IsNullOrWhiteSpace(_qdrantOptions.CollectionName) ? "documind-chunks" : _qdrantOptions.CollectionName;
            await _vectorStore.DeleteDocumentVectorsAsync(collectionName, id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Qdrant vectors for Document {DocumentId}. Continuing deletion.", id);
        }

        // 2. Delete physical storage file
        if (!string.IsNullOrWhiteSpace(document.StoragePath))
        {
            await _fileStorage.DeleteFileAsync(document.StoragePath, cancellationToken);
        }

        // 3. Remove document record from MySQL (EF Core cascade deletes chunks, conversations, messages)
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} and associated records successfully deleted.", id);
    }

    public static DocumentDto MapToDto(Document doc)
    {
        return new DocumentDto(
            doc.Id.ToString(),
            doc.FileName,
            doc.PageCount,
            FormatFileSize(doc.FileSizeBytes),
            doc.Status.ToString(),
            FormatUpdatedTime(doc.UpdatedAtUtc ?? doc.CreatedAtUtc),
            doc.ErrorMessage);
    }

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F0} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    public static string FormatUpdatedTime(DateTime utcTime)
    {
        var span = DateTime.UtcNow - utcTime;
        if (span.TotalMinutes < 2) return "Just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return "Today";
        if (span.TotalDays < 2) return "Yesterday";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return utcTime.ToString("MMM dd");
    }
}
