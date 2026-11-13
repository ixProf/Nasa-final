using FluentValidation;
using AstronautHealthMonitoring.DTOs.Recommendations;

namespace AstronautHealthMonitoring.Validators.Recommendations;

public class CreateRecommendationRequestValidator : AbstractValidator<CreateRecommendationRequest>
{
    public CreateRecommendationRequestValidator()
    {
        RuleFor(x => x.HabitPoints).InclusiveBetween(1, 100);
        RuleFor(x => x.ReminderIntervalMinutes).InclusiveBetween(1, 10080);
        RuleFor(x => x.TitleAr).MaximumLength(200);
        RuleFor(x => x.ContentAr).MaximumLength(2000);
        RuleFor(x => x.ReasonAr).MaximumLength(1000);
        When(x => x.IsDailyHabit, () =>
        {
            RuleFor(x => x.TitleAr).NotEmpty();
            RuleFor(x => x.ContentAr).NotEmpty();
            RuleFor(x => x.ReasonAr).NotEmpty();
        });
        RuleFor(x => x.PrescribedBy).MaximumLength(255);
        RuleFor(x => x.ScheduledTime).Matches(@"^([01]\d|2[0-3]):[0-5]\d$").When(x => x.ScheduledTime != null);
        RuleFor(x => x.AstronautId)
            .GreaterThan(0).WithMessage("AstronautId must be greater than 0.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(2000);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(1000);

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Valid RecommendationCategory is required.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Valid RecommendationPriority is required.");
    }
}

public class UpdateRecommendationStatusRequestValidator : AbstractValidator<UpdateRecommendationStatusRequest>
{
    public UpdateRecommendationStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Valid RecommendationStatus is required.");
    }
}

