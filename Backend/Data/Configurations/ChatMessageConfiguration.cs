using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(c => c.Id);

        // Offline-sync idempotency key: unique index
        builder.Property(c => c.ClientGeneratedId)
            .IsRequired();

        builder.HasIndex(c => c.ClientGeneratedId)
            .IsUnique();

        builder.Property(c => c.SenderUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(c => c.SenderRole)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(c => c.RecordedAt)
            .IsRequired();

        builder.Property(c => c.SentAt)
            .IsRequired();

        // Index for loading chat history for an astronaut thread ordered by SentAt
        builder.HasIndex(c => new { c.AstronautId, c.SentAt });

        builder.HasOne(c => c.Astronaut)
            .WithMany(a => a.ChatMessages)
            .HasForeignKey(c => c.AstronautId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

