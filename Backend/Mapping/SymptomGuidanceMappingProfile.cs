using Mapster;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class SymptomGuidanceMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Symptom, SymptomDto>();

        config.NewConfig<ConditionSymptom, ConditionSymptomDto>()
            .Map(dest => dest.SymptomCode, src => src.Symptom != null ? src.Symptom.Code : string.Empty)
            .Map(dest => dest.SymptomName, src => src.Symptom != null ? src.Symptom.Name : string.Empty)
            .Map(dest => dest.IsRedFlag, src => src.Symptom != null && src.Symptom.IsRedFlag);

        config.NewConfig<ConditionVitalSign, ConditionVitalSignDto>()
            .Map(dest => dest.VitalSignTypeCode, src => src.VitalSignType != null ? src.VitalSignType.Code : string.Empty)
            .Map(dest => dest.VitalSignTypeName, src => src.VitalSignType != null ? src.VitalSignType.Name : string.Empty);

        config.NewConfig<SpaceCondition, SpaceConditionDto>()
            .Map(dest => dest.AssociatedSymptoms, src => src.ConditionSymptoms)
            .Map(dest => dest.AssociatedVitalSigns, src => src.ConditionVitalSigns);
    }
}
