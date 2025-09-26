using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaglBackend.Infrastructure.Pages.Extensions;

namespace WaglBackend.Infrastructure.Pages.Features.Diagnostics;

public class DiagnosticsModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // No additional services needed for diagnostics
        // The controller will be auto-discovered through AddControllers()
        // This module exists to ensure the diagnostics feature is properly organized
    }
}