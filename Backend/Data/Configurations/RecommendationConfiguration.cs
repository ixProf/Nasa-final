using AstronautHealthMonitoring.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstronautHealthMonitoring.Data.Configurations;

public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(r => r.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(r => new { r.AstronautId, r.Status });

        builder.HasOne(r => r.Astronaut)
            .WithMany(a => a.Recommendations)
            .HasForeignKey(r => r.AstronautId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

