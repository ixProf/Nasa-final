using Mapster;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class RecommendationMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Recommendation, RecommendationDto>()
            .Map(dest => dest.AstronautFullName, src => src.Astronaut != null ? src.Astronaut.FullName : string.Empty);

        config.NewConfig<CreateRecommendationRequest, Recommendation>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Astronaut)
            .Ignore(dest => dest.GeneratedAt)
            .Ignore(dest => dest.Status);
    }
}
