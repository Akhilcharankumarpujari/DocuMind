using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Domain.Exceptions;
using DocuMind.Infrastructure.Configuration;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class DocumentServiceTests
{
    private readonly DocuMindDbContext _dbContext;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly Mock<IDocumentProcessingService> _processingMock;
    private readonly Mock<IVectorStore> _vectorStoreMock;
    private readonly IOptions<QdrantOptions> _qdrantOptions;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseInMemoryDatabase(databaseName: $"DocuMind_DocTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new DocuMindDbContext(options);
        _fileStorageMock = new Mock<IFileStorageService>();
        _processingMock = new Mock<IDocumentProcessingService>();
        _vectorStoreMock = new Mock<IVectorStore>();
        _qdrantOptions = Options.Create(new QdrantOptions { CollectionName = "documind-chunks" });

        _fileStorageMock
            .Setup(f => f.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\storage\\doc.pdf");

        _processingMock
            .Setup(p => p.ProcessDocumentAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentProcessingResult.Success(Guid.NewGuid(), 5, 10, 2000));
    }

    [Fact]
    public async Task UploadDocumentAsync_ValidPdf_ShouldSaveFileAndPersistRecord()
    {
        // Arrange: Valid %PDF header
        var pdfBytes = "%PDF-1.4 sample content for documind upload test"u8.ToArray();
        using var stream = new MemoryStream(pdfBytes);

        var service = new DocumentService(
            _dbContext,
            _fileStorageMock.Object,
            _processingMock.Object,
            _vectorStoreMock.Object,
            _qdrantOptions,
            NullLogger<DocumentService>.Instance);

        // Act
        var testUserId = Guid.NewGuid();
        var result = await service.UploadDocumentAsync(stream, "Sample.pdf", stream.Length, "application/pdf", testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Sample.pdf");
        result.Status.Should().Be("Pending");

        var docInDb = await _dbContext.Documents.FirstOrDefaultAsync(d => d.FileName == "Sample.pdf");
        docInDb.Should().NotBeNull();
        docInDb!.FileSizeBytes.Should().Be(pdfBytes.Length);
        docInDb.FileHash.Should().NotBeNullOrWhiteSpace();
        docInDb.UserId.Should().Be(testUserId);

        _fileStorageMock.Verify(f => f.SaveFileAsync(It.IsAny<Stream>(), "Sample.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _processingMock.Verify(p => p.ProcessDocumentAsync(docInDb.Id, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_InvalidSignature_ShouldThrowArgumentException()
    {
        // Arrange: Random bytes not starting with %PDF
        var fakeBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
        using var stream = new MemoryStream(fakeBytes);

        var service = new DocumentService(
            _dbContext,
            _fileStorageMock.Object,
            _processingMock.Object,
            _vectorStoreMock.Object,
            _qdrantOptions,
            NullLogger<DocumentService>.Instance);

        // Act
        var act = async () => await service.UploadDocumentAsync(stream, "Fake.pdf", stream.Length, "application/pdf", Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*file signature is invalid*");
    }

    [Fact]
    public async Task UploadDocumentAsync_NonPdfExtension_ShouldThrowArgumentException()
    {
        // Arrange
        var bytes = "%PDF-1.4 test"u8.ToArray();
        using var stream = new MemoryStream(bytes);

        var service = new DocumentService(
            _dbContext,
            _fileStorageMock.Object,
            _processingMock.Object,
            _vectorStoreMock.Object,
            _qdrantOptions,
            NullLogger<DocumentService>.Instance);

        // Act
        var act = async () => await service.UploadDocumentAsync(stream, "Document.docx", stream.Length, "application/pdf", Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Only PDF files are supported*");
    }

    [Fact]
    public async Task UploadDocumentAsync_Exceeds20Mb_ShouldThrowArgumentException()
    {
        // Arrange: 21 MB
        using var stream = new MemoryStream("%PDF"u8.ToArray());
        var largeSize = 21L * 1024 * 1024;

        var service = new DocumentService(
            _dbContext,
            _fileStorageMock.Object,
            _processingMock.Object,
            _vectorStoreMock.Object,
            _qdrantOptions,
            NullLogger<DocumentService>.Instance);

        // Act
        var act = async () => await service.UploadDocumentAsync(stream, "Huge.pdf", largeSize, "application/pdf", Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceeds the 20 MB limit*");
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldDeleteVectorsStorageAndDatabaseRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doc = new Document("DeleteMe.pdf", 1024, "C:\\storage\\deleteme.pdf", userId: userId);
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        var service = new DocumentService(
            _dbContext,
            _fileStorageMock.Object,
            _processingMock.Object,
            _vectorStoreMock.Object,
            _qdrantOptions,
            NullLogger<DocumentService>.Instance);

        // Act
        await service.DeleteDocumentAsync(doc.Id, userId);

        // Assert
        var docInDb = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == doc.Id);
        docInDb.Should().BeNull();

        _vectorStoreMock.Verify(v => v.DeleteDocumentVectorsAsync("documind-chunks", doc.Id, It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteFileAsync("C:\\storage\\deleteme.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }
}
