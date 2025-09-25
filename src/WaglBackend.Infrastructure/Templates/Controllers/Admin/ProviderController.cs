using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Asp.Versioning;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Infrastructure.Templates.Controllers.Base;
using WaglBackend.Infrastructure.Templates.Authorization;

namespace WaglBackend.Infrastructure.Templates.Controllers.Admin;

/// <summary>
/// Provider management endpoints - Admin access only
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = ChatAuthorizationPolicies.ChatAdmin)]
[ApiController]
[Route("api/v{version:apiVersion}/providers")]
public class ProviderController : BaseApiController
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IProviderService _providerService;

    public ProviderController(
        IApiKeyService apiKeyService,
        IProviderService providerService,
        ILogger<ProviderController> logger) : base(logger)
    {
        _apiKeyService = apiKeyService;
        _providerService = providerService;
    }

    /// <summary>
    /// Create a new provider account (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProviderResponse>> CreateProvider(
        [FromBody] CreateProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerService.CreateProviderAsync(request, cancellationToken);

            Logger.LogInformation("Provider created: {ProviderId} by admin {UserId}",
                provider.Id, GetUserId());

            return Ok(provider);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid request for creating provider: {Message}", ex.Message);
            return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating provider");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to create provider" });
        }
    }

    /// <summary>
    /// Get current provider profile (for providers authenticated with API key)
    /// </summary>
    [HttpGet("my-profile")]
    [Authorize] // Allow both JWT and API key auth
    public async Task<ActionResult<ProviderResponse>> GetMyProfile(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsProvider())
            {
                return BadRequest(new { error = "NOT_PROVIDER", message = "This endpoint is for providers only" });
            }

            if (!Guid.TryParse(GetUserId(), out var providerId))
            {
                return BadRequest(new { error = "INVALID_USER_ID", message = "Invalid user ID format" });
            }

            var provider = await _providerService.GetProviderByIdAsync(providerId, cancellationToken);

            if (provider == null)
            {
                Logger.LogWarning("Provider profile not found: {ProviderId}", providerId);
                return NotFound(new { error = "PROVIDER_NOT_FOUND", message = "Provider profile not found" });
            }

            return Ok(provider);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving provider profile");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve provider profile" });
        }
    }

    /// <summary>
    /// Regenerate API key for current provider
    /// </summary>
    [HttpPut("regenerate-api-key")]
    [Authorize] // Allow both JWT and API key auth, but must be provider
    public async Task<ActionResult<object>> RegenerateApiKey(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsProvider())
            {
                return BadRequest(new { error = "NOT_PROVIDER", message = "This endpoint is for providers only" });
            }

            if (!Guid.TryParse(GetUserId(), out var providerId))
            {
                return BadRequest(new { error = "INVALID_USER_ID", message = "Invalid user ID format" });
            }

            var newApiKey = await _providerService.RegenerateApiKeyAsync(providerId, cancellationToken);

            Logger.LogInformation("API key regenerated for provider: {ProviderId}", providerId);

            return Ok(new {
                apiKey = newApiKey,
                regeneratedAt = DateTime.UtcNow,
                message = "API key has been regenerated. Please update your applications with the new key."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error regenerating API key for provider: {ProviderId}", GetUserId());
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to regenerate API key" });
        }
    }

    /// <summary>
    /// List all providers (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProviderResponse>>> GetProviders(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var providers = await _providerService.GetAllProvidersAsync(cancellationToken);

            Logger.LogInformation("Retrieved {Count} providers for admin: {UserId}",
                providers.Count(), GetUserId());

            return Ok(providers);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving providers list");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve providers" });
        }
    }
}