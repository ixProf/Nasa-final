using FluentValidation;
using Microsoft.Extensions.Configuration;
using AstronautHealthMonitoring.DTOs.Sleep;

namespace AstronautHealthMonitoring.Validators.Sleep;

public class CreateSleepRecordRequestValidator : AbstractValidator<CreateSleepRecordRequest>
{
    public CreateSleepRecordRequestValidator(IConfiguration? configuration = null, TimeProvider? timeProvider = null)
    {
        var provider = timeProvider ?? TimeProvider.System;
        // PLACEHOLDER: 5-minute future tolerance for offline client clock skew; requires operational review
        var toleranceMinutes = configuration?["Sync:FutureToleranceMinutes"] is string val && double.TryParse(val, out var m) ? m : 5.0;

        RuleFor(x => x.ClientGeneratedId)
            .NotEmpty().WithMessage("ClientGeneratedId is required and cannot be empty.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be after StartTime.");

        RuleFor(x => x.Quality)
            .IsInEnum().WithMessage("Valid SleepQuality is required.");

        RuleFor(x => x.DeepSleepMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("DeepSleepMinutes cannot be negative.");

        RuleFor(x => x.RemSleepMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("RemSleepMinutes cannot be negative.");

        RuleFor(x => x.LightSleepMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("LightSleepMinutes cannot be negative.");

        RuleFor(x => x.AwakeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("AwakeMinutes cannot be negative.");

        RuleFor(x => x.SleepEfficiencyPercentage)
            .InclusiveBetween(0, 100).WithMessage("SleepEfficiencyPercentage must be between 0 and 100.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);

        RuleFor(x => x.RecordedAt)
            .NotEmpty().WithMessage("RecordedAt timestamp is required.")
            .Must(t => t <= provider.GetUtcNow().UtcDateTime.AddMinutes(toleranceMinutes))
            .WithMessage("RecordedAt cannot be in the future beyond tolerance.");
    }
}
