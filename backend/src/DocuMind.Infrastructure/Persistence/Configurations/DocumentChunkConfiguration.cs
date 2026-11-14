using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.DocumentId)
            .IsRequired();

        builder.Property(c => c.PageNumber)
            .IsRequired();

        builder.Property(c => c.ChunkIndex)
            .IsRequired();

        builder.Property(c => c.Content)
            .IsRequired()
            .HasColumnType("longtext");

        builder.Property(c => c.TokenCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.QdrantPointId)
            .HasMaxLength(36);

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime(6)");

        // Composite Indexes
        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("ix_document_chunks_document_id_chunk_index");

        builder.HasIndex(c => new { c.DocumentId, c.PageNumber })
            .HasDatabaseName("ix_document_chunks_document_id_page_number");

        builder.HasIndex(c => c.QdrantPointId)
            .HasDatabaseName("ix_document_chunks_qdrant_point_id");
    }
}
