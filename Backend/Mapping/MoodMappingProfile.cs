using Mapster;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class MoodMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MoodEntry, MoodEntryDto>()
            .Map(dest => dest.AstronautFullName, src => src.Astronaut != null ? src.Astronaut.FullName : string.Empty);

        config.NewConfig<CreateMoodEntryRequest, MoodEntry>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.AstronautId)
            .Ignore(dest => dest.Astronaut);
    }
}
