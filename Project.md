# Project Overview

## Technology Stack

- **Framework**: .NET Core 9 Web API
- **Database**: PostgreSQL
- **Cache**: Redis
- **Authentication**: Hybrid approach using .NET Identity + Custom API Key Authentication
- **Architecture Pattern**: Atomic Design Pattern

## Atomic Design Architecture

### Design Hierarchy

1. **Atoms** - Basic building blocks (Entities, Value Objects, Enums, Constants)
2. **Molecules** - Simple combinations (DTOs, Request/Response Models, Basic Interfaces)
3. **Organisms** - Complex components (Services, Repositories, Handlers, Validators)
4. **Templates** - Page structures (Controllers, Middleware, Filters)
5. **Pages** - Complete features (API Endpoints, Feature Modules)

## Project Structure

```
src/
├── ProjectName.Api/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── ProjectName.Api.csproj
│
├── ProjectName.Core/
│   ├── Atoms/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Provider.cs
│   │   │   ├── ApiUsageLog.cs
│   │   │   └── TierFeature.cs
│   │   ├── ValueObjects/
│   │   │   ├── ApiKey.cs
│   │   │   ├── TierLevel.cs
│   │   │   ├── Email.cs
│   │   │   └── UserId.cs
│   │   ├── Enums/
│   │   │   ├── AccountTier.cs
│   │   │   ├── AccountType.cs
│   │   │   └── FeatureFlags.cs
│   │   └── Constants/
│   │       ├── CacheKeys.cs
│   │       ├── PolicyNames.cs
│   │       ├── ClaimTypes.cs
│   │       └── ErrorCodes.cs
│   │
│   ├── Molecules/
│   │   ├── DTOs/
│   │   │   ├── Request/
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   ├── RegisterRequest.cs
│   │   │   │   ├── CreateProviderRequest.cs
│   │   │   │   └── RefreshTokenRequest.cs
│   │   │   └── Response/
│   │   │       ├── AuthResponse.cs
│   │   │       ├── UserProfileResponse.cs
│   │   │       ├── ProviderResponse.cs
│   │   │       └── ApiUsageResponse.cs
│   │   ├── Interfaces/
│   │   │   ├── IAuthenticatable.cs
│   │   │   ├── ICacheable.cs
│   │   │   ├── IAuditable.cs
│   │   │   └── IRateLimited.cs
│   │   ├── Configurations/
│   │   │   ├── JwtConfiguration.cs
│   │   │   ├── RedisConfiguration.cs
│   │   │   ├── RateLimitConfiguration.cs
│   │   │   └── DatabaseConfiguration.cs
│   │   └── Exceptions/
│   │       ├── UnauthorizedException.cs
│   │       ├── TierLimitExceededException.cs
│   │       ├── InvalidApiKeyException.cs
│   │       └── BusinessRuleException.cs
│   │
│   └── ProjectName.Core.csproj
│
├── ProjectName.Domain/
│   ├── Organisms/
│   │   ├── Services/
│   │   │   ├── Authentication/
│   │   │   │   ├── IUserAuthService.cs
│   │   │   │   ├── UserAuthService.cs
│   │   │   │   ├── IProviderAuthService.cs
│   │   │   │   ├── ProviderAuthService.cs
│   │   │   │   ├── IApiKeyService.cs
│   │   │   │   ├── ApiKeyService.cs
│   │   │   │   ├── IJwtService.cs
│   │   │   │   └── JwtService.cs
│   │   │   ├── Authorization/
│   │   │   │   ├── IAuthorizationService.cs
│   │   │   │   ├── TierAuthorizationService.cs
│   │   │   │   ├── IPermissionService.cs
│   │   │   │   └── PermissionService.cs
│   │   │   ├── Caching/
│   │   │   │   ├── ICacheService.cs
│   │   │   │   ├── RedisCacheService.cs
│   │   │   │   ├── IDistributedCacheService.cs
│   │   │   │   └── DistributedCacheService.cs
│   │   │   ├── RateLimiting/
│   │   │   │   ├── IRateLimitService.cs
│   │   │   │   ├── TierRateLimitService.cs
│   │   │   │   └── ProviderRateLimitService.cs
│   │   │   └── Business/
│   │   │       ├── IUserService.cs
│   │   │       ├── UserService.cs
│   │   │       ├── IProviderService.cs
│   │   │       ├── ProviderService.cs
│   │   │       ├── ITierManagementService.cs
│   │   │       └── TierManagementService.cs
│   │   │
│   │   ├── Repositories/
│   │   │   ├── IUserRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   ├── IProviderRepository.cs
│   │   │   ├── ProviderRepository.cs
│   │   │   ├── IApiUsageRepository.cs
│   │   │   ├── ApiUsageRepository.cs
│   │   │   └── Base/
│   │   │       ├── IRepository.cs
│   │   │       └── BaseRepository.cs
│   │   │
│   │   ├── Handlers/
│   │   │   ├── Authentication/
│   │   │   │   ├── ApiKeyAuthenticationHandler.cs
│   │   │   │   ├── JwtAuthenticationHandler.cs
│   │   │   │   └── MultiAuthenticationHandler.cs
│   │   │   └── Commands/
│   │   │       ├── CreateUserCommandHandler.cs
│   │   │       ├── CreateProviderCommandHandler.cs
│   │   │       ├── UpdateTierCommandHandler.cs
│   │   │       └── RevokeApiKeyCommandHandler.cs
│   │   │
│   │   └── Validators/
│   │       ├── UserValidator.cs
│   │       ├── ProviderValidator.cs
│   │       ├── ApiKeyValidator.cs
│   │       └── RequestValidator.cs
│   │
│   └── ProjectName.Domain.csproj
│
├── ProjectName.Infrastructure/
│   ├── Templates/
│   │   ├── Controllers/
│   │   │   ├── Base/
│   │   │   │   ├── BaseApiController.cs
│   │   │   │   └── AuthorizedController.cs
│   │   │   ├── Authentication/
│   │   │   │   ├── AuthController.cs
│   │   │   │   └── AccountController.cs
│   │   │   ├── Provider/
│   │   │   │   ├── ProviderController.cs
│   │   │   │   └── ProviderManagementController.cs
│   │   │   └── User/
│   │   │       ├── UserController.cs
│   │   │       ├── ProfileController.cs
│   │   │       └── TierController.cs
│   │   │
│   │   ├── Middleware/
│   │   │   ├── AuthenticationMiddleware.cs
│   │   │   ├── RateLimitingMiddleware.cs
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   └── ApiUsageTrackingMiddleware.cs
│   │   │
│   │   └── Filters/
│   │       ├── AuthorizationFilter.cs
│   │       ├── ValidationFilter.cs
│   │       ├── CacheFilter.cs
│   │       └── AuditFilter.cs
│   │
│   ├── Pages/
│   │   ├── Features/
│   │   │   ├── UserManagement/
│   │   │   │   ├── UserManagementModule.cs
│   │   │   │   ├── UserEndpoints.cs
│   │   │   │   └── UserDependencyInjection.cs
│   │   │   ├── ProviderManagement/
│   │   │   │   ├── ProviderManagementModule.cs
│   │   │   │   ├── ProviderEndpoints.cs
│   │   │   │   └── ProviderDependencyInjection.cs
│   │   │   ├── Authentication/
│   │   │   │   ├── AuthenticationModule.cs
│   │   │   │   ├── AuthEndpoints.cs
│   │   │   │   └── AuthDependencyInjection.cs
│   │   │   └── Analytics/
│   │   │       ├── AnalyticsModule.cs
│   │   │       ├── AnalyticsEndpoints.cs
│   │   │       └── AnalyticsDependencyInjection.cs
│   │   │
│   │   └── Extensions/
│   │       ├── ServiceCollectionExtensions.cs
│   │       ├── ApplicationBuilderExtensions.cs
│   │       ├── AuthenticationExtensions.cs
│   │       └── SwaggerExtensions.cs
│   │
│   ├── Persistence/
│   │   ├── Context/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── IdentityDbContext.cs
│   │   ├── Migrations/
│   │   │   └── [Migration files]
│   │   ├── Configurations/
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── ProviderConfiguration.cs
│   │   │   └── ApiUsageLogConfiguration.cs
│   │   └── Seeding/
│   │       ├── DatabaseSeeder.cs
│   │       ├── UserSeeder.cs
│   │       └── TierFeatureSeeder.cs
│   │
│   └── ProjectName.Infrastructure.csproj
│
└── ProjectName.Tests/
    ├── Unit/
    │   ├── Atoms/
    │   │   ├── Entities/
    │   │   └── ValueObjects/
    │   ├── Molecules/
    │   │   ├── DTOs/
    │   │   └── Configurations/
    │   └── Organisms/
    │       ├── Services/
    │       ├── Repositories/
    │       └── Handlers/
    ├── Integration/
    │   ├── Controllers/
    │   ├── Middleware/
    │   └── Features/
    └── ProjectName.Tests.csproj
```

