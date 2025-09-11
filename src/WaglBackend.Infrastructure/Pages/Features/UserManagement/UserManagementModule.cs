using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaglBackend.Infrastructure.Pages.Extensions;

namespace WaglBackend.Infrastructure.Pages.Features.UserManagement;

public class UserManagementModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register user management services
        // services.AddScoped<IUserService, UserService>();
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IValidator<User>, UserValidator>();
        // services.AddScoped<ITierManagementService, TierManagementService>();
        
        // Register user management handlers
        // services.AddScoped<CreateUserCommandHandler>();
        // services.AddScoped<UpdateUserCommandHandler>();
        // services.AddScoped<UpdateTierCommandHandler>();
        
        // Register user management validators
        // services.AddScoped<UserValidator>();
        // services.AddScoped<CreateUserRequestValidator>();
        // services.AddScoped<UpdateUserRequestValidator>();
    }
}