using Mapster;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class VitalSignMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<VitalSignReading, VitalSignReadingDto>()
            .Map(dest => dest.AstronautFullName, src => src.Astronaut != null ? src.Astronaut.FullName : string.Empty)
            .Map(dest => dest.VitalSignTypeCode, src => src.VitalSignType != null ? src.VitalSignType.Code : string.Empty)
            .Map(dest => dest.VitalSignTypeName, src => src.VitalSignType != null ? src.VitalSignType.Name : string.Empty)
            .Map(dest => dest.Unit, src => src.VitalSignType != null ? src.VitalSignType.Unit : string.Empty);

        config.NewConfig<CreateVitalSignReadingRequest, VitalSignReading>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.AstronautId)
            .Ignore(dest => dest.Astronaut)
            .Ignore(dest => dest.VitalSignType)
            .Ignore(dest => dest.Status)
            .Ignore(dest => dest.HealthAlerts);

        config.NewConfig<VitalSignType, VitalSignTypeDto>();
    }
}