## Account Types

### User Tiers (Managed by .NET Identity)
- **Tier1** - Basic access level with limited API calls and features
- **Tier2** - Standard access with increased limits and additional features
- **Tier3** - Premium access with highest limits and full feature set

### Provider Accounts (Custom Implementation)
- Static API key authentication
- No expiration on API keys
- Direct bearer token authentication
- Separate authorization pipeline from user tiers

## Atomic Design Implementation

### Atoms Layer

```csharp
// Atoms/Entities/Provider.cs
namespace ProjectName.Core.Atoms.Entities
{
    public class Provider : IAuditable
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ApiKey ApiKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
    }
}

// Atoms/ValueObjects/ApiKey.cs
namespace ProjectName.Core.Atoms.ValueObjects
{
    public class ApiKey : ValueObject
    {
        public string Value { get; }
        public string HashedValue { get; }
        
        private ApiKey(string value)
        {
            Value = value;
            HashedValue = BCrypt.Net.BCrypt.HashPassword(value);
        }
        
        public static ApiKey Create() => new(GenerateSecureKey());
        public static ApiKey FromString(string value) => new(value);
        
        public bool Verify(string plainTextKey) => 
            BCrypt.Net.BCrypt.Verify(plainTextKey, HashedValue);
    }
}

// Atoms/Enums/AccountTier.cs
namespace ProjectName.Core.Atoms.Enums
{
    public enum AccountTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3
    }
}

// Atoms/Constants/PolicyNames.cs
namespace ProjectName.Core.Atoms.Constants
{
    public static class PolicyNames
    {
        public const string Tier1Access = "Tier1Access";
        public const string Tier2Access = "Tier2Access";
        public const string Tier3Access = "Tier3Access";
        public const string ProviderAccess = "ProviderAccess";
        public const string Tier1OrProvider = "Tier1OrProvider";
    }
}
```

