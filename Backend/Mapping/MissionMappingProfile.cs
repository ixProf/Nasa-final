using Mapster;
using AstronautHealthMonitoring.DTOs.Missions;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class MissionMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Mission, MissionDto>()
            .Map(dest => dest.AstronautsCount, src => src.Astronauts.Count);

        config.NewConfig<CreateMissionRequest, Mission>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Astronauts);

        config.NewConfig<UpdateMissionRequest, Mission>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Code)
            .Ignore(dest => dest.Astronauts);
    }
}
