using Mapster;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class SleepMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SleepRecord, SleepRecordDto>()
            .Map(dest => dest.AstronautFullName, src => src.Astronaut != null ? src.Astronaut.FullName : string.Empty);

        config.NewConfig<CreateSleepRecordRequest, SleepRecord>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.AstronautId)
            .Ignore(dest => dest.Astronaut)
            .Ignore(dest => dest.DurationHours);
    }
}