### Molecules Layer

```csharp
// Molecules/DTOs/Request/CreateProviderRequest.cs
namespace ProjectName.Core.Molecules.DTOs.Request
{
    public class CreateProviderRequest : IValidatable
    {
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Name { get; set; }
        
        [EmailAddress]
        public string ContactEmail { get; set; }
        
        public string[] AllowedIpAddresses { get; set; }
    }
}

// Molecules/Configurations/JwtConfiguration.cs
namespace ProjectName.Core.Molecules.Configurations
{
    public class JwtConfiguration
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }
    }
}

// Molecules/Interfaces/ICacheable.cs
namespace ProjectName.Core.Molecules.Interfaces
{
    public interface ICacheable
    {
        string CacheKey { get; }
        TimeSpan? CacheDuration { get; }
        bool ShouldCache { get; }
    }
}
```

### Organisms Layer

```csharp
// Organisms/Services/Authentication/ApiKeyService.cs
namespace ProjectName.Domain.Organisms.Services.Authentication
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IProviderRepository _providerRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ApiKeyService> _logger;
        
        public ApiKeyService(
            IProviderRepository providerRepository,
            ICacheService cacheService,
            ILogger<ApiKeyService> logger)
        {
            _providerRepository = providerRepository;
            _cacheService = cacheService;
            _logger = logger;
        }
        
        public async Task<Provider> ValidateApiKeyAsync(string apiKey)
        {
            // Check cache first
            var cacheKey = $"{CacheKeys.ApiKeyPrefix}:{apiKey.GetHashCode()}";
            var cached = await _cacheService.GetAsync<Provider>(cacheKey);
            
            if (cached != null)
                return cached;
            
            // Validate against database
            var provider = await _providerRepository.GetByApiKeyAsync(apiKey);
            
            if (provider != null && provider.IsActive)
            {
                // Cache for 5 minutes
                await _cacheService.SetAsync(cacheKey, provider, TimeSpan.FromMinutes(5));
                
                // Update last accessed
                await _providerRepository.UpdateLastAccessedAsync(provider.Id);
                
                return provider;
            }
            
            return null;
        }
        
        public async Task<ApiKey> GenerateApiKeyAsync(Guid providerId)
        {
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
                throw new NotFoundException($"Provider {providerId} not found");
            
            var apiKey = ApiKey.Create();
            provider.ApiKey = apiKey;
            
            await _providerRepository.UpdateAsync(provider);
            
            // Invalidate cache
            await _cacheService.RemoveByPatternAsync($"{CacheKeys.ApiKeyPrefix}:*");
            
            return apiKey;
        }
    }
}

// Organisms/Handlers/Authentication/ApiKeyAuthenticationHandler.cs
namespace ProjectName.Domain.Organisms.Handlers.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly IApiKeyService _apiKeyService;
        
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IApiKeyService apiKeyService)
            : base(options, logger, encoder, clock)
        {
            _apiKeyService = apiKeyService;
        }
        
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.NoResult();
            
            string authorizationHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authorizationHeader))
                return AuthenticateResult.NoResult();
            
            if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.NoResult();
            
            var apiKey = authorizationHeader.Substring("Bearer ".Length).Trim();
            
            var provider = await _apiKeyService.ValidateApiKeyAsync(apiKey);
            if (provider == null)
                return AuthenticateResult.Fail("Invalid API key");
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, provider.Id.ToString()),
                new Claim(ClaimTypes.Name, provider.Name),
                new Claim(ClaimTypes.Role, "Provider"),
                new Claim("AccountType", "Provider")
            };
            
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            return AuthenticateResult.Success(ticket);
        }
    }
}

// Organisms/Repositories/ProviderRepository.cs
namespace ProjectName.Domain.Organisms.Repositories
{
    public class ProviderRepository : BaseRepository<Provider>, IProviderRepository
    {
        public ProviderRepository(ApplicationDbContext context) : base(context)
        {
        }
        
        public async Task<Provider> GetByApiKeyAsync(string apiKey)
        {
            return await _context.Providers
                .FirstOrDefaultAsync(p => p.ApiKey.Value == apiKey && p.IsActive);
        }
        
        public async Task UpdateLastAccessedAsync(Guid providerId)
        {
            var provider = await GetByIdAsync(providerId);
            if (provider != null)
            {
                provider.LastAccessedAt = DateTime.UtcNow;
                await UpdateAsync(provider);
            }
        }
    }
}
```

