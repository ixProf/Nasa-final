using FluentValidation;
using AstronautHealthMonitoring.DTOs.Astronaut;

namespace AstronautHealthMonitoring.Validators.Astronaut;

public class CreateAstronautRequestValidator : AbstractValidator<CreateAstronautRequest>
{
    public CreateAstronautRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256);

        RuleFor(x => x.InitialPassword)
            .NotEmpty().WithMessage("InitialPassword is required.");

        RuleFor(x => x.AstronautCode)
            .NotEmpty().WithMessage("AstronautCode is required.")
            .MaximumLength(50);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName is required.")
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .Must(dob => dob < DateTime.UtcNow).WithMessage("DateOfBirth must be in the past.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .MaximumLength(50);

        RuleFor(x => x.BloodType)
            .NotEmpty().WithMessage("BloodType is required.")
            .MaximumLength(10);

        RuleFor(x => x.HeightCm)
            .GreaterThan(0).WithMessage("HeightCm must be greater than 0.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("WeightKg must be greater than 0.");

        RuleFor(x => x.EmergencyContactInfo)
            .MaximumLength(500);
    }
}

public class UpdateAstronautRequestValidator : AbstractValidator<UpdateAstronautRequest>
{
    public UpdateAstronautRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName is required.")
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .Must(dob => dob < DateTime.UtcNow).WithMessage("DateOfBirth must be in the past.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .MaximumLength(50);

        RuleFor(x => x.BloodType)
            .NotEmpty().WithMessage("BloodType is required.")
            .MaximumLength(10);

        RuleFor(x => x.HeightCm)
            .GreaterThan(0).WithMessage("HeightCm must be greater than 0.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("WeightKg must be greater than 0.");

        RuleFor(x => x.EmergencyContactInfo)
            .MaximumLength(500);
    }
}

