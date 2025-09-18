using FluentValidation;
using WaglBackend.Core.Molecules.DTOs.Request;

namespace WaglBackend.Domain.Organisms.Validators;

public class SessionInviteRequestValidator : AbstractValidator<SessionInviteRequest>
{
    public SessionInviteRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");

        RuleFor(x => x.InviteeEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.InviteeEmail))
            .WithMessage("Invalid email address format")
            .MaximumLength(254)
            .WithMessage("Email address cannot exceed 254 characters");

        RuleFor(x => x.InviteeName)
            .MaximumLength(100)
            .WithMessage("Invitee name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'.]*$")
            .When(x => !string.IsNullOrEmpty(x.InviteeName))
            .WithMessage("Invitee name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.ExpirationMinutes)
            .GreaterThan(0)
            .WithMessage("Expiration time must be greater than 0 minutes")
            .LessThanOrEqualTo(1440)
            .WithMessage("Expiration time cannot exceed 1440 minutes (24 hours)");

        RuleFor(x => x)
            .Must(HaveEitherEmailOrName)
            .WithMessage("Either email or name must be provided");
    }

    private static bool HaveEitherEmailOrName(SessionInviteRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.InviteeEmail) || !string.IsNullOrWhiteSpace(request.InviteeName);
    }
}

public class InviteRecipientValidator : AbstractValidator<InviteRecipient>
{
    public InviteRecipientValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email address format");

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'.]*$")
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x)
            .Must(HaveEitherEmailOrName)
            .WithMessage("Either email or name must be provided");
    }

    private static bool HaveEitherEmailOrName(InviteRecipient recipient)
    {
        return !string.IsNullOrWhiteSpace(recipient.Email) || !string.IsNullOrWhiteSpace(recipient.Name);
    }
}

public class BulkSessionInviteRequestValidator : AbstractValidator<BulkSessionInviteRequest>
{
    public BulkSessionInviteRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");

        RuleFor(x => x.Recipients)
            .NotEmpty()
            .WithMessage("At least one recipient is required")
            .Must(HaveValidCount)
            .WithMessage("Cannot send more than 36 invites at once");

        RuleFor(x => x.ExpirationMinutes)
            .GreaterThan(0)
            .WithMessage("Expiration time must be greater than 0 minutes")
            .LessThanOrEqualTo(1440)
            .WithMessage("Expiration time cannot exceed 1440 minutes (24 hours)");

        RuleForEach(x => x.Recipients)
            .SetValidator(new InviteRecipientValidator());
    }

    private static bool HaveValidCount(List<InviteRecipient> recipients)
    {
        return recipients.Count <= 36;
    }
}