using DocuMind.Domain.Exceptions;
using DocuMind.Infrastructure.Pdf;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class PdfTextExtractorTests
{
    private readonly PdfPigTextExtractor _extractor;

    public PdfTextExtractorTests()
    {
        _extractor = new PdfPigTextExtractor(NullLogger<PdfPigTextExtractor>.Instance);
    }

    [Fact]
    public async Task ExtractTextAsync_MultiPagePdf_ShouldExtractTextAndPreservePageNumbers()
    {
        // Arrange: Generate an in-memory 3-page PDF using PdfPig builder
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        // Page 1
        var p1 = builder.AddPage(PageSize.A4);
        p1.AddText("Introduction to DocuMind Artificial Intelligence", 12, new PdfPoint(50, 700), font);

        // Page 2: Empty page
        builder.AddPage(PageSize.A4);

        // Page 3
        var p3 = builder.AddPage(PageSize.A4);
        p3.AddText("Experimental results and conclusion section", 12, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();
        using var stream = new MemoryStream(pdfBytes);

        // Act
        var result = await _extractor.ExtractTextAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.PageCount.Should().Be(3);
        result.HasExtractableText.Should().BeTrue();

        result.Pages[0].PageNumber.Should().Be(1);
        result.Pages[0].Text.Should().Contain("Introduction to DocuMind");
        result.Pages[0].IsEmpty.Should().BeFalse();

        result.Pages[1].PageNumber.Should().Be(2);
        result.Pages[1].IsEmpty.Should().BeTrue();

        result.Pages[2].PageNumber.Should().Be(3);
        result.Pages[2].Text.Should().Contain("Experimental results and conclusion");
        result.Pages[2].IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractTextAsync_CorruptedPdf_ShouldThrowDomainException()
    {
        // Arrange
        var corruptedBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0xFF, 0xFE, 0x55 };
        using var stream = new MemoryStream(corruptedBytes);

        // Act
        var act = async () => await _extractor.ExtractTextAsync(stream);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*corrupted or is not a valid PDF*");
    }

    [Fact]
    public async Task ExtractTextAsync_NonExistentFile_ShouldThrowEntityNotFoundException()
    {
        // Act
        var act = async () => await _extractor.ExtractTextAsync("C:\\non_existent_folder\\fake_file.pdf");

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task ExtractTextAsync_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var p = builder.AddPage(PageSize.A4);
        p.AddText("Test content", 12, new PdfPoint(50, 700), font);
        var bytes = builder.Build();

        using var stream = new MemoryStream(bytes);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _extractor.ExtractTextAsync(stream, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExtractTextAsync_RealPhysicalPdfFile_ShouldExtractAndChunkWithPageAttribution()
    {
        // Arrange: Generate and save an actual physical PDF to disk
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"documind_test_{Guid.NewGuid():N}.pdf");

        try
        {
            var builder = new PdfDocumentBuilder();
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            // Page 1: Introduction section
            var p1 = builder.AddPage(PageSize.A4);
            p1.AddText("Chapter 1: DocuMind Architecture and Foundation. This section covers system design.", 12, new PdfPoint(50, 700), font);

            // Page 2: Implementation details
            var p2 = builder.AddPage(PageSize.A4);
            p2.AddText("Chapter 2: Qdrant Vector Search and Intelligent Chunking. Embeddings are stored with cosine similarity.", 12, new PdfPoint(50, 700), font);

            var pdfBytes = builder.Build();
            await File.WriteAllBytesAsync(tempFilePath, pdfBytes);

            // Act: Extract from physical file path on disk
            var extractionResult = await _extractor.ExtractTextAsync(tempFilePath);

            // Assert Extraction
            extractionResult.Should().NotBeNull();
            extractionResult.PageCount.Should().Be(2);
            extractionResult.HasExtractableText.Should().BeTrue();
            extractionResult.Pages[0].PageNumber.Should().Be(1);
            extractionResult.Pages[0].Text.Should().Contain("Chapter 1: DocuMind Architecture");
            extractionResult.Pages[1].PageNumber.Should().Be(2);
            extractionResult.Pages[1].Text.Should().Contain("Chapter 2: Qdrant Vector Search");

            // Act: Pass pages to TextChunker
            var chunker = new DocuMind.Infrastructure.TextChunking.TextChunker(NullLogger<DocuMind.Infrastructure.TextChunking.TextChunker>.Instance);
            var chunks = chunker.CreateChunks(extractionResult.Pages);

            // Assert Chunking & Page Attribution
            chunks.Should().NotBeEmpty();
            chunks.Should().HaveCount(2);

            chunks[0].ChunkIndex.Should().Be(0);
            chunks[0].PageNumber.Should().Be(1);
            chunks[0].Content.Should().Contain("Chapter 1: DocuMind Architecture");

            chunks[1].ChunkIndex.Should().Be(1);
            chunks[1].PageNumber.Should().Be(2);
            chunks[1].Content.Should().Contain("Chapter 2: Qdrant Vector Search");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
