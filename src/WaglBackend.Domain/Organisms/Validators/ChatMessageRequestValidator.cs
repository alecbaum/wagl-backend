using FluentValidation;
using WaglBackend.Core.Molecules.DTOs.Request;

namespace WaglBackend.Domain.Organisms.Validators;

public class ChatMessageRequestValidator : AbstractValidator<ChatMessageRequest>
{
    public ChatMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content is required")
            .Length(1, 1000)
            .WithMessage("Message content must be between 1 and 1000 characters")
            .Must(NotContainOnlyWhitespace)
            .WithMessage("Message cannot contain only whitespace");

        RuleFor(x => x.RoomId)
            .NotEmpty()
            .WithMessage("Room ID is required");
    }

    private static bool NotContainOnlyWhitespace(string content)
    {
        return !string.IsNullOrWhiteSpace(content);
    }
}

public class JoinRoomRequestValidator : AbstractValidator<JoinRoomRequest>
{
    public JoinRoomRequestValidator()
    {
        RuleFor(x => x.InviteToken)
            .NotEmpty()
            .WithMessage("Invite token is required");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .Length(1, 50)
            .WithMessage("Display name must be between 1 and 50 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.]+$")
            .WithMessage("Display name can only contain letters, numbers, spaces, hyphens, underscores, and periods");
    }
}