using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Infrastructure.Ai;
using DocuMind.Infrastructure.Configuration;
using DocuMind.Infrastructure.Pdf;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Services;
using DocuMind.Infrastructure.Storage;
using DocuMind.Infrastructure.TextChunking;
using DocuMind.Infrastructure.VectorDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind Options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));
        services.Configure<AzureOpenAiOptions>(configuration.GetSection(AzureOpenAiOptions.SectionName));
        services.Configure<ChunkingOptions>(configuration.GetSection(ChunkingOptions.SectionName));

        // Core Infrastructure Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Phase 3 Ingestion & Vector Services
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddSingleton<IEmbeddingService, AzureOpenAiEmbeddingService>();
        services.AddSingleton<IVectorStore, QdrantVectorStore>();
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

        // Phase 4 RAG, Chat, Storage & Document Services
        services.AddSingleton<IFileStorageService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalFileStorageService>>();
            var storagePath = configuration["Storage:Path"] ?? configuration["STORAGE_PATH"];
            return new LocalFileStorageService(logger, storagePath);
        });
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IRagRetrievalService, RagRetrievalService>();
        services.AddScoped<IChatService, AzureOpenAiChatService>();
        services.AddScoped<IDocumentService, DocumentService>();

        // EF Core MySQL Registration
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? configuration[$"{DatabaseOptions.SectionName}:ConnectionString"]
                               ?? throw new InvalidOperationException("MySQL connection string was not configured.");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        services.AddDbContext<DocuMindDbContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
            {
                mySqlOptions.MigrationsAssembly(typeof(DocuMindDbContext).Assembly.FullName);
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<DocuMindDbContext>());

        return services;
    }
}
