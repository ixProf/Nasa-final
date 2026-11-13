using Mapster;
using AstronautHealthMonitoring.DTOs.Emergency;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class EmergencyMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EmergencyProcedure, EmergencyProcedureDto>();
    }
}
