using Mapster;
using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class AstronautMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Astronaut, AstronautDto>()
            .Map(dest => dest.MissionName, src => src.Mission != null ? src.Mission.Name : null);

        config.NewConfig<CreateAstronautRequest, Astronaut>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}".Trim())
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.IsActive)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Mission)
            .Ignore(dest => dest.VitalSignReadings)
            .Ignore(dest => dest.SleepRecords)
            .Ignore(dest => dest.MoodEntries)
            .Ignore(dest => dest.Recommendations)
            .Ignore(dest => dest.HealthAlerts)
            .Ignore(dest => dest.ChatMessages);

        config.NewConfig<UpdateAstronautRequest, Astronaut>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}".Trim())
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.AstronautCode)
            .Ignore(dest => dest.IsActive)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Mission)
            .Ignore(dest => dest.VitalSignReadings)
            .Ignore(dest => dest.SleepRecords)
            .Ignore(dest => dest.MoodEntries)
            .Ignore(dest => dest.Recommendations)
            .Ignore(dest => dest.HealthAlerts)
            .Ignore(dest => dest.ChatMessages);
    }
}
