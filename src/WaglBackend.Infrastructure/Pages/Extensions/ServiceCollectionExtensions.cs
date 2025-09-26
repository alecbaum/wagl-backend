using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Molecules.Configurations;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Domain.Organisms.Services.Caching;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Pages.Features.Authentication;
using WaglBackend.Infrastructure.Pages.Features.UserManagement;
using WaglBackend.Infrastructure.Pages.Features.ProviderManagement;
using WaglBackend.Infrastructure.Pages.Features.Analytics;
using WaglBackend.Infrastructure.Pages.Features.Diagnostics;
using WaglBackend.Infrastructure.Persistence.Context;
using WaglBackend.Infrastructure.Persistence.Seeding;
using WaglBackend.Infrastructure.Services.Authentication;
using WaglBackend.Infrastructure.Services.Caching;
using WaglBackend.Infrastructure.Services.Resilience;
using Polly;
using Polly.Extensions.Http;

namespace WaglBackend.Infrastructure.Pages.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAtomicDesignServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register Molecules - Configuration objects
        services.Configure<JwtConfiguration>(
            configuration.GetSection(JwtConfiguration.SectionName));
        services.Configure<RedisConfiguration>(
            configuration.GetSection(RedisConfiguration.SectionName));
        services.Configure<RateLimitConfiguration>(
            configuration.GetSection(RateLimitConfiguration.SectionName));
        services.Configure<DatabaseConfiguration>(
            configuration.GetSection(DatabaseConfiguration.SectionName));
        services.Configure<UAIConfiguration>(
            configuration.GetSection(UAIConfiguration.SectionName));

        // Register Entity Framework DbContext
        // Configure Npgsql data source for dynamic JSON serialization
        var connectionString = configuration.GetConnectionString("PostgreSQL");

        // Build connection string from environment variables if not properly set
        if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("${"))
        {
            var host = configuration["DATABASE_HOST"];
            var port = configuration["DATABASE_PORT"] ?? "5432";
            var database = configuration["DATABASE_NAME"];
            var username = configuration["DATABASE_USER"];
            var password = configuration["DATABASE_PASSWORD"];
            var sslMode = configuration["DATABASE_SSL_MODE"] ?? "Require";

            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(database) &&
                !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode}";
            }
            else
            {
                throw new InvalidOperationException("Database connection string could not be built. Missing required environment variables: DATABASE_HOST, DATABASE_NAME, DATABASE_USER, DATABASE_PASSWORD");
            }
        }

        // Configure Npgsql data source with connection pooling for Aurora Serverless
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();

        // Add connection pooling configuration for Aurora Serverless v2
        dataSourceBuilder.ConnectionStringBuilder.Pooling = true;
        dataSourceBuilder.ConnectionStringBuilder.MinPoolSize = 5;  // Maintain warm connections
        dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 50; // Allow scaling
        dataSourceBuilder.ConnectionStringBuilder.ConnectionIdleLifetime = 300; // 5 minutes
        dataSourceBuilder.ConnectionStringBuilder.ConnectionPruningInterval = 10; // 10 seconds

        services.AddSingleton(dataSourceBuilder.Build());

        // Use DbContext pooling for better performance
        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<Npgsql.NpgsqlDataSource>();
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("WaglBackend.Infrastructure");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30); // 30 second timeout for Aurora Serverless
            });

            // Configure for development/production
            if (configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            if (configuration.GetValue<bool>("Database:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }

            // Suppress pending model changes warning for development
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }, poolSize: 128); // DbContext pool size for high concurrency

        // Register ASP.NET Core Identity
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register Redis Cache
        var redisConfig = configuration.GetSection(RedisConfiguration.SectionName).Get<RedisConfiguration>();
        var redisConnectionString = redisConfig?.ConnectionString;

        // Check for Redis connection string from various sources if not set in config
        if (string.IsNullOrEmpty(redisConnectionString) || redisConnectionString.Contains("${"))
        {
            redisConnectionString = configuration.GetConnectionString("Redis") ?? configuration["REDIS_CONNECTION_STRING"];
        }

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = redisConfig?.InstanceName ?? "WaglBackendCache";
            });
        }
        else
        {
            // Fallback to in-memory cache if Redis is not configured
            services.AddMemoryCache();
        }

        // Register Organisms - Services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IChatCacheService, WaglBackend.Infrastructure.Services.Caching.ChatCacheService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IUserAuthService, UserAuthService>();
        services.AddScoped<DatabaseSeeder>();
        // services.AddScoped<IUserService, UserService>();
        // services.AddScoped<IProviderService, ProviderService>();
        // services.AddScoped<ITierManagementService, TierManagementService>();
        // services.AddScoped<IRateLimitService, TierRateLimitService>();

        // Register Chat Services
        services.AddScoped<IChatSessionService, WaglBackend.Infrastructure.Services.ChatSessionService>();
        services.AddScoped<IRoomAllocationService, WaglBackend.Infrastructure.Services.RoomAllocationService>();
        services.AddScoped<IInviteManagementService, WaglBackend.Infrastructure.Services.InviteManagementService>();
        services.AddScoped<IParticipantTrackingService, WaglBackend.Infrastructure.Services.ParticipantTrackingService>();
        services.AddScoped<IChatMessageService, WaglBackend.Infrastructure.Services.ChatMessageService>();

        // Register UAI Integration Services
        // Register UAI Resilience Service
        services.AddSingleton<UAIResilienceService>();

        // Register HttpClient for UAI with Polly retry policies
        services.AddHttpClient<IUAIIntegrationService, WaglBackend.Infrastructure.Services.UAIIntegrationService>()
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var resilienceService = serviceProvider.GetRequiredService<UAIResilienceService>();
                return resilienceService.GetResiliencePolicy();
            });

        services.AddScoped<IUAIWebhookService, WaglBackend.Infrastructure.Services.UAIWebhookService>();
        services.AddScoped<ISystemParticipantService, WaglBackend.Infrastructure.Services.SystemParticipantService>();

        // Register Organisms - Repositories (would be implemented in actual repositories)
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IProviderRepository, ProviderRepository>();
        // services.AddScoped<IApiUsageRepository, ApiUsageRepository>();

        // Register Chat Repositories
        services.AddScoped<IChatSessionRepository, WaglBackend.Infrastructure.Persistence.Repositories.ChatSessionRepository>();
        services.AddScoped<IChatRoomRepository, WaglBackend.Infrastructure.Persistence.Repositories.ChatRoomRepository>();
        services.AddScoped<ISessionInviteRepository, WaglBackend.Infrastructure.Persistence.Repositories.SessionInviteRepository>();
        services.AddScoped<IChatMessageRepository, WaglBackend.Infrastructure.Persistence.Repositories.ChatMessageRepository>();
        services.AddScoped<IParticipantRepository, WaglBackend.Infrastructure.Persistence.Repositories.ParticipantRepository>();

        // Register Background Services
        services.AddHostedService<WaglBackend.Infrastructure.Services.Background.SessionCleanupBackgroundService>();
        services.AddHostedService<WaglBackend.Infrastructure.Services.Background.SessionSchedulerBackgroundService>();
        services.AddHostedService<WaglBackend.Infrastructure.Services.Background.DatabaseWarmupService>();

        // Register Authorization Handlers
        services.AddScoped<IAuthorizationHandler, WaglBackend.Infrastructure.Templates.Authorization.SessionParticipantHandler>();
        services.AddScoped<IAuthorizationHandler, WaglBackend.Infrastructure.Templates.Authorization.RoomParticipantHandler>();

        // Register Pages - Feature Modules
        services.RegisterModule<AuthenticationModule>(configuration);
        services.RegisterModule<UserManagementModule>(configuration);
        services.RegisterModule<ProviderManagementModule>(configuration);
        services.RegisterModule<AnalyticsModule>(configuration);
        services.RegisterModule<DiagnosticsModule>(configuration);

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

public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}