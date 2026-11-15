using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.ExpiresAtUtc)
            .IsRequired()
            .HasColumnType("datetime(6)");

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime(6)");

        builder.Property(t => t.RevokedAtUtc)
            .HasColumnType("datetime(6)");

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_token_hash");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");

        builder.HasIndex(t => t.ExpiresAtUtc)
            .HasDatabaseName("ix_refresh_tokens_expires_at_utc");

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
