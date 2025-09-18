using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WaglBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false, defaultValue: 36),
                    MaxParticipantsPerRoom = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ApiKeyValue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ApiKeyHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ApiKeyCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'"),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AllowedIpAddresses = table.Column<string[]>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TierFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredTier = table.Column<int>(type: "integer", nullable: false),
                    FeatureName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FeatureFlag = table.Column<long>(type: "bigint", nullable: false),
                    RateLimitPerHour = table.Column<int>(type: "integer", nullable: true),
                    DailyLimit = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TierLevel = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'"),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParticipantCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatRooms", x => x.Id);
                    table.CheckConstraint("CK_ChatRooms_MaxParticipants", "\"MaxParticipants\" > 0");
                    table.CheckConstraint("CK_ChatRooms_ParticipantCount", "\"ParticipantCount\" >= 0");
                    table.CheckConstraint("CK_ChatRooms_ParticipantCount_Limit", "\"ParticipantCount\" <= \"MaxParticipants\"");
                    table.ForeignKey(
                        name: "FK_ChatRooms_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatRooms_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviteeEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    InviteeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumedByName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionInvites_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionInvites_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    RequestTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestSize = table.Column<int>(type: "integer", nullable: false),
                    ResponseSize = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiUsageLogs_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApiUsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'"),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ChatRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuidId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participants_ChatRooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "ChatRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participants_ChatRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "ChatRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participants_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participants_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participants_Users_UserGuidId",
                        column: x => x.UserGuidId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChatRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatRooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "ChatRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "ChatRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Participants_ParticipantId1",
                        column: x => x.ParticipantId1,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_AccountType",
                table: "ApiUsageLogs",
                column: "AccountType");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_AccountType_RequestTimestamp",
                table: "ApiUsageLogs",
                columns: new[] { "AccountType", "RequestTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_Endpoint",
                table: "ApiUsageLogs",
                column: "Endpoint");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_Endpoint_RequestTimestamp",
                table: "ApiUsageLogs",
                columns: new[] { "Endpoint", "RequestTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_ProviderId",
                table: "ApiUsageLogs",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_ProviderId_RequestTimestamp",
                table: "ApiUsageLogs",
                columns: new[] { "ProviderId", "RequestTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_RequestTimestamp",
                table: "ApiUsageLogs",
                column: "RequestTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_ResponseStatusCode",
                table: "ApiUsageLogs",
                column: "ResponseStatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_UserId",
                table: "ApiUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_UserId_RequestTimestamp",
                table: "ApiUsageLogs",
                columns: new[] { "UserId", "RequestTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatRoomId",
                table: "ChatMessages",
                column: "ChatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId",
                table: "ChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_IsDeleted",
                table: "ChatMessages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ParticipantId",
                table: "ChatMessages",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ParticipantId1",
                table: "ChatMessages",
                column: "ParticipantId1");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ParticipantId_SentAt",
                table: "ChatMessages",
                columns: new[] { "ParticipantId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_RoomId",
                table: "ChatMessages",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_RoomId_SentAt",
                table: "ChatMessages",
                columns: new[] { "RoomId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SentAt",
                table: "ChatMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId_SentAt",
                table: "ChatMessages",
                columns: new[] { "SessionId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_ChatSessionId",
                table: "ChatRooms",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_CreatedAt",
                table: "ChatRooms",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_SessionId",
                table: "ChatRooms",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_SessionId_ParticipantCount",
                table: "ChatRooms",
                columns: new[] { "SessionId", "ParticipantCount" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_SessionId_Status",
                table: "ChatRooms",
                columns: new[] { "SessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_Status",
                table: "ChatRooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CreatedAt",
                table: "ChatSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_ScheduledStartTime",
                table: "ChatSessions",
                column: "ScheduledStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_Status",
                table: "ChatSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_Status_ScheduledStartTime",
                table: "ChatSessions",
                columns: new[] { "Status", "ScheduledStartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_ChatRoomId",
                table: "Participants",
                column: "ChatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_ChatSessionId",
                table: "Participants",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_ConnectionId",
                table: "Participants",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_IsActive",
                table: "Participants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_JoinedAt",
                table: "Participants",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_RoomId",
                table: "Participants",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_RoomId_IsActive",
                table: "Participants",
                columns: new[] { "RoomId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_SessionId",
                table: "Participants",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_SessionId_IsActive",
                table: "Participants",
                columns: new[] { "SessionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_Type",
                table: "Participants",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UserGuidId",
                table: "Participants",
                column: "UserGuidId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UserId",
                table: "Participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UserId_SessionId",
                table: "Participants",
                columns: new[] { "UserId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UserId_SessionId_IsActive",
                table: "Participants",
                columns: new[] { "UserId", "SessionId", "IsActive" },
                filter: "\"UserId\" IS NOT NULL AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ApiKeyValue",
                table: "Providers",
                column: "ApiKeyValue",
                unique: true,
                filter: "\"ApiKeyValue\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_CreatedAt",
                table: "Providers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_IsActive",
                table: "Providers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_LastAccessedAt",
                table: "Providers",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Name",
                table: "Providers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked",
                table: "RefreshTokens",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_ChatSessionId",
                table: "SessionInvites",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_CreatedAt",
                table: "SessionInvites",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_ExpiresAt",
                table: "SessionInvites",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_IsConsumed",
                table: "SessionInvites",
                column: "IsConsumed");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_IsConsumed_ExpiresAt",
                table: "SessionInvites",
                columns: new[] { "IsConsumed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_SessionId",
                table: "SessionInvites",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_SessionId_IsConsumed",
                table: "SessionInvites",
                columns: new[] { "SessionId", "IsConsumed" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_Token",
                table: "SessionInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TierFeatures_FeatureName",
                table: "TierFeatures",
                column: "FeatureName");

            migrationBuilder.CreateIndex(
                name: "IX_TierFeatures_FeatureName_RequiredTier",
                table: "TierFeatures",
                columns: new[] { "FeatureName", "RequiredTier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TierFeatures_IsEnabled",
                table: "TierFeatures",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TierFeatures_RequiredTier",
                table: "TierFeatures",
                column: "RequiredTier");

            migrationBuilder.CreateIndex(
                name: "IX_TierFeatures_RequiredTier_IsEnabled",
                table: "TierFeatures",
                columns: new[] { "RequiredTier", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiUsageLogs");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "SessionInvites");

            migrationBuilder.DropTable(
                name: "TierFeatures");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ChatRooms");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ChatSessions");
        }
    }
}
