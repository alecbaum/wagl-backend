using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaglBackend.Infrastructure.Pages.Extensions;

namespace WaglBackend.Infrastructure.Pages.Features.ProviderManagement;

public class ProviderManagementModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register provider management services
        // services.AddScoped<IProviderService, ProviderService>();
        // services.AddScoped<IProviderRepository, ProviderRepository>();
        // services.AddScoped<IValidator<Provider>, ProviderValidator>();
        // services.AddScoped<IApiKeyService, ApiKeyService>();
        
        // Register provider management handlers
        // services.AddScoped<CreateProviderCommandHandler>();
        // services.AddScoped<UpdateProviderCommandHandler>();
        // services.AddScoped<GenerateApiKeyCommandHandler>();
        // services.AddScoped<RevokeApiKeyCommandHandler>();
        
        // Register provider management validators
        // services.AddScoped<ProviderValidator>();
        // services.AddScoped<CreateProviderRequestValidator>();
        // services.AddScoped<ApiKeyValidator>();
    }
}