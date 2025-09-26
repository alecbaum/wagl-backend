-- SQL Script to Promote Users to Admin Role
-- Run this directly in your PostgreSQL database

-- First, let's check what users exist and their current roles
SELECT
    u."Email",
    u."FirstName",
    u."LastName",
    string_agg(r."Name", ', ') as current_roles
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" IN ('bash@sentry10.com', 'brian@wagl.ai', 'admin@example.com')
GROUP BY u."Email", u."FirstName", u."LastName"
ORDER BY u."Email";

-- Promote bash@sentry10.com to Admin and ChatAdmin
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT
    u."Id" as "UserId",
    r."Id" as "RoleId"
FROM "AspNetUsers" u
CROSS JOIN "AspNetRoles" r
WHERE u."Email" = 'bash@sentry10.com'
  AND r."Name" IN ('Admin', 'ChatAdmin')
  AND NOT EXISTS (
    SELECT 1 FROM "AspNetUserRoles" ur2
    WHERE ur2."UserId" = u."Id" AND ur2."RoleId" = r."Id"
  );

-- Promote brian@wagl.ai to Admin and ChatAdmin
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT
    u."Id" as "UserId",
    r."Id" as "RoleId"
FROM "AspNetUsers" u
CROSS JOIN "AspNetRoles" r
WHERE u."Email" = 'brian@wagl.ai'
  AND r."Name" IN ('Admin', 'ChatAdmin')
  AND NOT EXISTS (
    SELECT 1 FROM "AspNetUserRoles" ur2
    WHERE ur2."UserId" = u."Id" AND ur2."RoleId" = r."Id"
  );

-- Also promote admin@example.com to Admin and ChatAdmin
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT
    u."Id" as "UserId",
    r."Id" as "RoleId"
FROM "AspNetUsers" u
CROSS JOIN "AspNetRoles" r
WHERE u."Email" = 'admin@example.com'
  AND r."Name" IN ('Admin', 'ChatAdmin')
  AND NOT EXISTS (
    SELECT 1 FROM "AspNetUserRoles" ur2
    WHERE ur2."UserId" = u."Id" AND ur2."RoleId" = r."Id"
  );

-- Verify the promotions worked
SELECT
    u."Email",
    u."FirstName",
    u."LastName",
    string_agg(r."Name", ', ') as updated_roles
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" IN ('bash@sentry10.com', 'brian@wagl.ai', 'admin@example.com')
GROUP BY u."Email", u."FirstName", u."LastName"
ORDER BY u."Email";

-- Show a summary
SELECT 'Admin promotion completed!' as status;