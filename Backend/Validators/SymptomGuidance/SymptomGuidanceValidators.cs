using FluentValidation;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;

namespace AstronautHealthMonitoring.Validators.SymptomGuidance;

public class EvaluateSymptomsRequestValidator : AbstractValidator<EvaluateSymptomsRequest>
{
    public EvaluateSymptomsRequestValidator()
    {
        RuleFor(x=>x.Locale).Must(locale=>locale is "ar" or "en");
        RuleFor(x=>x.SymptomDetails).NotNull();
        RuleFor(x=>x.SymptomDetails).Must(details=>details is null || details.Count<=32);
        RuleFor(x=>x).Must(request=>request.SymptomDetails is null || request.SymptomDetails.Select(d=>(d.Code ?? "").Trim().ToUpperInvariant()).Distinct().Count()==request.SymptomDetails.Count).WithMessage("Provide one severity entry per symptom.");
        RuleFor(x=>x).Must(request=>request.SymptomDetails is null || request.SymptomDetails.All(d=>request.SymptomCodes is not null && request.SymptomCodes.Any(code=>string.Equals((code ?? "").Trim(),(d.Code ?? "").Trim(),StringComparison.OrdinalIgnoreCase)))).WithMessage("Severity entries must belong to selected symptoms.");
        RuleForEach(x=>x.SymptomDetails).ChildRules(detail=>{
            detail.RuleFor(d=>d.Code).NotEmpty().MaximumLength(50);
            detail.RuleFor(d=>d.Severity).Must(value=>value is "mild" or "moderate" or "severe");
            detail.RuleFor(d=>d.DurationHours).InclusiveBetween(0,8760);
        });
        RuleFor(x=>x.SymptomCodes).Must(codes=>codes is null || codes.Count<=32).WithMessage("Select at most 32 symptom codes.");
        RuleFor(x => x.Severity).Must(x => new[] { "mild", "moderate", "severe" }.Contains(x))
            .WithMessage("Severity must be mild, moderate, or severe.");
        RuleFor(x => x.DurationHours).InclusiveBetween(0, 8760);
        RuleFor(x => x.AstronautId)
            .GreaterThan(0).WithMessage("AstronautId must be greater than 0.");

        RuleFor(x => x.SymptomCodes)
            .NotNull().WithMessage("SymptomCodes list cannot be null.")
            .NotEmpty().WithMessage("At least one symptom code must be provided.");

        RuleForEach(x => x.SymptomCodes)
            .NotEmpty().WithMessage("Symptom code cannot be empty.")
            .MaximumLength(50);
    }
}


