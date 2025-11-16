using Chatty.BE.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatty.BE.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(256);
        builder.Property(t => t.CreatedByIp).HasMaxLength(128);
        builder.Property(t => t.RevokedByIp).HasMaxLength(128);
        builder.Property(t => t.ReasonRevoked).HasMaxLength(256);

        builder
            .HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(t => t.ReplacedByToken)
            .WithMany()
            .HasForeignKey(t => t.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
