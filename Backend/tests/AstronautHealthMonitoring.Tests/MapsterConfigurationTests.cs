using Mapster;
using MapsterMapper;
using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class MapsterConfigurationTests
{
    [Fact]
    public void Mapster_Configuration_IsValid_And_Compiles()
    {
        var config = new TypeAdapterConfig();
        config.RequireDestinationMemberSource = true;
        config.RequireExplicitMapping = true;

        config.Scan(typeof(AstronautMappings).Assembly);

        // Compile must not throw
        var exception = Record.Exception(() => config.Compile());
        Assert.Null(exception);
    }

    [Fact]
    public void Map_Astronaut_To_AstronautDto_Flattens_FullName_And_MissionName()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(AstronautMappings).Assembly);
        var mapper = new ServiceMapper(null!, config);

        var astronaut = new Astronaut
        {
            Id = 1,
            AstronautCode = "AST-001",
            FirstName = "Alex",
            LastName = "Vance",
            FullName = "Alex Vance",
            Mission = new Mission
            {
                Id = 10,
                Code = "ARES-III",
                Name = "Ares III Expedition"
            }
        };

        var dto = mapper.Map<AstronautDto>(astronaut);

        Assert.Equal(astronaut.Id, dto.Id);
        Assert.Equal("Alex Vance", dto.FullName);
        Assert.Equal("Ares III Expedition", dto.MissionName);
    }

    [Fact]
    public void Map_MoodEntry_To_MoodEntryDto_Copies_All_Domain_Fields()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(MoodMappings).Assembly);
        var mapper = new ServiceMapper(null!, config);

        var clientGuid = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var entry = new MoodEntry
        {
            Id = 5,
            ClientGeneratedId = clientGuid,
            AstronautId = 2,
            RecordedAt = now,
            MoodScore = 8,
            StressLevel = 3,
            DominantEmotion = "Calm",
            Notes = "Feeling focused."
        };

        var dto = mapper.Map<MoodEntryDto>(entry);

        Assert.Equal(5, dto.Id);
        Assert.Equal(clientGuid, dto.ClientGeneratedId);
        Assert.Equal(2, dto.AstronautId);
        Assert.Equal(now, dto.RecordedAt);
        Assert.Equal(8, dto.MoodScore);
        Assert.Equal(3, dto.StressLevel);
        Assert.Equal("Calm", dto.DominantEmotion);
        Assert.Equal("Feeling focused.", dto.Notes);
    }

    [Fact]
    public void Map_CreateSleepRecordRequest_To_SleepRecord_Copies_ClientData_And_Leaves_ServerFields_Unset()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(SleepMappings).Assembly);
        var mapper = new ServiceMapper(null!, config);

        var clientGuid = Guid.NewGuid();
        var recordedAt = DateTime.UtcNow;
        var start = recordedAt.AddHours(-8);
        var end = recordedAt;

        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = clientGuid,
            RecordedAt = recordedAt,
            StartTime = start,
            EndTime = end,
            Quality = SleepQuality.Good,
            DeepSleepMinutes = 120,
            RemSleepMinutes = 90,
            LightSleepMinutes = 240,
            AwakeMinutes = 30,
            SleepEfficiencyPercentage = 93.75,
            Notes = "Deep restful sleep."
        };

        var entity = mapper.Map<SleepRecord>(request);

        // Client-sent data copied
        Assert.Equal(clientGuid, entity.ClientGeneratedId);
        Assert.Equal(0, entity.AstronautId); // Set later by service from JWT parameter
        Assert.Equal(recordedAt, entity.RecordedAt);
        Assert.Equal(start, entity.StartTime);
        Assert.Equal(end, entity.EndTime);
        Assert.Equal(SleepQuality.Good, entity.Quality);
        Assert.Equal(120, entity.DeepSleepMinutes);
        Assert.Equal(90, entity.RemSleepMinutes);
        Assert.Equal(240, entity.LightSleepMinutes);
        Assert.Equal(30, entity.AwakeMinutes);
        Assert.Equal(93.75, entity.SleepEfficiencyPercentage);
        Assert.Equal("Deep restful sleep.", entity.Notes);

        // Server-set members not set from request
        Assert.Equal(0, entity.Id);
        Assert.Equal(default, entity.UpdatedAt);
        Assert.Equal(0.0, entity.DurationHours); // Computed later by service
        Assert.Null(entity.Astronaut);
    }

    [Fact]
    public void Map_HealthAlert_To_HealthAlertDto_Flattens_EmergencyProcedure()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(AlertMappings).Assembly);
        var mapper = new ServiceMapper(null!, config);

        var alert = new HealthAlert
        {
            Id = 12,
            AstronautId = 4,
            Severity = AlertSeverity.Emergency,
            Title = "Severe Hypoxia",
            Message = "SpO2 dropped below critical threshold",
            Reason = "SpO2 < 85%",
            ActionRequired = "Apply supplemental oxygen immediately",
            EmergencyProcedure = new EmergencyProcedure
            {
                Id = 99,
                Code = "EP-HYPOXIA-01",
                Title = "Acute Hypoxia Countermeasure Protocol"
            }
        };

        var dto = mapper.Map<HealthAlertDto>(alert);

        Assert.Equal(12, dto.Id);
        Assert.Equal("EP-HYPOXIA-01", dto.EmergencyProcedureCode);
        Assert.Equal("Acute Hypoxia Countermeasure Protocol", dto.EmergencyProcedureTitle);
    }
}

