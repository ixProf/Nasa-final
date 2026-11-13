using AstronautHealthMonitoring.Data.Seed;
using AstronautHealthMonitoring.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Astronaut> Astronauts => Set<Astronaut>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<VitalSignType> VitalSignTypes => Set<VitalSignType>();
    public DbSet<VitalSignReading> VitalSignReadings => Set<VitalSignReading>();
    public DbSet<ReferenceRange> ReferenceRanges => Set<ReferenceRange>();
    public DbSet<HealthMetricGuide> HealthMetricGuides => Set<HealthMetricGuide>();
    public DbSet<HealthAlert> HealthAlerts => Set<HealthAlert>();
    public DbSet<SleepRecord> SleepRecords => Set<SleepRecord>();
    public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<HabitCompletion> HabitCompletions => Set<HabitCompletion>();
    public DbSet<AssignmentReminderSetting> AssignmentReminderSettings => Set<AssignmentReminderSetting>();
    public DbSet<AssignmentReminderDelivery> AssignmentReminderDeliveries => Set<AssignmentReminderDelivery>();
    public DbSet<EmergencyProcedure> EmergencyProcedures => Set<EmergencyProcedure>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Symptom> Symptoms => Set<Symptom>();
    public DbSet<SpaceCondition> SpaceConditions => Set<SpaceCondition>();
    public DbSet<ConditionSymptom> ConditionSymptoms => Set<ConditionSymptom>();
    public DbSet<ConditionVitalSign> ConditionVitalSigns => Set<ConditionVitalSign>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AssignmentReminderSetting>(entity =>
        {
            entity.HasKey(s => s.RecommendationId);
            entity.HasOne(s => s.Recommendation).WithMany().HasForeignKey(s => s.RecommendationId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(s => s.Revision).IsConcurrencyToken();
            entity.HasIndex(s => new { s.Enabled, s.NextDueAt });
        });
        modelBuilder.Entity<AssignmentReminderDelivery>(entity =>
        {
            entity.HasKey(s => s.RecommendationId);
            entity.HasOne(s => s.Recommendation).WithMany().HasForeignKey(s => s.RecommendationId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(s => s.SenderName).HasMaxLength(255);
            entity.Property(s => s.Token).IsConcurrencyToken();
            entity.HasIndex(s => s.Token).IsUnique();
        });
        modelBuilder.Entity<HabitCompletion>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.RecommendationId, c.CompletedOn }).IsUnique();
            entity.HasIndex(c => new { c.AstronautId, c.CompletedOn });
            entity.HasOne(c => c.Recommendation).WithMany().HasForeignKey(c => c.RecommendationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Recommendation>().Property(r => r.TitleAr).HasMaxLength(200);
        modelBuilder.Entity<Recommendation>().Property(r => r.ContentAr).HasMaxLength(2000);
        modelBuilder.Entity<Recommendation>().Property(r => r.ReasonAr).HasMaxLength(1000);
        modelBuilder.Entity<Recommendation>().Property(r => r.ScheduledTime).HasMaxLength(5);
        modelBuilder.Entity<Recommendation>().Property(r => r.PrescribedBy).HasMaxLength(255);

        // Apply all 12 entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Seed static catalogs, guides, baseline ranges, and emergency procedures
        modelBuilder.Seed();
    }
}

