using FluentValidation;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Validators.Alerts;

public class AcknowledgeAlertRequestValidator : AbstractValidator<AcknowledgeAlertRequest>
{
    public AcknowledgeAlertRequestValidator()
    {
        RuleFor(x => x.ActionTaken)
            .MaximumLength(1000);
    }
}

public class TriggerManualAlertRequestValidator : AbstractValidator<TriggerManualAlertRequest>
{
    public TriggerManualAlertRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Alert title is required.")
            .MaximumLength(200).WithMessage("Alert title cannot exceed 200 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Alert message is required.")
            .MaximumLength(1000).WithMessage("Alert message cannot exceed 1000 characters.");

        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason cannot exceed 1000 characters.");

        RuleFor(x => x.ActionRequired)
            .MaximumLength(1000).WithMessage("ActionRequired cannot exceed 1000 characters.");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Valid AlertSeverity is required.")
            .Must(s => s != AlertSeverity.Routine)
            .WithMessage("Manual alerts must be at least MedicalAttention or Emergency severity.");

        RuleFor(x => x.AstronautId)
            .GreaterThanOrEqualTo(0).WithMessage("AstronautId must be non-negative.");
    }
}

public class ResolveAlertRequestValidator : AbstractValidator<ResolveAlertRequest>
{
    public ResolveAlertRequestValidator()
    {
        RuleFor(x => x.ResolutionNotes)
            .MaximumLength(1000).WithMessage("Resolution notes cannot exceed 1000 characters.");
    }
}
