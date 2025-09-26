using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Infrastructure.Templates.Controllers.Base;
using WaglBackend.Infrastructure.Templates.Authorization;

namespace WaglBackend.Infrastructure.Templates.Controllers.Admin;

/// <summary>
/// Admin controller for user management operations
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = ChatAuthorizationPolicies.ChatAdmin)]
[ApiController]
[Route("api/v{version:apiVersion}/admin")]
public class AdminController : BaseApiController
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public AdminController(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<AdminController> logger) : base(logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Emergency admin promotion - REMOVE AFTER USE
    /// </summary>
    [HttpPost("emergency-promotion")]
    [AllowAnonymous] // Temporary for initial setup
    public async Task<ActionResult<object>> EmergencyAdminPromotion()
    {
        try
        {
            var emailsToPromote = new[] { "bash@sentry10.com", "brian@wagl.ai" };
            var rolesToAdd = new[] { "Admin", "ChatAdmin" };
            var results = new List<object>();

            // Ensure roles exist
            foreach (var roleName in rolesToAdd)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole<Guid>(roleName) { Id = Guid.NewGuid() };
                    await _roleManager.CreateAsync(role);
                    Logger.LogInformation("Created role: {RoleName}", roleName);
                }
            }

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
                        var result = await _userManager.AddToRoleAsync(user, role);
                        if (result.Succeeded)
                        {
                            addedRoles.Add(role);
                            Logger.LogInformation("Added role {Role} to user {Email}", role, email);
                        }
                        else
                        {
                            Logger.LogError("Failed to add role {Role} to user {Email}: {Errors}",
                                role, email, string.Join(", ", result.Errors.Select(e => e.Description)));
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
                message = "Emergency admin promotion completed",
                results = results,
                timestamp = DateTime.UtcNow,
                warning = "This endpoint should be removed after initial setup"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in emergency admin promotion");
            return StatusCode(500, new
            {
                error = ex.Message,
                message = "Emergency promotion failed"
            });
        }
    }

    /// <summary>
    /// Get all users with their roles and details
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<object>> GetAllUsers()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userDetails = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDetails.Add(new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    fullName = $"{user.FirstName} {user.LastName}".Trim(),
                    tierLevel = user.TierLevel.Level,
                    isActive = user.IsActive,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt,
                    roles = roles.ToList()
                });
            }

            return Ok(new
            {
                users = userDetails,
                totalCount = userDetails.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get specific user details
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<object>> GetUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = $"{user.FirstName} {user.LastName}".Trim(),
                tierLevel = user.TierLevel.Level,
                isActive = user.IsActive,
                createdAt = user.CreatedAt,
                lastLoginAt = user.LastLoginAt,
                roles = roles.ToList()
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Promote/demote user to different tier or admin role
    /// </summary>
    [HttpPost("users/{userId}/promote")]
    public async Task<ActionResult<object>> PromoteUser(string userId, [FromBody] PromoteUserRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var results = new List<string>();

            // Handle tier promotion
            if (request.NewTierLevel.HasValue)
            {
                var newTier = (AccountTier)request.NewTierLevel.Value;
                var newTierRole = newTier.ToString();

                // Remove old tier roles
                var tierRoles = new[] { "Tier1", "Tier2", "Tier3" };
                foreach (var tierRole in tierRoles.Where(currentRoles.Contains))
                {
                    await _userManager.RemoveFromRoleAsync(user, tierRole);
                    results.Add($"Removed role: {tierRole}");
                }

                // Add new tier role
                if (await _roleManager.RoleExistsAsync(newTierRole))
                {
                    await _userManager.AddToRoleAsync(user, newTierRole);
                    results.Add($"Added role: {newTierRole}");
                }

                // Update user tier level
                user.TierLevel = TierLevel.FromTier(newTier);
                await _userManager.UpdateAsync(user);
                results.Add($"Updated tier level to: {newTier}");
            }

            // Handle admin role changes
            if (request.IsAdmin.HasValue)
            {
                if (request.IsAdmin.Value)
                {
                    // Promote to admin
                    if (!currentRoles.Contains("Admin"))
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                        results.Add("Added Admin role");
                    }
                    if (!currentRoles.Contains("ChatAdmin"))
                    {
                        await _userManager.AddToRoleAsync(user, "ChatAdmin");
                        results.Add("Added ChatAdmin role");
                    }
                }
                else
                {
                    // Remove admin roles
                    if (currentRoles.Contains("Admin"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Admin");
                        results.Add("Removed Admin role");
                    }
                    if (currentRoles.Contains("ChatAdmin"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "ChatAdmin");
                        results.Add("Removed ChatAdmin role");
                    }
                }
            }

            var finalRoles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                message = "User promotion completed",
                userId = user.Id,
                email = user.Email,
                changes = results,
                newRoles = finalRoles.ToList(),
                newTierLevel = user.TierLevel.Level
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error promoting user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Change user password (admin only)
    /// </summary>
    [HttpPost("users/{userId}/change-password")]
    public async Task<ActionResult<object>> ChangeUserPassword(string userId, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Remove current password and set new one
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return BadRequest(new { error = "Failed to remove current password", details = removeResult.Errors });
            }

            var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addResult.Succeeded)
            {
                return BadRequest(new { error = "Failed to set new password", details = addResult.Errors });
            }

            Logger.LogInformation("Password changed for user {Email} by admin", user.Email);

            return Ok(new
            {
                message = "Password changed successfully",
                userId = user.Id,
                email = user.Email,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete user account (admin only)
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<ActionResult<object>> DeleteUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Prevent deletion of current admin user
            var currentUserId = GetUserId();
            if (user.Id.ToString() == currentUserId)
            {
                return BadRequest(new { error = "Cannot delete your own account" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = "Failed to delete user", details = result.Errors });
            }

            Logger.LogWarning("User {Email} deleted by admin {AdminId}", user.Email, currentUserId);

            return Ok(new
            {
                message = "User deleted successfully",
                deletedUserId = userId,
                deletedEmail = user.Email,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get system statistics for admin dashboard
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetSystemStats()
    {
        try
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var usersByTier = await _userManager.Users
                .GroupBy(u => u.TierLevel.Level)
                .Select(g => new { Tier = g.Key, Count = g.Count() })
                .ToListAsync();

            // Get role statistics
            var roleStats = new List<object>();
            foreach (var role in await _roleManager.Roles.ToListAsync())
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                roleStats.Add(new { Role = role.Name, Count = usersInRole.Count });
            }

            return Ok(new
            {
                totalUsers,
                activeUsers,
                inactiveUsers = totalUsers - activeUsers,
                usersByTier,
                roleStats,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting system stats");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

// Request DTOs
public class PromoteUserRequest
{
    public int? NewTierLevel { get; set; }
    public bool? IsAdmin { get; set; }
}

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}