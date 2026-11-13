using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class SleepRecordConfiguration : IEntityTypeConfiguration<SleepRecord>
{
    public void Configure(EntityTypeBuilder<SleepRecord> builder)
    {
        builder.ToTable("SleepRecords");

        builder.HasKey(s => s.Id);

        // Offline-sync idempotency key: unique index
        builder.Property(s => s.ClientGeneratedId)
            .IsRequired();

        builder.HasIndex(s => s.ClientGeneratedId)
            .IsUnique();

        builder.Property(s => s.RecordedAt)
            .IsRequired();

        builder.HasIndex(s => new { s.AstronautId, s.RecordedAt });

        builder.Property(s => s.Quality)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.HasOne(s => s.Astronaut)
            .WithMany(a => a.SleepRecords)
            .HasForeignKey(s => s.AstronautId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

