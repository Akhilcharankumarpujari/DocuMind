using DocuMind.Application.Common.Interfaces;
using DocuMind.Domain.Common;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class DocuMindDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTimeProvider? _dateTimeProvider;

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DocuMindDbContext(DbContextOptions<DocuMindDbContext> options) : base(options)
    {
    }

    public DocuMindDbContext(DbContextOptions<DocuMindDbContext> options, IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocuMindDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<DocumentChunk>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ChatMessage>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