### Templates Layer

```csharp
// Templates/Controllers/Base/BaseApiController.cs
namespace ProjectName.Infrastructure.Templates.Controllers.Base
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected ILogger Logger { get; }
        
        protected BaseApiController(ILogger logger)
        {
            Logger = logger;
        }
        
        protected ActionResult<T> HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return result.Error switch
            {
                ErrorType.NotFound => NotFound(result.ErrorMessage),
                ErrorType.Validation => BadRequest(result.ErrorMessage),
                ErrorType.Unauthorized => Unauthorized(result.ErrorMessage),
                _ => StatusCode(500, "An error occurred")
            };
        }
    }
}

// Templates/Middleware/RateLimitingMiddleware.cs
namespace ProjectName.Infrastructure.Templates.Middleware
{
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
                var accountType = context.User.FindFirst("AccountType")?.Value;
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
                    userId, accountType, context.Request.Path);
                
                if (!rateLimitResult.IsAllowed)
                {
                    context.Response.StatusCode = 429;
                    await context.Response.WriteAsync("Rate limit exceeded");
                    return;
                }
                
                context.Response.Headers.Add("X-RateLimit-Limit", 
                    rateLimitResult.Limit.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", 
                    rateLimitResult.Remaining.ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", 
                    rateLimitResult.ResetTime.ToString());
            }
            
            await _next(context);
        }
    }
}
```

### Pages Layer

