using WaglBackend.Core.Atoms.Entities;
using System.Security.Claims;

namespace WaglBackend.Domain.Organisms.Services.Authentication;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Task<bool> IsTokenValidAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> IsRefreshTokenValidAsync(string refreshToken, Guid userId, CancellationToken cancellationToken = default);
    Task StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}