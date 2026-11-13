using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class VitalSignTypeConfiguration : IEntityTypeConfiguration<VitalSignType>
{
    public void Configure(EntityTypeBuilder<VitalSignType> builder)
    {
        builder.ToTable("VitalSignTypes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(v => v.Code)
            .IsUnique();

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.Description)
            .HasMaxLength(500);

        // 1-to-1: VitalSignType -> HealthMetricGuide
        builder.HasOne(v => v.HealthMetricGuide)
            .WithOne(g => g.VitalSignType)
            .HasForeignKey<HealthMetricGuide>(g => g.VitalSignTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1-to-many: VitalSignType -> ReferenceRanges
        builder.HasMany(v => v.ReferenceRanges)
            .WithOne(r => r.VitalSignType)
            .HasForeignKey(r => r.VitalSignTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1-to-many: VitalSignType -> VitalSignReadings (Restrict deletion if readings exist)
        builder.HasMany(v => v.VitalSignReadings)
            .WithOne(r => r.VitalSignType)
            .HasForeignKey(r => r.VitalSignTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

