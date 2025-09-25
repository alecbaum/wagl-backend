using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WaglBackend.Core.Atoms.Constants;
using WaglBackend.Core.Molecules.Configurations;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Infrastructure.Pages.Extensions;

namespace WaglBackend.Infrastructure.Pages.Features.Authentication;

public class AuthenticationModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication services
        services.AddScoped<IUserAuthService, WaglBackend.Infrastructure.Services.Authentication.UserAuthService>();
        // services.AddScoped<IProviderAuthService, ProviderAuthService>(); // Not implemented yet
        // services.AddScoped<IApiKeyService, ApiKeyService>(); // Not implemented yet
        services.AddScoped<IJwtService, WaglBackend.Infrastructure.Services.Authentication.JwtService>();

        // Get JWT configuration - use explicit binding to ensure it works
        var jwtConfig = new JwtConfiguration();
        var jwtSection = configuration.GetSection(JwtConfiguration.SectionName);
        jwtSection.Bind(jwtConfig);

        // Also try direct configuration access as fallback
        if (string.IsNullOrEmpty(jwtConfig.SecretKey))
        {
            jwtConfig.SecretKey = configuration["Authentication:Jwt:SecretKey"] ?? "";
            jwtConfig.Issuer = configuration["Authentication:Jwt:Issuer"] ?? "";
            jwtConfig.Audience = configuration["Authentication:Jwt:Audience"] ?? "";
        }

        // Log JWT configuration for debugging - use Console during service registration
        // to avoid "logger is already frozen" issues

        // Force JWT authentication even if configuration seems empty
        // This ensures authentication middleware is always registered
        if (string.IsNullOrEmpty(jwtConfig.SecretKey))
        {
            Console.WriteLine("JWT SecretKey is empty! Using environment variable fallback...");
            jwtConfig.SecretKey = Environment.GetEnvironmentVariable("Authentication__Jwt__SecretKey") ??
                                 Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ??
                                 "WaglBackendJwtSecretKeyForProductionEnvironment2024!VerySecureKey123";
        }

        if (string.IsNullOrEmpty(jwtConfig.Issuer))
        {
            jwtConfig.Issuer = Environment.GetEnvironmentVariable("Authentication__Jwt__Issuer") ??
                              Environment.GetEnvironmentVariable("JWT_ISSUER") ??
                              "https://api.wagl.ai";
        }

        if (string.IsNullOrEmpty(jwtConfig.Audience))
        {
            jwtConfig.Audience = Environment.GetEnvironmentVariable("Authentication__Jwt__Audience") ??
                                Environment.GetEnvironmentVariable("JWT_AUDIENCE") ??
                                "wagl-backend-api";
        }

        Console.WriteLine($"JWT Configuration - Issuer: {jwtConfig.Issuer}, Audience: {jwtConfig.Audience}, SecretKey Length: {jwtConfig.SecretKey?.Length ?? 0}");

        // Configure JWT authentication (ALWAYS - no conditional check)
        Console.WriteLine($"Configuring JWT authentication with SecretKey length: {jwtConfig.SecretKey?.Length ?? 0}");

        // JWT authentication setup - UNCONDITIONAL
        {
            // Configure JWT Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "MultiAuth";
                options.DefaultChallengeScheme = "MultiAuth";
            })
            .AddJwtBearer("JwtBearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtConfig.ValidateIssuer,
                    ValidateAudience = jwtConfig.ValidateAudience,
                    ValidateLifetime = jwtConfig.ValidateLifetime,
                    ValidateIssuerSigningKey = jwtConfig.ValidateIssuerSigningKey,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwtConfig.ClockSkewSeconds)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<AuthenticationModule>>();
                        logger?.LogError("JWT Authentication failed: {Exception}", context.Exception.ToString());

                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                            logger?.LogWarning("JWT Token expired");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<AuthenticationModule>>();
                        logger?.LogInformation("JWT Token validated successfully for user: {UserId}",
                            context.Principal?.FindFirst("sub")?.Value ?? "Unknown");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<AuthenticationModule>>();
                        var token = context.Token;
                        if (!string.IsNullOrEmpty(token))
                        {
                            logger?.LogInformation("JWT Token received, length: {Length}", token.Length);
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            // API Key authentication would be implemented here
            // .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { })
            .AddPolicyScheme("MultiAuth", "MultiAuth", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var logger = context.RequestServices.GetService<ILogger<AuthenticationModule>>();
                    var authorization = context.Request.Headers["Authorization"].ToString();

                    logger?.LogInformation("MultiAuth selector called. Authorization header: {AuthHeader}",
                        string.IsNullOrEmpty(authorization) ? "EMPTY" : $"Bearer {authorization.Substring(0, Math.Min(20, authorization.Length))}...");

                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                    {
                        var token = authorization.Substring("Bearer ".Length).Trim();

                        // JWT tokens have exactly 2 dots (3 parts: header.payload.signature)
                        var parts = token.Split('.');
                        logger?.LogInformation("Token parts count: {PartsCount}", parts.Length);

                        if (parts.Length == 3)
                        {
                            logger?.LogInformation("Routing to JwtBearer scheme");
                            return "JwtBearer";
                        }

                        logger?.LogInformation("Routing to ApiKey scheme");
                        return "ApiKey";
                    }

                    logger?.LogInformation("No auth header, defaulting to JwtBearer scheme");
                    return "JwtBearer";
                };
            });
        }

        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.Tier1Access, 
                policy => policy.RequireRole("Tier1", "Tier2", "Tier3"));
            
            options.AddPolicy(PolicyNames.Tier2Access, 
                policy => policy.RequireRole("Tier2", "Tier3"));
            
            options.AddPolicy(PolicyNames.Tier3Access, 
                policy => policy.RequireRole("Tier3"));
            
            options.AddPolicy(PolicyNames.ProviderAccess, 
                policy => policy.RequireRole("Provider"));
            
            options.AddPolicy(PolicyNames.Tier1OrProvider, 
                policy => policy.RequireAssertion(context =>
                    context.User.IsInRole("Tier1") || 
                    context.User.IsInRole("Tier2") || 
                    context.User.IsInRole("Tier3") || 
                    context.User.IsInRole("Provider")));
            
            options.AddPolicy(PolicyNames.AnyAuthenticated, 
                policy => policy.RequireAuthenticatedUser());
        });
    }
}