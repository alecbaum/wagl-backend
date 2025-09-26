using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Asp.Versioning;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Infrastructure.Templates.Controllers.Base;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace WaglBackend.Infrastructure.Pages.Features.Diagnostics.Controllers;

/// <summary>
/// Diagnostic endpoints for debugging deployment issues
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/diagnostics")]
public class DiagnosticController : BaseApiController
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;

    public DiagnosticController(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        ILogger<DiagnosticController> logger) : base(logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Simple health check to test controller registration
    /// </summary>
    [HttpGet("ping")]
    public ActionResult<object> Ping()
    {
        return Ok(new
        {
            status = "success",
            message = "DiagnosticController is working",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
        });
    }

    /// <summary>
    /// Check database seeding status
    /// </summary>
    [HttpGet("database-status")]
    public async Task<ActionResult<object>> GetDatabaseStatus()
    {
        try
        {
            var allUsers = _userManager.Users.ToList();
            var userCount = allUsers.Count;

            var adminUser = await _userManager.FindByEmailAsync("admin@wagl.com");
            var tierUser = await _userManager.FindByEmailAsync("tier2@wagl.com");
            var moderatorUser = await _userManager.FindByEmailAsync("moderator@wagl.com");

            string adminRoles = "Not found";
            string tierRoles = "Not found";
            string moderatorRoles = "Not found";

            if (adminUser != null)
            {
                var roles = await _userManager.GetRolesAsync(adminUser);
                adminRoles = string.Join(", ", roles);
            }

            if (tierUser != null)
            {
                var roles = await _userManager.GetRolesAsync(tierUser);
                tierRoles = string.Join(", ", roles);
            }

            if (moderatorUser != null)
            {
                var roles = await _userManager.GetRolesAsync(moderatorUser);
                moderatorRoles = string.Join(", ", roles);
            }

            return Ok(new
            {
                databaseConnected = true,
                totalUsers = userCount,
                users = new
                {
                    admin = new { exists = adminUser != null, roles = adminRoles },
                    tier2 = new { exists = tierUser != null, roles = tierRoles },
                    moderator = new { exists = moderatorUser != null, roles = moderatorRoles }
                },
                allUserEmails = allUsers.Select(u => u.Email).ToList(),
                message = userCount > 0 ? "Database appears to be seeded" : "Database appears empty - seeding may have failed"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking database status");
            return StatusCode(500, new
            {
                databaseConnected = false,
                error = ex.Message,
                message = "Database connection failed"
            });
        }
    }

    /// <summary>
    /// Test raw database connectivity without Entity Framework
    /// </summary>
    [HttpGet("database-connection")]
    public async Task<ActionResult<object>> TestDatabaseConnection()
    {
        var connectionString = _configuration.GetConnectionString("PostgreSQL");

        if (string.IsNullOrEmpty(connectionString))
        {
            return StatusCode(500, new
            {
                connected = false,
                error = "No connection string configured",
                connectionString = "NULL"
            });
        }

        try
        {
            Logger.LogInformation("Testing database connection with connection string: {ConnectionString}",
                connectionString.Replace(_configuration["DATABASE_PASSWORD"] ?? "password", "***"));

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand("SELECT version();", connection);
            var version = await command.ExecuteScalarAsync();

            return Ok(new
            {
                connected = true,
                databaseVersion = version?.ToString() ?? "Unknown",
                connectionString = connectionString.Replace(_configuration["DATABASE_PASSWORD"] ?? "password", "***"),
                message = "Database connection successful"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Database connection test failed");
            return StatusCode(500, new
            {
                connected = false,
                error = ex.Message,
                innerException = ex.InnerException?.Message,
                connectionString = connectionString.Replace(_configuration["DATABASE_PASSWORD"] ?? "password", "***"),
                message = "Database connection failed"
            });
        }
    }

    /// <summary>
    /// One-time admin promotion for specific users - REMOVE AFTER USE
    /// </summary>
    [HttpPost("promote-admins")]
    public async Task<ActionResult<object>> PromoteAdmins()
    {
        try
        {
            var emailsToPromote = new[] { "bash@sentry10.com", "brian@wagl.ai", "admin@example.com" };
            var rolesToAdd = new[] { "Admin", "ChatAdmin" };
            var results = new List<object>();

            foreach (var email in emailsToPromote)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    results.Add(new { email, status = "User not found" });
                    continue;
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                var addedRoles = new List<string>();

                foreach (var role in rolesToAdd)
                {
                    if (!currentRoles.Contains(role))
                    {
                        var roleExists = await _roleManager.RoleExistsAsync(role);
                        if (roleExists)
                        {
                            var result = await _userManager.AddToRoleAsync(user, role);
                            if (result.Succeeded)
                            {
                                addedRoles.Add(role);
                            }
                        }
                    }
                }

                var finalRoles = await _userManager.GetRolesAsync(user);
                results.Add(new
                {
                    email,
                    status = "Success",
                    rolesAdded = addedRoles,
                    allRoles = finalRoles.ToList()
                });
            }

            return Ok(new
            {
                message = "Admin promotion completed",
                results = results,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error promoting users to admin");
            return StatusCode(500, new
            {
                error = ex.Message,
                message = "Admin promotion failed"
            });
        }
    }
}