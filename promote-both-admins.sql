-- Comprehensive Admin Promotion Script for Both Users
-- Promotes bash@sentry10.com and brian@wagl.ai to Admin roles

-- Check current status
SELECT 'Current user status:' as info;
SELECT
    u."Email",
    u."FirstName",
    u."LastName",
    string_agg(r."Name", ', ') as current_roles
FROM "Users" u
LEFT JOIN "UserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Email" IN ('bash@sentry10.com', 'brian@wagl.ai')
GROUP BY u."Email", u."FirstName", u."LastName"
ORDER BY u."Email";

-- Create ChatAdmin role if it doesn't exist
INSERT INTO "Roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT gen_random_uuid(), 'ChatAdmin', 'CHATADMIN', gen_random_uuid()::text
WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'ChatAdmin');

-- Promote bash@sentry10.com to Admin and ChatAdmin
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT
    u."Id" as "UserId",
    r."Id" as "RoleId"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" = 'bash@sentry10.com'
  AND r."Name" IN ('Admin', 'ChatAdmin')
  AND NOT EXISTS (
    SELECT 1 FROM "UserRoles" ur2
    WHERE ur2."UserId" = u."Id" AND ur2."RoleId" = r."Id"
  );

-- Promote brian@wagl.ai to Admin and ChatAdmin
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT
    u."Id" as "UserId",
    r."Id" as "RoleId"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" = 'brian@wagl.ai'
  AND r."Name" IN ('Admin', 'ChatAdmin')
  AND NOT EXISTS (
    SELECT 1 FROM "UserRoles" ur2
    WHERE ur2."UserId" = u."Id" AND ur2."RoleId" = r."Id"
  );

-- Verify final status
SELECT 'Final user status after promotion:' as info;
SELECT
    u."Email",
    u."FirstName",
    u."LastName",
    string_agg(r."Name", ', ') as updated_roles
FROM "Users" u
LEFT JOIN "UserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Email" IN ('bash@sentry10.com', 'brian@wagl.ai')
GROUP BY u."Email", u."FirstName", u."LastName"
ORDER BY u."Email";

SELECT 'Admin promotion completed for both users!' as status;