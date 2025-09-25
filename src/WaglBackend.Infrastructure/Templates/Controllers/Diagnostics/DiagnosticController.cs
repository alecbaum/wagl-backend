using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Asp.Versioning;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Infrastructure.Templates.Controllers.Base;

namespace WaglBackend.Infrastructure.Templates.Controllers.Diagnostics;

/// <summary>
/// Diagnostic endpoints for debugging deployment issues
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/diagnostics")]
public class DiagnosticController : BaseApiController
{
    private readonly UserManager<User> _userManager;

    public DiagnosticController(
        UserManager<User> userManager,
        ILogger<DiagnosticController> logger) : base(logger)
    {
        _userManager = userManager;
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
}