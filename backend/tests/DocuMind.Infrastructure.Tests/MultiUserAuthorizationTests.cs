using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
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

public class MultiUserAuthorizationTests
{
    private readonly DocuMindDbContext _dbContext;
    private readonly DocumentService _documentService;
    private readonly ConversationService _conversationService;
    private readonly RagRetrievalService _ragService;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly Mock<IDocumentProcessingService> _processingMock;
    private readonly Mock<IVectorStore> _vectorStoreMock;
    private readonly Mock<IEmbeddingService> _embeddingMock;

    private readonly Guid _userAId = Guid.NewGuid();
    private readonly Guid _userBId = Guid.NewGuid();

    public MultiUserAuthorizationTests()
    {
        var dbOptions = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseInMemoryDatabase(databaseName: $"DocuMind_MultiUserTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new DocuMindDbContext(dbOptions);

        _fileStorageMock = new Mock<IFileStorageService>();
        _processingMock = new Mock<IDocumentProcessingService>();
        _vectorStoreMock = new Mock<IVectorStore>();
        _embeddingMock = new Mock<IEmbeddingService>();

        var qdrantOptions = Options.Create(new QdrantOptions { CollectionName = "documind-chunks" });

        _documentService = new DocumentService(
            _dbContext,
            _fileStorageMock.Object,
            _processingMock.Object,
            _vectorStoreMock.Object,
            qdrantOptions,
            NullLogger<DocumentService>.Instance);

        _conversationService = new ConversationService(
            _dbContext,
            NullLogger<ConversationService>.Instance);

        _ragService = new RagRetrievalService(
            _embeddingMock.Object,
            _vectorStoreMock.Object,
            _dbContext,
            qdrantOptions,
            NullLogger<RagRetrievalService>.Instance);
    }

    [Fact]
    public async Task GetAllDocumentsAsync_ShouldOnlyReturnOwnedDocuments()
    {
        // Arrange
        var docA = new Document("UserA_Doc.pdf", 1024, "C:\\storage\\a.pdf", userId: _userAId);
        var docB = new Document("UserB_Doc.pdf", 2048, "C:\\storage\\b.pdf", userId: _userBId);
        _dbContext.Documents.AddRange(docA, docB);
        await _dbContext.SaveChangesAsync();

        // Act
        var docsForUserA = await _documentService.GetAllDocumentsAsync(_userAId);
        var docsForUserB = await _documentService.GetAllDocumentsAsync(_userBId);

        // Assert
        docsForUserA.Should().ContainSingle(d => d.Name == "UserA_Doc.pdf");
        docsForUserA.Any(d => d.Name == "UserB_Doc.pdf").Should().BeFalse();

        docsForUserB.Should().ContainSingle(d => d.Name == "UserB_Doc.pdf");
        docsForUserB.Any(d => d.Name == "UserA_Doc.pdf").Should().BeFalse();
    }

    [Fact]
    public async Task GetDocumentByIdAsync_WhenRequestedByOtherUser_ShouldReturnNullToPreventEnumeration()
    {
        // Arrange: Document belongs to User A
        var docA = new Document("PrivateA.pdf", 1024, "C:\\storage\\priv_a.pdf", userId: _userAId);
        _dbContext.Documents.Add(docA);
        await _dbContext.SaveChangesAsync();

        // Act: User B tries to view User A's document
        var result = await _documentService.GetDocumentByIdAsync(docA.Id, _userBId);

        // Assert: Must return null (results in 404 in controller, preventing IDOR)
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDocumentFileAsync_WhenRequestedByOtherUser_ShouldReturnNull()
    {
        // Arrange
        var docA = new Document("ConfidentialA.pdf", 1024, "C:\\storage\\conf_a.pdf", userId: _userAId);
        _dbContext.Documents.Add(docA);
        await _dbContext.SaveChangesAsync();

        // Act: User B attempts to download User A's PDF
        var stream = await _documentService.GetDocumentFileAsync(docA.Id, _userBId);

        // Assert
        stream.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenRequestedByOtherUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var docA = new Document("TargetA.pdf", 1024, "C:\\storage\\target_a.pdf", userId: _userAId);
        _dbContext.Documents.Add(docA);
        await _dbContext.SaveChangesAsync();

        // Act: User B attempts to delete User A's document
        var act = async () => await _documentService.DeleteDocumentAsync(docA.Id, _userBId);

        // Assert: Must be rejected
        await act.Should().ThrowAsync<EntityNotFoundException>();

        // Verify document is NOT deleted
        var stillInDb = await _dbContext.Documents.AnyAsync(d => d.Id == docA.Id);
        stillInDb.Should().BeTrue();
    }

    [Fact]
    public async Task RetrieveContextAsync_UserCannotQueryVectorsForUnownedDocument()
    {
        // Arrange: Document belongs to User A
        var docA = new Document("ContractA.pdf", 1024, "C:\\storage\\contract_a.pdf", userId: _userAId);
        _dbContext.Documents.Add(docA);
        await _dbContext.SaveChangesAsync();

        // Act: User B attempts to query User A's document
        var act = async () => await _ragService.RetrieveContextAsync(docA.Id, "What are the secret terms?", _userBId);

        // Assert: Throws before Qdrant is called
        await act.Should().ThrowAsync<EntityNotFoundException>();

        // Verify Qdrant was never called
        _vectorStoreMock.Verify(v => v.SearchAsync(
            It.IsAny<string>(),
            It.IsAny<ReadOnlyMemory<float>>(),
            It.IsAny<Guid?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Conversations_UserCannotAccessOtherUsersConversationsOrMessages()
    {
        // Arrange
        var docA = new Document("DocA.pdf", 1024, "C:\\storage\\doc_a.pdf", userId: _userAId);
        _dbContext.Documents.Add(docA);
        await _dbContext.SaveChangesAsync();

        var convA = await _conversationService.GetOrCreateConversationAsync(docA.Id, null, _userAId);
        await _conversationService.AddMessageAsync(convA.Id, Domain.Enums.MessageRole.User, "Secret info from User A");

        // Act 1: User B tries to list conversations for Doc A
        var convsForUserB = await _conversationService.GetConversationsByDocumentAsync(docA.Id, _userBId);
        convsForUserB.Should().BeEmpty();

        // Act 2: User B tries to get convA by Id
        var singleConv = await _conversationService.GetConversationByIdAsync(convA.Id, _userBId);
        singleConv.Should().BeNull();

        // Act 3: User B tries to read messages of convA
        var readMessagesAct = async () => await _conversationService.GetMessagesAsync(convA.Id, _userBId);
        await readMessagesAct.Should().ThrowAsync<EntityNotFoundException>();
    }
}
