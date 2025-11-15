using Chatty.BE.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatty.BE.Infrastructure.Persistence.Configurations;

public class ConversationParticipantConfiguration
    : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants");

        // 1 user không nên có 2 record trùng ConversationId
        builder.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();

        builder.Property(x => x.IsAdmin).IsRequired();
        builder.Property(x => x.JoinedAt).IsRequired();

        builder
            .HasOne(x => x.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.User)
            .WithMany(u => u.ConversationParticipants)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
