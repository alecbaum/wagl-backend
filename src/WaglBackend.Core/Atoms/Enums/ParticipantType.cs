namespace WaglBackend.Core.Atoms.Enums;

public enum ParticipantType
{
    RegisteredUser = 1,
    GuestUser = 2,
    SystemModerator = 3,    // TODO: Placeholder - UAI doesn't send moderator messages yet
    BotParticipant = 4      // TODO: Placeholder - UAI doesn't send bot messages yet
}