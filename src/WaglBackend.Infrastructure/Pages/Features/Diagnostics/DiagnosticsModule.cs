using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaglBackend.Infrastructure.Pages.Extensions;

namespace WaglBackend.Infrastructure.Pages.Features.Diagnostics;

public class DiagnosticsModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Explicitly ensure DiagnosticController is available
        // Add any diagnostic-specific services here if needed
        services.AddTransient<Controllers.DiagnosticController>();
    }
}