using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class VitalSignReadingConfiguration : IEntityTypeConfiguration<VitalSignReading>
{
    public void Configure(EntityTypeBuilder<VitalSignReading> builder)
    {
        builder.ToTable("VitalSignReadings");

        builder.HasKey(r => r.Id);

        // Offline-sync idempotency key: unique index
        builder.Property(r => r.ClientGeneratedId)
            .IsRequired();

        builder.HasIndex(r => r.ClientGeneratedId)
            .IsUnique();

        builder.Property(r => r.RecordedAt)
            .IsRequired();

        // Index for time-series range queries per astronaut
        builder.HasIndex(r => new { r.AstronautId, r.RecordedAt });

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.BodyPosition)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.ActivityContext)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.DataQuality)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.MealContext)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.MeasurementMethod)
            .HasMaxLength(100);

        builder.Property(r => r.SensorLocation)
            .HasMaxLength(100);

        builder.Property(r => r.DeviceId)
            .HasMaxLength(100);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.HasOne(r => r.Astronaut)
            .WithMany(a => a.VitalSignReadings)
            .HasForeignKey(r => r.AstronautId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

