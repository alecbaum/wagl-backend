-- Quick promotion for brian@wagl.ai only
-- User ID: ea4f793f-3eb8-4451-b7fd-9c5632c5b873

-- Create ChatAdmin role if it doesn't exist
INSERT INTO "Roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT gen_random_uuid(), 'ChatAdmin', 'CHATADMIN', gen_random_uuid()::text
WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'ChatAdmin');

-- Promote brian@wagl.ai to Admin role
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT
    u."Id" as "UserId",
    r."Id" as "RoleId"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" = 'brian@wagl.ai'
  AND r."Name" = 'Admin'
  AND NOT EXISTS (
    SELECT 1 FROM "UserRoles" ur2
    WHERE ur2."UserId" = u."Id" AND ur2."RoleId" = r."Id"
  );

-- Promote brian@wagl.ai to ChatAdmin role
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT
    u."Id" as "UserId",
    r."Id" as "RoleId"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" = 'brian@wagl.ai'
  AND r."Name" = 'ChatAdmin'
  AND NOT EXISTS (
    SELECT 1 FROM "UserRoles" ur2
    WHERE ur2."UserId" = u."Id" AND ur2."RoleId" = r."Id"
  );

-- Verify brian@wagl.ai promotion
SELECT
    u."Email",
    string_agg(r."Name", ', ') as roles
FROM "Users" u
LEFT JOIN "UserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'brian@wagl.ai'
GROUP BY u."Email";

SELECT 'brian@wagl.ai promoted to Admin + ChatAdmin!' as status;