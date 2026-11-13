using FluentValidation;
using Microsoft.Extensions.Configuration;
using AstronautHealthMonitoring.DTOs.Sync;
using AstronautHealthMonitoring.Validators.Chat;
using AstronautHealthMonitoring.Validators.Mood;
using AstronautHealthMonitoring.Validators.Sleep;
using AstronautHealthMonitoring.Validators.VitalSigns;

namespace AstronautHealthMonitoring.Validators.Sync;

public class SyncBatchRequestValidator : AbstractValidator<SyncBatchRequest>
{
    public SyncBatchRequestValidator(IConfiguration? configuration = null, TimeProvider? timeProvider = null)
    {
        var provider = timeProvider ?? TimeProvider.System;
        // PLACEHOLDER: 5-minute future tolerance for offline client clock skew; requires operational review
        var toleranceMinutes = configuration?["Sync:FutureToleranceMinutes"] is string val && double.TryParse(val, out var m) ? m : 5.0;

        RuleFor(x => x.DeviceSyncTime)
            .NotEmpty().WithMessage("DeviceSyncTime is required.")
            .Must(t => t <= provider.GetUtcNow().UtcDateTime.AddMinutes(toleranceMinutes))
            .WithMessage("DeviceSyncTime cannot be in the future beyond tolerance.");

        RuleForEach(x => x.VitalSignReadings).SetValidator(new CreateVitalSignReadingRequestValidator(configuration, timeProvider));
        RuleForEach(x => x.SleepRecords).SetValidator(new CreateSleepRecordRequestValidator(configuration, timeProvider));
        RuleForEach(x => x.MoodEntries).SetValidator(new CreateMoodEntryRequestValidator(configuration, timeProvider));
        RuleForEach(x => x.ChatMessages).SetValidator(new SendChatMessageRequestValidator(configuration, timeProvider));
    }
}
