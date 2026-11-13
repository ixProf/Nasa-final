using FluentValidation;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;

namespace AstronautHealthMonitoring.Validators.SymptomGuidance;

public class EvaluateSymptomsRequestValidator : AbstractValidator<EvaluateSymptomsRequest>
{
    public EvaluateSymptomsRequestValidator()
    {
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