```csharp
// Pages/Features/Authentication/AuthenticationModule.cs
namespace ProjectName.Infrastructure.Pages.Features.Authentication
{
    public class AuthenticationModule : IModule
    {
        public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register authentication services
            services.AddScoped<IUserAuthService, UserAuthService>();
            services.AddScoped<IProviderAuthService, ProviderAuthService>();
            services.AddScoped<IApiKeyService, ApiKeyService>();
            services.AddScoped<IJwtService, JwtService>();
            
            // Configure authentication
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "MultiAuth";
                options.DefaultChallengeScheme = "MultiAuth";
            })
            .AddJwtBearer("JwtBearer", options =>
            {
                var jwtConfig = configuration.GetSection("Authentication:Jwt")
                    .Get<JwtConfiguration>();
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtConfig.SecretKey))
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                "ApiKey", options => { })
            .AddPolicyScheme("MultiAuth", "MultiAuth", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authorization = context.Request.Headers["Authorization"].ToString();
                    
                    if (!string.IsNullOrEmpty(authorization) && 
                        authorization.StartsWith("Bearer "))
                    {
                        var token = authorization.Substring("Bearer ".Length).Trim();
                        
                        // JWT tokens are typically longer and contain dots
                        if (token.Contains(".") && token.Length > 100)
                            return "JwtBearer";
                        
                        return "ApiKey";
                    }
                    
                    return "JwtBearer";
                };
            });
            
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
            });
        }
        
        public void ConfigureMiddleware(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
}

// Pages/Extensions/ServiceCollectionExtensions.cs
namespace ProjectName.Infrastructure.Pages.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAtomicDesignServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Register Atoms (typically don't need DI registration)
            
            // Register Molecules
            services.Configure<JwtConfiguration>(
                configuration.GetSection("Authentication:Jwt"));
            services.Configure<RedisConfiguration>(
                configuration.GetSection("Redis"));
            services.Configure<RateLimitConfiguration>(
                configuration.GetSection("RateLimiting"));
            
            // Register Organisms - Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProviderService, ProviderService>();
            services.AddScoped<ITierManagementService, TierManagementService>();
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<IRateLimitService, TierRateLimitService>();
            
            // Register Organisms - Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProviderRepository, ProviderRepository>();
            services.AddScoped<IApiUsageRepository, ApiUsageRepository>();
            
            // Register Organisms - Validators
            services.AddScoped<IValidator<User>, UserValidator>();
            services.AddScoped<IValidator<Provider>, ProviderValidator>();
            
            // Register Pages - Feature Modules
            services.RegisterModule<AuthenticationModule>(configuration);
            services.RegisterModule<UserManagementModule>(configuration);
            services.RegisterModule<ProviderManagementModule>(configuration);
            services.RegisterModule<AnalyticsModule>(configuration);
            
            return services;
        }
        
        public static IServiceCollection RegisterModule<TModule>(
            this IServiceCollection services, 
            IConfiguration configuration) where TModule : IModule, new()
        {
            var module = new TModule();
            module.RegisterServices(services, configuration);
            return services;
        }
    }
}
```

## Database Design with Atomic Pattern

### Entity Configurations Following Atomic Design

```csharp
// Persistence/Configurations/ProviderConfiguration.cs
namespace ProjectName.Infrastructure.Persistence.Configurations
{
    public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
    {
        public void Configure(EntityTypeBuilder<Provider> builder)
        {
            builder.ToTable("Providers");
            
            builder.HasKey(p => p.Id);
            
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(255);
            
            // Value object configuration
            builder.OwnsOne(p => p.ApiKey, apiKey =>
            {
                apiKey.Property(a => a.Value)
                    .HasColumnName("ApiKey")
                    .HasMaxLength(255);
                
                apiKey.Property(a => a.HashedValue)
                    .HasColumnName("ApiKeyHash")
                    .IsRequired();
                
                apiKey.HasIndex(a => a.Value)
                    .IsUnique();
            });
            
            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);
            
            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            builder.HasIndex(p => p.Name);
        }
    }
}
```

## Program.cs Configuration

```csharp
// Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services following atomic design pattern
builder.Services.AddAtomicDesignServices(builder.Configuration);

// Add controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<AuditFilter>();
});

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL"))
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Configure rate limiting policies for each tier
    ConfigureRateLimitingPolicies(options, builder.Configuration);
});

var app = builder.Build();

// Configure middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseMiddleware<ApiUsageTrackingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Seed database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
```

## Testing Structure Following Atomic Design

