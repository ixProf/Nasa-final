using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class AstronautConfiguration : IEntityTypeConfiguration<Astronaut>
{
    public void Configure(EntityTypeBuilder<Astronaut> builder)
    {
        builder.ToTable("Astronauts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AstronautCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.AstronautCode)
            .IsUnique();

        builder.Property(a => a.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(200);

        // Index on FullName for fast lookups (queried with ILike in PostgreSQL)
        builder.HasIndex(a => a.FullName);

        builder.Property(a => a.UserId)
            .HasMaxLength(450);

        // One-to-zero-or-one relationship: An ApplicationUser can be linked to at most one Astronaut.
        // DeleteBehavior.SetNull ensures medical telemetry is never lost if an Identity user account is deleted.
        builder.HasOne(a => a.User)
            .WithOne(u => u.Astronaut)
            .HasForeignKey<Astronaut>(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique filtered index: Ensures non-null UserIds cannot be claimed by multiple astronauts.
        builder.HasIndex(a => a.UserId)
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");

        builder.Property(a => a.Gender)
            .HasMaxLength(20);

        builder.Property(a => a.BloodType)
            .HasMaxLength(10);

        builder.Property(a => a.EmergencyContactInfo)
            .HasMaxLength(500);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationship: Astronaut -> Mission (Nullable, SetNull on delete)
        builder.HasOne(a => a.Mission)
            .WithMany(m => m.Astronauts)
            .HasForeignKey(a => a.MissionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

