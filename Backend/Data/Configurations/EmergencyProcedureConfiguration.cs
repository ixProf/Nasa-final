using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class EmergencyProcedureConfiguration : IEntityTypeConfiguration<EmergencyProcedure>
{
    public void Configure(EntityTypeBuilder<EmergencyProcedure> builder)
    {
        builder.ToTable("EmergencyProcedures");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.TargetCondition)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ImmediateAction)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.StepByStepInstructions)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(e => e.RequiredEquipment)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.GuidanceNotes)
            .HasMaxLength(1000);
    }
}

