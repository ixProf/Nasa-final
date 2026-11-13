using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class MoodEntryConfiguration : IEntityTypeConfiguration<MoodEntry>
{
    public void Configure(EntityTypeBuilder<MoodEntry> builder)
    {
        builder.ToTable("MoodEntries");

        builder.HasKey(m => m.Id);
        builder.Property(m=>m.MoodScaleMaximum).HasDefaultValue(10);

        // Offline-sync idempotency key: unique index
        builder.Property(m => m.ClientGeneratedId)
            .IsRequired();

        builder.HasIndex(m => m.ClientGeneratedId)
            .IsUnique();

        builder.Property(m => m.RecordedAt)
            .IsRequired();

        builder.HasIndex(m => new { m.AstronautId, m.RecordedAt });

        builder.Property(m => m.DominantEmotion)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Notes)
            .HasMaxLength(1000);

        builder.HasOne(m => m.Astronaut)
            .WithMany(a => a.MoodEntries)
            .HasForeignKey(m => m.AstronautId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

