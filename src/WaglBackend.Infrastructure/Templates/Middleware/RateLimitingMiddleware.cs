using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WaglBackend.Core.Atoms.Constants;
using WaglBackend.Domain.Organisms.Services.RateLimiting;
using CustomClaimTypes = WaglBackend.Core.Atoms.Constants.ClaimTypes;

namespace WaglBackend.Infrastructure.Templates.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitService rateLimitService,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var identity = context.User.Identity;
        if (identity?.IsAuthenticated == true)
        {
            var accountType = context.User.FindFirst(CustomClaimTypes.AccountType)?.Value ?? "User";
            var userId = context.User.FindFirst(CustomClaimTypes.UserId)?.Value ?? string.Empty;
            var endpoint = context.Request.Path.Value ?? string.Empty;

            try
            {
                var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(userId, accountType, endpoint);

                // Add rate limit headers
                context.Response.Headers.Add("X-RateLimit-Limit", rateLimitResult.Limit.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", rateLimitResult.Remaining.ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", ((DateTimeOffset)rateLimitResult.ResetTime).ToUnixTimeSeconds().ToString());

                if (!rateLimitResult.IsAllowed)
                {
                    context.Response.Headers.Add("Retry-After", ((int)rateLimitResult.RetryAfter.TotalSeconds).ToString());
                    
                    _logger.LogWarning("Rate limit exceeded for user {UserId}, account type {AccountType}, endpoint {Endpoint}. Reason: {Reason}", 
                        userId, accountType, endpoint, rateLimitResult.Reason);

                    context.Response.StatusCode = 429;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        error = ErrorCodes.RateLimit.LimitExceeded,
                        message = "Rate limit exceeded. Please try again later.",
                        details = new
                        {
                            limit = rateLimitResult.Limit,
                            used = rateLimitResult.Used,
                            resetTime = rateLimitResult.ResetTime,
                            retryAfter = rateLimitResult.RetryAfter.TotalSeconds,
                            reason = rateLimitResult.Reason
                        }
                    };

                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
                // Continue processing if rate limiting fails
            }
        }

        await _next(context);
    }
}