using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the DocuMind persistence context.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<ChatConversation> ChatConversations { get; }
    DbSet<ChatMessage> ChatMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
