using DocuMind.Application.Common.Models;
using DocuMind.Infrastructure.TextChunking;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class TextChunkerTests
{
    private readonly TextChunker _chunker;

    public TextChunkerTests()
    {
        _chunker = new TextChunker(NullLogger<TextChunker>.Instance);
    }

    [Fact]
    public void CreateChunks_EmptyInput_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyPages = Array.Empty<PdfPageText>();

        // Act
        var chunks = _chunker.CreateChunks(emptyPages);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void CreateChunks_ShortText_ShouldCreateSingleChunk()
    {
        // Arrange
        var pages = new List<PdfPageText>
        {
            new(pageNumber: 1, text: "DocuMind simplifies document interaction using RAG and vector databases.")
        };

        var options = new ChunkingOptions
        {
            TargetChunkSize = 1000,
            OverlapSize = 200,
            MinChunkSize = 50
        };

        // Act
        var chunks = _chunker.CreateChunks(pages, options);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].ChunkIndex.Should().Be(0);
        chunks[0].PageNumber.Should().Be(1);
        chunks[0].Content.Should().Be("DocuMind simplifies document interaction using RAG and vector databases.");
    }

    [Fact]
    public void CreateChunks_LongText_ShouldProduceOverlappingChunks_WithSequentialIndexes()
    {
        // Arrange: Generate a text longer than target chunk size (~1200 characters)
        var sentence = "Artificial intelligence and retrieval augmented generation provide grounded responses from document embeddings. ";
        var longText = string.Concat(Enumerable.Repeat(sentence, 15)); // ~1700 chars

        var pages = new List<PdfPageText>
        {
            new(pageNumber: 1, text: longText)
        };

        var options = new ChunkingOptions
        {
            TargetChunkSize = 500,
            OverlapSize = 100,
            MinChunkSize = 50
        };

        // Act
        var chunks = _chunker.CreateChunks(pages, options);

        // Assert
        chunks.Count.Should().BeGreaterThan(1);

        // Verify sequential indexes starting at 0
        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i].ChunkIndex.Should().Be(i);
            chunks[i].PageNumber.Should().Be(1);
            chunks[i].Content.Length.Should().BeGreaterThanOrEqualTo(options.MinChunkSize);
        }

        // Verify overlap between chunk 0 and chunk 1
        var chunk0End = chunks[0].Content.Substring(chunks[0].Content.Length - 50);
        chunks[1].Content.Should().Contain(chunk0End.Trim());
    }

    [Fact]
    public void CreateChunks_MultiPageDocument_ShouldPreservePageAttributionAcrossPages()
    {
        // Arrange
        var pages = new List<PdfPageText>
        {
            new(pageNumber: 1, text: "Page 1 overview and executive summary content."),
            new(pageNumber: 2, text: ""), // Empty page
            new(pageNumber: 3, text: "Page 3 methodology, architecture diagrams, and pipeline steps.")
        };

        var options = new ChunkingOptions
        {
            TargetChunkSize = 1000,
            OverlapSize = 200
        };

        // Act
        var chunks = _chunker.CreateChunks(pages, options);

        // Assert
        chunks.Should().HaveCount(2);

        chunks[0].ChunkIndex.Should().Be(0);
        chunks[0].PageNumber.Should().Be(1);
        chunks[0].Content.Should().Contain("Page 1 overview");

        chunks[1].ChunkIndex.Should().Be(1);
        chunks[1].PageNumber.Should().Be(3);
        chunks[1].Content.Should().Contain("Page 3 methodology");
    }

    [Fact]
    public void ChunkingOptions_Validation_ShouldEnforceConstraints()
    {
        // Target <= 0
        var optZero = new ChunkingOptions { TargetChunkSize = 0 };
        var actZero = () => optZero.Validate();
        actZero.Should().Throw<ArgumentOutOfRangeException>();

        // Overlap >= Target
        var optOverlap = new ChunkingOptions { TargetChunkSize = 500, OverlapSize = 500 };
        var actOverlap = () => optOverlap.Validate();
        actOverlap.Should().Throw<ArgumentOutOfRangeException>();

        // Overlap < 0
        var optNegativeOverlap = new ChunkingOptions { TargetChunkSize = 500, OverlapSize = -10 };
        var actNegativeOverlap = () => optNegativeOverlap.Validate();
        actNegativeOverlap.Should().Throw<ArgumentOutOfRangeException>();
    }
}
