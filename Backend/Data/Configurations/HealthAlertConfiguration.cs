using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class HealthAlertConfiguration : IEntityTypeConfiguration<HealthAlert>
{
    public void Configure(EntityTypeBuilder<HealthAlert> builder)
    {
        builder.ToTable("HealthAlerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.ActionRequired)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.AcknowledgedBy)
            .HasMaxLength(200);

        builder.Property(a => a.ActionTaken)
            .HasMaxLength(1000);

        // Index for dashboard query (active alerts for an astronaut by date)
        builder.HasIndex(a => new { a.AstronautId, a.Status, a.TriggeredAt });

        builder.HasOne(a => a.Astronaut)
            .WithMany(ast => ast.HealthAlerts)
            .HasForeignKey(a => a.AstronautId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.VitalSignReading)
            .WithMany(r => r.HealthAlerts)
            .HasForeignKey(a => a.VitalSignReadingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.EmergencyProcedure)
            .WithMany(e => e.HealthAlerts)
            .HasForeignKey(a => a.EmergencyProcedureId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

