using Microsoft.AspNetCore.Authorization;

namespace WaglBackend.Infrastructure.Templates.Authorization;

public static class ChatAuthorizationPolicies
{
    // Policy names
    public const string ChatAccess = "ChatAccess";
    public const string SessionCreator = "SessionCreator";
    public const string SessionParticipant = "SessionParticipant";
    public const string RoomParticipant = "RoomParticipant";
    public const string ChatModerator = "ChatModerator";
    public const string ChatAdmin = "ChatAdmin";

    // Claim types
    public const string SessionIdClaim = "chat_session_id";
    public const string RoomIdClaim = "chat_room_id";
    public const string ParticipantTypeClaim = "chat_participant_type";

    public static void AddChatPolicies(this AuthorizationOptions options)
    {
        // Basic chat access - requires authentication
        options.AddPolicy(ChatAccess, policy =>
        {
            policy.RequireAuthenticatedUser();
        });

        // Session creator - user must be the creator of a session
        options.AddPolicy(SessionCreator, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("role", "ChatSessionCreator");
        });

        // Session participant - user must be a participant in a session
        options.AddPolicy(SessionParticipant, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new SessionParticipantRequirement());
        });

        // Room participant - user must be a participant in a specific room
        options.AddPolicy(RoomParticipant, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new RoomParticipantRequirement());
        });

        // Chat moderator - can moderate chat sessions
        options.AddPolicy(ChatModerator, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                // User is either a Tier3 user or has specific chat moderator role
                return context.User.HasClaim("role", "Tier3") ||
                       context.User.HasClaim("role", "ChatModerator") ||
                       context.User.HasClaim("role", "Provider") ||
                       context.User.HasClaim("role", "Admin");
            });
        });

        // Chat admin - full chat system administration
        options.AddPolicy(ChatAdmin, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                return context.User.HasClaim("role", "Provider") ||
                       context.User.HasClaim("role", "ChatAdmin") ||
                       context.User.HasClaim("role", "Admin");
            });
        });
    }
}

public class SessionParticipantRequirement : IAuthorizationRequirement
{
}

public class RoomParticipantRequirement : IAuthorizationRequirement
{
}