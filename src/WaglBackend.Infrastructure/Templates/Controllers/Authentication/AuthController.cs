using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Asp.Versioning;
using WaglBackend.Core.Atoms.Constants;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Infrastructure.Templates.Controllers.Base;

namespace WaglBackend.Infrastructure.Templates.Controllers.Authentication;

[ApiVersion("1.0")]
public class AuthController : BaseApiController
{
    private readonly IUserAuthService _userAuthService;

    public AuthController(
        IUserAuthService userAuthService,
        ILogger<AuthController> logger) : base(logger)
    {
        _userAuthService = userAuthService;
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userAuthService.LoginAsync(request, cancellationToken);
            Logger.LogInformation("User {Email} logged in successfully", request.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to login user {Email}", request.Email);
            return Unauthorized(new { error = ErrorCodes.Authentication.InvalidCredentials, message = "Invalid email or password" });
        }
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userAuthService.RegisterAsync(request, cancellationToken);
            Logger.LogInformation("User {Email} registered successfully", request.Email);
            return CreatedAtAction(nameof(Login), result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register user {Email}", request.Email);
            return BadRequest(new { error = ErrorCodes.User.UserAlreadyExists, message = "User registration failed" });
        }
    }

    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userAuthService.RefreshTokenAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh token");
            return Unauthorized(new { error = ErrorCodes.Authentication.InvalidToken, message = "Invalid refresh token" });
        }
    }

    /// <summary>
    /// Logout user and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize(Policy = PolicyNames.AnyAuthenticated)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            await _userAuthService.LogoutAsync(userId, cancellationToken);
            Logger.LogInformation("User {UserId} logged out successfully", userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to logout user");
            return BadRequest(new { error = ErrorCodes.GeneralError, message = "Logout failed" });
        }
    }
}