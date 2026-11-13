using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class SymptomConfiguration : IEntityTypeConfiguration<Symptom>
{
    public void Configure(EntityTypeBuilder<Symptom> builder)
    {
        builder.ToTable("Symptoms");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.BodyArea)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.IsRedFlag)
            .IsRequired();
    }
}

