using Mapster;
using AstronautHealthMonitoring.DTOs.Guides;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class GuideMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<HealthMetricGuide, HealthMetricGuideDto>()
            .Map(dest => dest.VitalSignTypeCode, src => src.VitalSignType != null ? src.VitalSignType.Code : string.Empty);

        config.NewConfig<UpdateHealthMetricGuideRequest, HealthMetricGuide>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.VitalSignTypeId)
            .Ignore(dest => dest.VitalSignType);
    }
}
