using Mapster;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class AlertMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<HealthAlert, HealthAlertDto>()
            .Map(dest => dest.AstronautFullName, src => src.Astronaut != null ? src.Astronaut.FullName : string.Empty)
            .Map(dest => dest.VitalSignTypeId, src => src.VitalSignReading != null ? (int?)src.VitalSignReading.VitalSignTypeId : null)
            .Map(dest => dest.VitalSignTypeCode, src => src.VitalSignReading != null && src.VitalSignReading.VitalSignType != null ? src.VitalSignReading.VitalSignType.Code : null)
            .Map(dest => dest.EmergencyProcedureCode, src => src.EmergencyProcedure != null ? src.EmergencyProcedure.Code : null)
            .Map(dest => dest.EmergencyProcedureTitle, src => src.EmergencyProcedure != null ? src.EmergencyProcedure.Title : null);
    }
}
