using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services.Authentication;

public interface IUserAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateUserAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}