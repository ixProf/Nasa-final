using Mapster;
using AstronautHealthMonitoring.DTOs.ReferenceRanges;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class ReferenceRangeMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ReferenceRange, ReferenceRangeDto>()
            .Map(dest => dest.VitalSignTypeCode, src => src.VitalSignType != null ? src.VitalSignType.Code : string.Empty);

        config.NewConfig<UpdateReferenceRangeRequest, ReferenceRange>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.VitalSignTypeId)
            .Ignore(dest => dest.VitalSignType)
            .Ignore(dest => dest.ApplicableGender)
            .Ignore(dest => dest.MinAge)
            .Ignore(dest => dest.MaxAge)
            .Ignore(dest => dest.ReviewedBy)
            .Ignore(dest => dest.ReviewedAt);
    }
}
