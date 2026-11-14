using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("longtext");

        builder.Property(m => m.SourcesJson)
            .HasColumnType("longtext");

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime(6)");

        // Composite Index for message sequence inside a conversation
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAtUtc })
            .HasDatabaseName("ix_chat_messages_conversation_id_created_at_utc");
    }
}
