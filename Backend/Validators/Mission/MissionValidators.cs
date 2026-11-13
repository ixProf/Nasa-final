using FluentValidation;
using AstronautHealthMonitoring.DTOs.Missions;

namespace AstronautHealthMonitoring.Validators.Mission;

public class CreateMissionRequestValidator : AbstractValidator<CreateMissionRequest>
{
    public CreateMissionRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mission code is required.")
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mission name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Destination is required.")
            .MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Valid mission status is required.");
    }
}

public class UpdateMissionRequestValidator : AbstractValidator<UpdateMissionRequest>
{
    public UpdateMissionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mission name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Destination is required.")
            .MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Valid mission status is required.");
    }
}

