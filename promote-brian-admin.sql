-- Check if brian@wagl.ai exists and promote to Admin
SELECT 'Checking brian@wagl.ai...' as status;

-- Check current users
SELECT "Email", "FirstName", "LastName" FROM "Users"
WHERE "Email" IN ('bash@sentry10.com', 'brian@wagl.ai')
ORDER BY "Email";

-- Promote brian@wagl.ai to Admin if exists
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

-- Check final status
SELECT
    u."Email",
    string_agg(r."Name", ', ') as roles
FROM "Users" u
LEFT JOIN "UserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Email" IN ('bash@sentry10.com', 'brian@wagl.ai')
GROUP BY u."Email", u."FirstName", u."LastName"
ORDER BY u."Email";

SELECT 'Admin promotion check completed!' as status;