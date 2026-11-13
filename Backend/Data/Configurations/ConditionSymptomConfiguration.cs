using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class ConditionSymptomConfiguration : IEntityTypeConfiguration<ConditionSymptom>
{
    public void Configure(EntityTypeBuilder<ConditionSymptom> builder)
    {
        builder.ToTable("ConditionSymptoms");

        builder.HasKey(cs => cs.Id);

        builder.HasIndex(cs => new { cs.ConditionId, cs.SymptomId })
            .IsUnique();

        builder.HasOne(cs => cs.Condition)
            .WithMany(c => c.ConditionSymptoms)
            .HasForeignKey(cs => cs.ConditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cs => cs.Symptom)
            .WithMany(s => s.ConditionSymptoms)
            .HasForeignKey(cs => cs.SymptomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

