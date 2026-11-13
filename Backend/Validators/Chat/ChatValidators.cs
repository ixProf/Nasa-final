using FluentValidation;
using Microsoft.Extensions.Configuration;
using AstronautHealthMonitoring.DTOs.Chat;

namespace AstronautHealthMonitoring.Validators.Chat;

public class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator(IConfiguration? configuration = null, TimeProvider? timeProvider = null)
    {
        var provider = timeProvider ?? TimeProvider.System;
        // PLACEHOLDER: 5-minute future tolerance for offline client clock skew; requires operational review
        var toleranceMinutes = configuration?["Sync:FutureToleranceMinutes"] is string val && double.TryParse(val, out var m) ? m : 5.0;

        RuleFor(x => x.ClientGeneratedId)
            .NotEmpty().WithMessage("ClientGeneratedId is required and cannot be empty.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(4000);

        RuleFor(x => x.RecordedAt)
            .NotEmpty().WithMessage("RecordedAt timestamp is required.")
            .Must(t => t <= provider.GetUtcNow().UtcDateTime.AddMinutes(toleranceMinutes))
            .WithMessage("RecordedAt cannot be in the future beyond tolerance.");
    }
}
