using FluentValidation;
using WaglBackend.Core.Molecules.DTOs.Request;

namespace WaglBackend.Domain.Organisms.Validators;

public class ChatSessionRequestValidator : AbstractValidator<ChatSessionRequest>
{
    public ChatSessionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Session name is required")
            .Length(1, 100)
            .WithMessage("Session name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.]+$")
            .WithMessage("Session name can only contain letters, numbers, spaces, hyphens, underscores, and periods");

        RuleFor(x => x.ScheduledStartTime)
            .NotEmpty()
            .WithMessage("Scheduled start time is required")
            .Must(BeInTheFuture)
            .WithMessage("Scheduled start time must be in the future")
            .Must(BeWithinValidRange)
            .WithMessage("Scheduled start time cannot be more than 30 days in the future");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0 minutes")
            .LessThanOrEqualTo(1440)
            .WithMessage("Duration cannot exceed 1440 minutes (24 hours)");

        RuleFor(x => x.MaxParticipants)
            .GreaterThanOrEqualTo(6)
            .WithMessage("Maximum participants must be at least 6")
            .LessThanOrEqualTo(36)
            .WithMessage("Maximum participants cannot exceed 36 (6 rooms × 6 participants each)")
            .Must(BeMultipleOfSix)
            .WithMessage("Maximum participants should be a multiple of 6 for optimal room allocation");

        RuleFor(x => x.MaxParticipantsPerRoom)
            .GreaterThanOrEqualTo(2)
            .WithMessage("Maximum participants per room must be at least 2")
            .LessThanOrEqualTo(6)
            .WithMessage("Maximum participants per room cannot exceed 6");
    }

    private static bool BeInTheFuture(DateTime scheduledStartTime)
    {
        return scheduledStartTime > DateTime.UtcNow.AddMinutes(5); // Allow 5 minutes buffer
    }

    private static bool BeWithinValidRange(DateTime scheduledStartTime)
    {
        return scheduledStartTime <= DateTime.UtcNow.AddDays(30);
    }

    private static bool BeMultipleOfSix(int maxParticipants)
    {
        return maxParticipants % 6 == 0;
    }
}