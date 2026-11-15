using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using Azure.AI.OpenAI;
using DocuMind.Infrastructure.Ai;
using DocuMind.Infrastructure.Configuration;
using DocuMind.Infrastructure.Pdf;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Services;
using DocuMind.Infrastructure.Storage;
using DocuMind.Infrastructure.TextChunking;
using DocuMind.Infrastructure.VectorDb;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class LiveIntegrationTests
{
    private static string FindEnvPath()
    {
        var current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, ".env");
            if (File.Exists(candidate)) return candidate;

            var backendEnv = Path.Combine(current, "backend", ".env");
            if (File.Exists(backendEnv)) return backendEnv;

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return Path.Combine("C:\\Users\\Manasa\\OneDrive\\Desktop\\Projects\\docmind ai\\backend", ".env");
    }

    private static (string Endpoint, string ApiKey, string ChatModel, string EmbeddingModel) LoadAzureConfig()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                       ?? Environment.GetEnvironmentVariable("AzureOpenAi__Endpoint");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                     ?? Environment.GetEnvironmentVariable("AzureOpenAi__ApiKey");
        var chatModel = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT")
                        ?? Environment.GetEnvironmentVariable("AzureOpenAi__ChatDeploymentName");
        var embeddingModel = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")
                             ?? Environment.GetEnvironmentVariable("AzureOpenAi__EmbeddingDeploymentName");

        var envPath = FindEnvPath();
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;
                var parts = trimmed.Split('=', 2);
                var k = parts[0].Trim();
                var v = parts[1].Trim();
                if (k == "AZURE_OPENAI_ENDPOINT" && string.IsNullOrWhiteSpace(endpoint)) endpoint = v;
                if (k == "AZURE_OPENAI_API_KEY" && string.IsNullOrWhiteSpace(apiKey)) apiKey = v;
                if (k == "AZURE_OPENAI_CHAT_DEPLOYMENT" && string.IsNullOrWhiteSpace(chatModel)) chatModel = v;
                if (k == "AZURE_OPENAI_EMBEDDING_DEPLOYMENT" && string.IsNullOrWhiteSpace(embeddingModel)) embeddingModel = v;
            }
        }

        return (
            endpoint ?? "https://YOUR_RESOURCE_NAME.openai.azure.com/",
            apiKey ?? "",
            chatModel ?? "gpt-4.1-mini",
            embeddingModel ?? "text-embedding-3-small"
        );
    }

    private static string LoadMySqlPassword()
    {
        var pwd = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
        if (!string.IsNullOrWhiteSpace(pwd)) return pwd;

        var envPath = FindEnvPath();
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;
                var parts = trimmed.Split('=', 2);
                if (parts[0].Trim() == "MYSQL_PASSWORD") return parts[1].Trim();
            }
        }
        return "YOUR_PASSWORD_HERE";
    }

    [Fact]
    public async Task LiveAzureOpenAi_GenerateEmbedding_ShouldReturn1536Dimensions()
    {
        // 1. Arrange
        var config = LoadAzureConfig();
        config.ApiKey.Should().NotBeNullOrWhiteSpace("Azure OpenAI API Key must be configured in .env");

        var options = Options.Create(new AzureOpenAiOptions
        {
            Endpoint = config.Endpoint,
            ApiKey = config.ApiKey,
            ChatDeploymentName = config.ChatModel,
            EmbeddingDeploymentName = config.EmbeddingModel,
            EmbeddingDimensions = 1536
        });

        var embeddingService = new AzureOpenAiEmbeddingService(
            options,
            NullLogger<AzureOpenAiEmbeddingService>.Instance);

        // 2. Act
        var embedding = await embeddingService.GenerateEmbeddingAsync("DocuMind Phase 3 live embedding integration test");

        // 3. Assert
        embedding.Should().NotBeNull();
        embedding.Length.Should().Be(1536, "text-embedding-3-small must produce 1536 dimensions");
    }

    [Fact]
    public async Task LiveAzureOpenAi_ChatCompletion_ShouldSucceed()
    {
        var config = LoadAzureConfig();
        config.ApiKey.Should().NotBeNullOrWhiteSpace("Azure OpenAI API Key must be configured in .env");

        var azureClient = new AzureOpenAIClient(
            new Uri(config.Endpoint),
            new System.ClientModel.ApiKeyCredential(config.ApiKey));

        var chatClient = azureClient.GetChatClient(config.ChatModel);
        var messages = new OpenAI.Chat.ChatMessage[]
        {
            new OpenAI.Chat.SystemChatMessage("You are a test assistant. Answer with the phrase: DocuMind Verified"),
            new OpenAI.Chat.UserChatMessage("Hello, respond with the verification phrase.")
        };

        var response = await chatClient.CompleteChatAsync(messages);
        response.Value.Content.Should().NotBeEmpty();
        response.Value.Content[0].Text.Should().Contain("DocuMind Verified");
    }

    [Fact]
    public async Task LiveQdrant_Connection_And_CollectionManagement_ShouldSucceed()
    {
        // 1. Arrange: Qdrant gRPC is on port 6334
        var qdrantClient = new QdrantClient(host: "localhost", port: 6334);

        // 2. Act: Check health / list collections
        var collections = await qdrantClient.ListCollectionsAsync();
        collections.Should().NotBeNull();

        const string testCollection = "documind-chunks";
        const ulong vectorSize = 1536;

        var exists = await qdrantClient.CollectionExistsAsync(testCollection);
        if (!exists)
        {
            await qdrantClient.CreateCollectionAsync(
                testCollection,
                new VectorParams
                {
                    Size = vectorSize,
                    Distance = Distance.Cosine
                });

            await qdrantClient.CreatePayloadIndexAsync(
                testCollection,
                fieldName: "document_id",
                schemaType: PayloadSchemaType.Keyword);
        }

        var collectionInfo = await qdrantClient.GetCollectionInfoAsync(testCollection);
        collectionInfo.Should().NotBeNull();
        collectionInfo.Config.Params.VectorsConfig.Params.Size.Should().Be(vectorSize);
        collectionInfo.Config.Params.VectorsConfig.Params.Distance.Should().Be(Distance.Cosine);

        // 3. Upsert a temporary test vector
        var testPointId = Guid.NewGuid();
        var testDocumentId = Guid.NewGuid();
        var testVector = new float[1536];
        for (var i = 0; i < 1536; i++) testVector[i] = (float)(i % 10) / 10f;

        var point = new PointStruct
        {
            Id = testPointId,
            Vectors = testVector
        };
        point.Payload.Add("document_id", testDocumentId.ToString());
        point.Payload.Add("text", "This is a temporary integration test vector");
        point.Payload.Add("page_number", 1);
        point.Payload.Add("chunk_index", 0);

        var updateResult = await qdrantClient.UpsertAsync(testCollection, new[] { point });
        updateResult.Status.Should().Be(UpdateStatus.Completed);

        // 4. Query / Search for that vector
        var searchResults = await qdrantClient.QueryAsync(
            testCollection,
            query: testVector,
            filter: Conditions.MatchKeyword("document_id", testDocumentId.ToString()),
            limit: 1);

        searchResults.Should().NotBeNull();
        searchResults.Should().NotBeEmpty();
        searchResults[0].Id.Uuid.Should().Be(testPointId.ToString());
        searchResults[0].Payload["text"].StringValue.Should().Be("This is a temporary integration test vector");

        // 5. Delete the temporary test vector
        await qdrantClient.DeleteAsync(testCollection, new[] { testPointId });

        // 6. Confirm the collection remains available
        var existsAfterDelete = await qdrantClient.CollectionExistsAsync(testCollection);
        existsAfterDelete.Should().BeTrue();
    }

    [Fact]
    public async Task LiveQdrantVectorStore_FullServiceWorkflow_ShouldSucceed()
    {
        // 1. Arrange: Test the actual QdrantVectorStore service with standard options
        var options = Options.Create(new QdrantOptions
        {
            Endpoint = "http://localhost:6333",
            CollectionName = "documind-chunks",
            VectorSize = 1536
        });

        var vectorStore = new QdrantVectorStore(
            options,
            NullLogger<QdrantVectorStore>.Instance);

        // 2. Health check
        var isHealthy = await vectorStore.IsHealthyAsync();
        isHealthy.Should().BeTrue("Qdrant service must be healthy");

        // 3. Ensure collection
        await vectorStore.EnsureCollectionAsync("documind-chunks", 1536);

        // 4. Upsert a test point
        var docId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var dummyVector = new float[1536];
        for (var i = 0; i < 1536; i++) dummyVector[i] = 0.05f;

        var payload = new VectorPointPayload(
            documentId: docId,
            chunkId: chunkId,
            chunkIndex: 1,
            pageNumber: 2,
            content: "Service integration test text",
            tokenCount: 15);

        await vectorStore.UpsertVectorsAsync(
            "documind-chunks",
            new[] { (Id: chunkId, Vector: (ReadOnlyMemory<float>)dummyVector, Payload: payload) });

        // 5. Search with document filter
        var results = await vectorStore.SearchAsync(
            "documind-chunks",
            queryVector: dummyVector,
            documentIdFilter: docId,
            limit: 5);

        results.Should().NotBeEmpty();
        results[0].DocumentId.Should().Be(docId);
        results[0].ChunkId.Should().Be(chunkId);
        results[0].Content.Should().Be("Service integration test text");
        results[0].PageNumber.Should().Be(2);

        // 6. Delete document vectors
        await vectorStore.DeleteDocumentVectorsAsync("documind-chunks", docId);

        // 7. Verify deletion
        var afterDeleteResults = await vectorStore.SearchAsync(
            "documind-chunks",
            queryVector: dummyVector,
            documentIdFilter: docId,
            limit: 5);

        afterDeleteResults.Should().BeEmpty();
    }

    [Fact]
    public async Task LiveEndToEnd_RealPdf_Ingestion_RagRetrieval_And_StreamingChat_ShouldSucceed()
    {
        var config = LoadAzureConfig();
        config.ApiKey.Should().NotBeNullOrWhiteSpace("Azure OpenAI credentials must be configured");

        // 1. Arrange: Services using live MySQL, live Qdrant, and live Azure OpenAI
        var mysqlPassword = LoadMySqlPassword();
        var connectionString = $"Server=localhost;Port=3308;Database=documind_db;Uid=documind_user;Pwd={mysqlPassword};";
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        var dbOptions = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        await using var dbContext = new DocuMindDbContext(dbOptions);

        var qdrantOptions = Options.Create(new QdrantOptions
        {
            Endpoint = "http://localhost:6333",
            CollectionName = "documind-chunks",
            VectorSize = 1536
        });

        var azureOptions = Options.Create(new AzureOpenAiOptions
        {
            Endpoint = config.Endpoint,
            ApiKey = config.ApiKey,
            ChatDeploymentName = config.ChatModel,
            EmbeddingDeploymentName = config.EmbeddingModel,
            EmbeddingDimensions = 1536
        });

        var vectorStore = new QdrantVectorStore(qdrantOptions, NullLogger<QdrantVectorStore>.Instance);
        var embeddingService = new AzureOpenAiEmbeddingService(azureOptions, NullLogger<AzureOpenAiEmbeddingService>.Instance);
        var pdfExtractor = new PdfPigTextExtractor(NullLogger<PdfPigTextExtractor>.Instance);
        var textChunker = new TextChunker(NullLogger<TextChunker>.Instance);
        var chunkingOptions = Options.Create(new ChunkingOptions());
        var processingService = new DocumentProcessingService(
            dbContext,
            pdfExtractor,
            textChunker,
            embeddingService,
            vectorStore,
            qdrantOptions,
            chunkingOptions,
            NullLogger<DocumentProcessingService>.Instance);

        var tempStorageDir = Path.Combine(Path.GetTempPath(), $"documind_storage_{Guid.NewGuid():N}");
        var fileStorage = new LocalFileStorageService(NullLogger<LocalFileStorageService>.Instance, tempStorageDir);
        var documentService = new DocumentService(
            dbContext,
            fileStorage,
            processingService,
            vectorStore,
            qdrantOptions,
            NullLogger<DocumentService>.Instance);

        var conversationService = new ConversationService(dbContext, NullLogger<ConversationService>.Instance);
        var ragService = new RagRetrievalService(
            embeddingService,
            vectorStore,
            dbContext,
            qdrantOptions,
            NullLogger<RagRetrievalService>.Instance);

        var chatService = new AzureOpenAiChatService(
            ragService,
            conversationService,
            azureOptions,
            NullLogger<AzureOpenAiChatService>.Instance);

        // 2. Act: Generate a real 2-page PDF
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var p1 = builder.AddPage(PageSize.A4);
        p1.AddText("Project DocuMind Specification. The primary vector database engine used for chunk indexing is Qdrant. The embedding model is text-embedding-3-small producing 1536 dimensions.", 12, new PdfPoint(50, 700), font);

        var p2 = builder.AddPage(PageSize.A4);
        p2.AddText("DocuMind Performance Benchmarks. The maximum supported PDF file size is 20 megabytes. The default target chunk size is 1000 characters with a 200 character overlap.", 12, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();
        using var pdfStream = new MemoryStream(pdfBytes);

        // 3. Create test user and upload & ingest real PDF
        var testUser = new User("Integration Test User", $"integration_{Guid.NewGuid():N}@documind.test", "dummyhash");
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();
        var testUserId = testUser.Id;

        var uploadedDoc = await documentService.UploadDocumentAsync(pdfStream, "DocuMind_Spec.pdf", pdfBytes.Length, "application/pdf", testUserId);
        uploadedDoc.Should().NotBeNull();
        var docId = Guid.Parse(uploadedDoc.Id);

        try
        {
            // Verify document ready in DB
            var dbDoc = await dbContext.Documents.FirstOrDefaultAsync(d => d.Id == docId);
            dbDoc.Should().NotBeNull();
            dbDoc!.Status.Should().Be(Domain.Enums.DocumentStatus.Ready);
            dbDoc.PageCount.Should().Be(2);

            // 4. Test RAG Retrieval directly
            var retrievalResult = await ragService.RetrieveContextAsync(docId, "What is the primary vector database engine?", testUserId);
            retrievalResult.HasContext.Should().BeTrue();
            retrievalResult.Citations.Should().NotBeEmpty();
            retrievalResult.Citations[0].Page.Should().Be(1);
            retrievalResult.Citations[0].Snippet.Should().Contain("Qdrant");

            // 5. Test Live Streaming Chat (SSE)
            var sseEvents = new List<SseStreamEvent>();
            await foreach (var evt in chatService.StreamQuestionAsync(docId, "What is the primary vector database engine used by DocuMind?", testUserId))
            {
                sseEvents.Add(evt);
            }

            // Verify SSE event order: sources -> token... -> done
            sseEvents.Should().NotBeEmpty();
            sseEvents[0].Type.Should().Be("sources");
            sseEvents[0].Sources.Should().NotBeEmpty();
            sseEvents[0].Sources![0].Page.Should().Be(1);

            var fullAnswer = string.Concat(sseEvents.Where(e => e.Type == "token").Select(e => e.Content));
            fullAnswer.Should().Contain("Qdrant");
            fullAnswer.Should().Contain("[Page 1]");

            sseEvents.Last().Type.Should().Be("done");

            // 6. Test Out-of-Context Question (Grounded refusal)
            var refusalResponse = await chatService.AskQuestionAsync(docId, "What is the secret recipe for Coca-Cola?", testUserId);
            refusalResponse.Answer.Trim().Should().Be("I couldn't find that information in this document.");

            // 7. Verify Conversation Persistence
            var convs = await conversationService.GetConversationsByDocumentAsync(docId, testUserId);
            convs.Should().NotBeEmpty();
            var messages = await conversationService.GetMessagesAsync(convs[0].Id, testUserId);
            messages.Should().NotBeEmpty();
            messages.Any(m => m.Role == "user").Should().BeTrue();
            messages.Any(m => m.Role == "assistant").Should().BeTrue();
        }
        finally
        {
            // 8. Clean up: Delete test document and storage
            await documentService.DeleteDocumentAsync(docId, testUserId);

            if (Directory.Exists(tempStorageDir))
            {
                try { Directory.Delete(tempStorageDir, true); } catch { }
            }
        }
    }
}
