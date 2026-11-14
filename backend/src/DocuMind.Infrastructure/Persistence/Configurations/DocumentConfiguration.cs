using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("application/pdf");

        builder.Property(d => d.FileSizeBytes)
            .IsRequired();

        builder.Property(d => d.PageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(d => d.StoragePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.FileHash)
            .HasMaxLength(64);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.ErrorMessage)
            .HasColumnType("text");

        builder.Property(d => d.Summary)
            .HasColumnType("longtext");

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime(6)");

        builder.Property(d => d.UpdatedAtUtc)
            .HasColumnType("datetime(6)");

        // Indexes for query performance
        builder.HasIndex(d => d.FileName)
            .HasDatabaseName("ix_documents_file_name");

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("ix_documents_status");

        builder.HasIndex(d => d.CreatedAtUtc)
            .HasDatabaseName("ix_documents_created_at_utc");

        builder.HasIndex(d => d.FileHash)
            .HasDatabaseName("ix_documents_file_hash");

        // Relationships
        builder.HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Conversations)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
