using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class HealthMetricGuideConfiguration : IEntityTypeConfiguration<HealthMetricGuide>
{
    public void Configure(EntityTypeBuilder<HealthMetricGuide> builder)
    {
        builder.ToTable("HealthMetricGuides");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.WhatIsIt)
            .IsRequired()
            .HasMaxLength(1500);

        builder.Property(g => g.WhyItMatters)
            .IsRequired()
            .HasMaxLength(1500);

        builder.Property(g => g.HowToMeasure)
            .IsRequired()
            .HasMaxLength(1500);

        builder.Property(g => g.InterpretationGuidance)
            .IsRequired()
            .HasMaxLength(1500);

        builder.Property(g => g.ActionGuidance)
            .IsRequired()
            .HasMaxLength(1500);
    }
}

