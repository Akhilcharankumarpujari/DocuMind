using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("chat_conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.DocumentId)
            .IsRequired();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue("New Conversation");

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime(6)");

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnType("datetime(6)");

        // Index on DocumentId + CreatedAtUtc
        builder.HasIndex(c => new { c.DocumentId, c.CreatedAtUtc })
            .HasDatabaseName("ix_chat_conversations_document_id_created_at_utc");

        // Relationships
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
