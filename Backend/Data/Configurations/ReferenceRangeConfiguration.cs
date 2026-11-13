using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class ReferenceRangeConfiguration : IEntityTypeConfiguration<ReferenceRange>
{
    public void Configure(EntityTypeBuilder<ReferenceRange> builder)
    {
        builder.ToTable("ReferenceRanges");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Source)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.ReviewedBy)
            .HasMaxLength(200);

        builder.Property(r => r.ApplicableGender)
            .HasMaxLength(20);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(r => new { r.VitalSignTypeId, r.Scope, r.IsActive });
    }
}

