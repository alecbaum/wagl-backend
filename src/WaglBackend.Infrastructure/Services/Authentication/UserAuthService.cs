using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Core.Molecules.Exceptions;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Services.Authentication;

public class UserAuthService : IUserAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserAuthService> _logger;

    public UserAuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        ApplicationDbContext context,
        ILogger<UserAuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt for non-existent or inactive user: {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Login attempt for locked out user: {Email}", request.Email);
                    throw new UnauthorizedException("Account is locked due to multiple failed login attempts");
                }
                
                _logger.LogWarning("Invalid login attempt for user: {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate tokens
            var accessToken = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7); // Refresh token expiry

            // Store refresh token
            await _jwtService.StoreRefreshTokenAsync(refreshToken, user.Id, expiresAt, cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Access token expiry
                User = MapToUserProfileResponse(user)
            };
        }
        catch (Exception ex) when (ex is not UnauthorizedException)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new BusinessRuleException("UserAlreadyExists", "A user with this email already exists");
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = false, // Set to true for now, implement email confirmation later
                FirstName = request.FirstName,
                LastName = request.LastName,
                TierLevel = TierLevel.FromTier(request.RequestedTier),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
                throw new BusinessRuleException("UserCreationFailed", $"User registration failed: {errors}");
            }

            // Add user to role based on tier
            await _userManager.AddToRoleAsync(user, request.RequestedTier.ToString());

            // Generate tokens for immediate login
            var accessToken = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            await _jwtService.StoreRefreshTokenAsync(refreshToken, user.Id, expiresAt, cancellationToken);

            _logger.LogInformation("User {UserId} registered successfully with tier {Tier}", user.Id, request.RequestedTier);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = MapToUserProfileResponse(user)
            };
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the refresh token
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (storedToken == null || !storedToken.IsActive)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            if (!storedToken.User.IsActive)
            {
                throw new UnauthorizedException("User account is inactive");
            }

            // Revoke the used refresh token
            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

            // Generate new tokens
            var accessToken = _jwtService.GenerateToken(storedToken.User);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            await _jwtService.StoreRefreshTokenAsync(newRefreshToken, storedToken.User.Id, expiresAt, cancellationToken);

            _logger.LogDebug("Tokens refreshed for user {UserId}", storedToken.User.Id);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = MapToUserProfileResponse(storedToken.User)
            };
        }
        catch (Exception ex) when (ex is not UnauthorizedException)
        {
            _logger.LogError(ex, "Error refreshing token");
            throw;
        }
    }

    public async Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                await _jwtService.RevokeAllUserTokensAsync(userGuid, cancellationToken);
                _logger.LogInformation("User {UserId} logged out successfully", userGuid);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ValidateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user {Email}", email);
            return false;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return null;
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _userManager.FindByIdAsync(userId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return null;
        }
    }

    private static UserProfileResponse MapToUserProfileResponse(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            TierLevel = user.TierLevel.Tier,
            AvailableFeatures = user.TierLevel.GetAvailableFeatures(),
            HourlyRateLimit = user.TierLevel.GetHourlyRateLimit(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            SubscriptionExpiresAt = user.SubscriptionExpiresAt
        };
    }
}