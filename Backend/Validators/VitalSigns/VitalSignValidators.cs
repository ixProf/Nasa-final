using FluentValidation;
using Microsoft.Extensions.Configuration;
using AstronautHealthMonitoring.DTOs.VitalSigns;

namespace AstronautHealthMonitoring.Validators.VitalSigns;

public class CreateVitalSignReadingRequestValidator : AbstractValidator<CreateVitalSignReadingRequest>
{
    public CreateVitalSignReadingRequestValidator(IConfiguration? configuration = null, TimeProvider? timeProvider = null)
    {
        var provider = timeProvider ?? TimeProvider.System;
        // PLACEHOLDER: 5-minute future tolerance for offline client clock skew; requires operational review
        var toleranceMinutes = configuration?["Sync:FutureToleranceMinutes"] is string val && double.TryParse(val, out var m) ? m : 5.0;

        RuleFor(x => x.ClientGeneratedId)
            .NotEmpty().WithMessage("ClientGeneratedId is required and cannot be empty.");

        RuleFor(x => x.VitalSignTypeId)
            .GreaterThan(0).WithMessage("VitalSignTypeId must be greater than 0.");

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0).WithMessage("Value must be non-negative.");

        RuleFor(x => x.SecondaryValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SecondaryValue.HasValue)
            .WithMessage("SecondaryValue must be non-negative.");

        RuleFor(x => x.MeasurementMethod)
            .MaximumLength(100);

        RuleFor(x => x.SensorLocation)
            .MaximumLength(100);

        RuleFor(x => x.DataQuality)
            .IsInEnum().WithMessage("Valid DataQuality is required.");

        RuleFor(x => x.BodyPosition)
            .IsInEnum().WithMessage("Valid BodyPosition is required.");

        RuleFor(x => x.ActivityContext)
            .IsInEnum().WithMessage("Valid ActivityContext is required.");

        RuleFor(x => x.MealContext)
            .IsInEnum()
            .When(x => x.MealContext.HasValue)
            .WithMessage("Valid MealContext is required.");

        RuleFor(x => x.MinutesSinceLastMeal)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinutesSinceLastMeal.HasValue)
            .WithMessage("MinutesSinceLastMeal must be non-negative.");

        RuleFor(x => x.RecordedAt)
            .NotEmpty().WithMessage("RecordedAt timestamp is required.")
            .Must(t => t <= provider.GetUtcNow().UtcDateTime.AddMinutes(toleranceMinutes))
            .WithMessage("RecordedAt cannot be in the future beyond tolerance.");
    }
}

public class SyncVitalSignReadingsRequestValidator : AbstractValidator<SyncVitalSignReadingsRequest>
{
    public SyncVitalSignReadingsRequestValidator(IConfiguration? configuration = null, TimeProvider? timeProvider = null)
    {
        RuleFor(x => x.Readings)
            .NotNull().WithMessage("Readings collection is required.")
            .NotEmpty().WithMessage("At least one reading must be provided.");

        RuleForEach(x => x.Readings).SetValidator(new CreateVitalSignReadingRequestValidator(configuration, timeProvider));
    }
}
