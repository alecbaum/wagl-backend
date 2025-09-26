using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WaglBackend.Core.Atoms.Constants;
using CustomClaimTypes = WaglBackend.Core.Atoms.Constants.ClaimTypes;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Molecules.Configurations;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Domain.Organisms.Services.Caching;
using WaglBackend.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace WaglBackend.Infrastructure.Services.Authentication;

public class JwtService : IJwtService
{
    private readonly JwtConfiguration _jwtConfig;
    private readonly ICacheService _cacheService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<JwtService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtService(
        IOptions<JwtConfiguration> jwtConfig,
        ICacheService cacheService,
        ApplicationDbContext context,
        UserManager<User> userManager,
        ILogger<JwtService> logger)
    {
        _jwtConfig = jwtConfig.Value;
        _cacheService = cacheService;
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
    }

    public string GenerateToken(User user)
    {
        try
        {
            // Get all user roles from Identity
            var userRoles = _userManager.GetRolesAsync(user).Result;

            // Determine the primary role - prioritize Admin roles, then ChatAdmin, then tier roles
            var primaryRole = user.TierLevel.Tier.ToString(); // Default to tier
            if (userRoles.Contains("Admin"))
                primaryRole = "Admin";
            else if (userRoles.Contains("ChatAdmin"))
                primaryRole = "ChatAdmin";

            var claims = new List<Claim>
            {
                new Claim(CustomClaimTypes.UserId, user.Id.ToString()),
                new Claim(CustomClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(CustomClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new Claim(CustomClaimTypes.Role, primaryRole),
                new Claim(CustomClaimTypes.AccountType, "User"),
                new Claim(CustomClaimTypes.TierLevel, user.TierLevel.Level.ToString()),
                new Claim(CustomClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim(CustomClaimTypes.Jti, Guid.NewGuid().ToString())
            };

            // Add all user roles as separate claims
            foreach (var role in userRoles)
            {
                claims.Add(new Claim("role", role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature)
            };

            // Add subscription expiry if applicable
            if (user.SubscriptionExpiresAt.HasValue)
            {
                tokenDescriptor.Subject.AddClaim(new Claim(
                    CustomClaimTypes.SubscriptionExpiresAt, 
                    ((DateTimeOffset)user.SubscriptionExpiresAt.Value).ToUnixTimeSeconds().ToString()));
            }

            // Add last login time
            if (user.LastLoginAt.HasValue)
            {
                tokenDescriptor.Subject.AddClaim(new Claim(
                    CustomClaimTypes.LastLoginAt, 
                    ((DateTimeOffset)user.LastLoginAt.Value).ToUnixTimeSeconds().ToString()));
            }

            // Add available features based on tier
            var features = string.Join(",", user.TierLevel.GetAvailableFeatures());
            tokenDescriptor.Subject.AddClaim(new Claim(CustomClaimTypes.Features, features));

            // Add rate limit tier
            tokenDescriptor.Subject.AddClaim(new Claim(CustomClaimTypes.RateLimitTier, user.TierLevel.Tier.ToString()));

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);

            _logger.LogDebug("JWT token generated for user {UserId}", user.Id);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
            throw;
        }
    }

    public string GenerateRefreshToken()
    {
        try
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token");
            throw;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = _jwtConfig.ValidateAudience,
                ValidateIssuer = _jwtConfig.ValidateIssuer,
                ValidateIssuerSigningKey = _jwtConfig.ValidateIssuerSigningKey,
                ValidateLifetime = false, // We don't validate lifetime for expired tokens
                IssuerSigningKey = _signingKey,
                ValidIssuer = _jwtConfig.Issuer,
                ValidAudience = _jwtConfig.Audience,
                ClockSkew = TimeSpan.FromSeconds(_jwtConfig.ClockSkewSeconds)
            };

            var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Invalid token format or algorithm");
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating expired token");
            return null;
        }
    }

    public async Task<bool> IsTokenValidAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check token blacklist in cache first
            var tokenHash = token.GetHashCode().ToString();
            var isBlacklisted = await _cacheService.ExistsAsync($"blacklist:{tokenHash}", cancellationToken);
            
            if (isBlacklisted)
            {
                _logger.LogWarning("Token is blacklisted");
                return false;
            }

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = _jwtConfig.ValidateAudience,
                ValidateIssuer = _jwtConfig.ValidateIssuer,
                ValidateIssuerSigningKey = _jwtConfig.ValidateIssuerSigningKey,
                ValidateLifetime = _jwtConfig.ValidateLifetime,
                IssuerSigningKey = _signingKey,
                ValidIssuer = _jwtConfig.Issuer,
                ValidAudience = _jwtConfig.Audience,
                ClockSkew = TimeSpan.FromSeconds(_jwtConfig.ClockSkewSeconds)
            };

            _tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return true;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId, cancellationToken);

            return storedToken?.IsActive == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
            return false;
        }
    }

    public async Task StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = userId,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(tokenEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Refresh token stored for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

            if (storedToken != null)
            {
                storedToken.IsRevoked = true;
                storedToken.RevokedAt = DateTime.UtcNow;
                storedToken.RevokedReason = "Manually revoked";

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Refresh token revoked: {TokenId}", storedToken.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "All tokens revoked";
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("All refresh tokens revoked for user {UserId}. Count: {Count}", userId, userTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            throw;
        }
    }
}