using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WaglBackend.Core.Atoms.Constants;
using WaglBackend.Core.Molecules.Configurations;
using WaglBackend.Infrastructure.Pages.Extensions;

namespace WaglBackend.Infrastructure.Pages.Features.Authentication;

public class AuthenticationModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication services (would be implemented)
        // services.AddScoped<IUserAuthService, UserAuthService>();
        // services.AddScoped<IProviderAuthService, ProviderAuthService>();
        // services.AddScoped<IApiKeyService, ApiKeyService>();
        // services.AddScoped<IJwtService, JwtService>();

        var jwtConfig = configuration.GetSection(JwtConfiguration.SectionName).Get<JwtConfiguration>();
        
        if (jwtConfig != null)
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
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
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
                    var authorization = context.Request.Headers["Authorization"].ToString();
                    
                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                    {
                        var token = authorization.Substring("Bearer ".Length).Trim();
                        
                        // JWT tokens typically contain dots and are longer
                        if (token.Contains(".") && token.Length > 100)
                            return "JwtBearer";
                        
                        return "ApiKey";
                    }
                    
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