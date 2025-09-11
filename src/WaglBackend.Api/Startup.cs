using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Asp.Versioning;
using WaglBackend.Infrastructure.Pages.Extensions;
using WaglBackend.Infrastructure.Templates.Middleware;

namespace WaglBackend.Api;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add services following atomic design pattern
        services.AddAtomicDesignServices(Configuration);

        // Add controllers
        services.AddControllers(options =>
        {
            // Add filters (would be implemented in actual filters)
            // options.Filters.Add<ValidationFilter>();
            // options.Filters.Add<AuditFilter>();
        });

        // Add API versioning with API Explorer
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Wagl Backend API", 
                Version = "v1",
                Description = "A .NET Core 9 Web API with hybrid authentication and tiered user access",
                Contact = new OpenApiContact
                {
                    Name = "Wagl Backend Team",
                    Email = "api@wagl.com"
                }
            });
            
            // Add JWT authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            
            // Add API Key authentication
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your API key in the text input below.\r\n\r\nExample: \"Bearer wagl_abcd1234...\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Add health checks
        services.AddHealthChecks()
            .AddNpgSql(
                Configuration.GetConnectionString("PostgreSQL") ?? string.Empty,
                name: "postgresql",
                tags: new[] { "database" })
            .AddRedis(
                Configuration.GetConnectionString("Redis") ?? string.Empty,
                name: "redis",
                tags: new[] { "cache" });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://localhost:3001") // Add your frontend URLs
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // Configure rate limiting (basic setup - actual implementation would be in services)
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("DefaultPolicy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromHours(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wagl Backend API V1");
                c.RoutePrefix = string.Empty; // Set Swagger UI as the default page
            });
        }

        // Add custom middleware
        app.UseMiddleware<ErrorHandlingMiddleware>();
        // app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseRateLimiter();

        // app.UseMiddleware<ApiUsageTrackingMiddleware>();
        // app.UseMiddleware<RateLimitingMiddleware>(); // Temporarily disabled

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            
            // Add health check endpoints
            endpoints.MapHealthChecks("/health");
            endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
            endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => false
            });
        });
    }
}