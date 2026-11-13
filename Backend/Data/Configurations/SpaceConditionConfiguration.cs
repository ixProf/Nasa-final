using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class SpaceConditionConfiguration : IEntityTypeConfiguration<SpaceCondition>
{
    public void Configure(EntityTypeBuilder<SpaceCondition> builder)
    {
        builder.ToTable("SpaceConditions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.SelfCareGuidance)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.WhenToContactMonitor)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.SourceReference)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);
    }
}