```csharp
// Tests/Unit/Organisms/Services/ApiKeyServiceTests.cs
namespace ProjectName.Tests.Unit.Organisms.Services
{
    public class ApiKeyServiceTests
    {
        private readonly Mock<IProviderRepository> _providerRepositoryMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly ApiKeyService _sut;
        
        public ApiKeyServiceTests()
        {
            _providerRepositoryMock = new Mock<IProviderRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _sut = new ApiKeyService(
                _providerRepositoryMock.Object,
                _cacheServiceMock.Object,
                Mock.Of<ILogger<ApiKeyService>>()
            );
        }
        
        [Fact]
        public async Task ValidateApiKeyAsync_WhenCached_ReturnsFromCache()
        {
            // Arrange
            var apiKey = "test-api-key";
            var provider = new Provider { Id = Guid.NewGuid(), Name = "Test Provider" };
            
            _cacheServiceMock
                .Setup(x => x.GetAsync<Provider>(It.IsAny<string>()))
                .ReturnsAsync(provider);
            
            // Act
            var result = await _sut.ValidateApiKeyAsync(apiKey);
            
            // Assert
            result.Should().BeEquivalentTo(provider);
            _providerRepositoryMock.Verify(
                x => x.GetByApiKeyAsync(It.IsAny<string>()), 
                Times.Never);
        }
    }
}
```

## Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=apidb;Username=api_user;Password=****",
    "Redis": "localhost:6379,abortConnect=false"
  },
  "Authentication": {
    "Jwt": {
      "SecretKey": "your-256-bit-secret-key-for-jwt-token-generation",
      "Issuer": "your-api",
      "Audience": "your-api-users",
      "ExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 7
    },
    "ApiKey": {
      "HeaderName": "Authorization",
      "Prefix": "Bearer"
    }
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "EnableRedisRateLimiting": true,
    "Policies": {
      "Tier1": {
        "PermitLimit": 100,
        "Window": "01:00:00"
      },
      "Tier2": {
        "PermitLimit": 500,
        "Window": "01:00:00"
      },
      "Tier3": {
        "PermitLimit": 2000,
        "Window": "01:00:00"
      },
      "Provider": {
        "PermitLimit": 10000,
        "Window": "01:00:00"
      }
    }
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "ApiCache",
    "DefaultExpirationMinutes": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Benefits of Atomic Design Pattern

1. **Clear Separation of Concerns**: Each layer has a specific responsibility
2. **High Reusability**: Atoms and Molecules can be reused across different Organisms
3. **Testability**: Each component can be tested in isolation
4. **Scalability**: New features can be added as new Pages without affecting existing code
5. **Maintainability**: Changes to business logic don't affect presentation layer
6. **Consistency**: Enforces consistent patterns across the entire application

## Development Workflow with Atomic Design

### Creating New Features

1. **Identify Atoms**: Define entities, value objects, and enums
2. **Create Molecules**: Build DTOs, interfaces, and configurations
3. **Implement Organisms**: Develop services, repositories, and handlers
4. **Setup Templates**: Create controllers, middleware, and filters
5. **Compose Pages**: Combine everything into feature modules

### Example: Adding a New Feature

```bash
# 1. Create Atom
src/ProjectName.Core/Atoms/Entities/Invoice.cs

# 2. Create Molecules
src/ProjectName.Core/Molecules/DTOs/Request/CreateInvoiceRequest.cs
src/ProjectName.Core/Molecules/DTOs/Response/InvoiceResponse.cs

# 3. Create Organisms
src/ProjectName.Domain/Organisms/Services/Business/InvoiceService.cs
src/ProjectName.Domain/Organisms/Repositories/InvoiceRepository.cs

# 4. Create Templates
src/ProjectName.Infrastructure/Templates/Controllers/InvoiceController.cs

# 5. Create Page
src/ProjectName.Infrastructure/Pages/Features/Invoicing/InvoicingModule.cs
```

## Best Practices

1. **Keep Atoms Pure**: No dependencies, only data and behavior
2. **Molecules Should Be Simple**: Combine atoms but don't add complex logic
3. **Organisms Handle Business Logic**: All complex operations go here
4. **Templates Are Thin**: Minimal logic, mainly orchestration
5. **Pages Compose Features**: Wire up all components for a complete feature
6. **Use Dependency Injection**: Let the DI container manage object lifetimes
7. **Follow SOLID Principles**: Especially Single Responsibility and Dependency Inversion
8. **Write Tests at Each Level**: Unit tests for Atoms/Molecules, integration tests for Organisms/Pages