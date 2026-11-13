using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class ConditionVitalSignConfiguration : IEntityTypeConfiguration<ConditionVitalSign>
{
    public void Configure(EntityTypeBuilder<ConditionVitalSign> builder)
    {
        builder.ToTable("ConditionVitalSigns");

        builder.HasKey(cv => cv.Id);

        builder.Property(cv => cv.Direction)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(cv => cv.ContextNote)
            .HasMaxLength(500);

        builder.HasIndex(cv => new { cv.ConditionId, cv.VitalSignTypeId });

        builder.HasOne(cv => cv.Condition)
            .WithMany(c => c.ConditionVitalSigns)
            .HasForeignKey(cv => cv.ConditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cv => cv.VitalSignType)
            .WithMany(v => v.ConditionVitalSigns)
            .HasForeignKey(cv => cv.VitalSignTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

