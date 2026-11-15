using System.Net.Mail;
using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Services;

/// <summary>
/// Secure multi-user authentication service supporting Argon2id hashing,
/// JWT access tokens, refresh token rotation, and reuse attack detection.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<(AuthResponse Response, string RawRefreshToken)> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate full name
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name cannot be empty.", nameof(request.FullName));

        // 2. Validate email format
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            throw new ArgumentException("Please provide a valid email address.", nameof(request.Email));

        // 3. Validate password complexity
        var (isValid, errorMessage) = _passwordHasher.ValidatePassword(request.Password);
        if (!isValid)
            throw new ArgumentException(errorMessage ?? "Password does not meet security requirements.", nameof(request.Password));

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        // 4. Check for existing account
        var existingUser = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            _logger.LogWarning("Registration attempt with existing email address {NormalizedEmail}", normalizedEmail);
            throw new DomainException("An account with this email address already exists.");
        }

        // 5. Hash password with Argon2id
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 6. Create User entity
        var user = new User(request.FullName, request.Email, passwordHash);
        _context.Users.Add(user);

        // 7. Issue Access Token and Refresh Token
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var (rawRefreshToken, refreshTokenEntity) = _jwtTokenService.GenerateRefreshToken(user.Id, ipAddress);
        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered successfully with ID {UserId}", user.Id);

        var profile = new UserProfileDto(user.Id, user.FullName, user.Email, user.CreatedAtUtc);
        return (new AuthResponse(accessToken, profile), rawRefreshToken);
    }

    public async Task<(AuthResponse Response, string RawRefreshToken)> LoginAsync(
        LoginRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new DomainException("Invalid email or password.");

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        // Generic error message to prevent account enumeration
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Login failed for email {NormalizedEmail} (User not found or inactive)", normalizedEmail);
            throw new DomainException("Invalid email or password.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed for User {UserId}: Invalid password attempt", user.Id);
            throw new DomainException("Invalid email or password.");
        }

        // Issue Access Token and Refresh Token
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var (rawRefreshToken, refreshTokenEntity) = _jwtTokenService.GenerateRefreshToken(user.Id, ipAddress);
        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        var profile = new UserProfileDto(user.Id, user.FullName, user.Email, user.CreatedAtUtc);
        return (new AuthResponse(accessToken, profile), rawRefreshToken);
    }

    public async Task<(AuthResponse Response, string NewRawRefreshToken)> RefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            throw new DomainException("Refresh token is required.");

        var tokenHash = _jwtTokenService.HashToken(rawRefreshToken);

        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Refresh token lookup failed: Hash not found");
            throw new DomainException("Invalid refresh token.");
        }

        // 1. Token Reuse Detection
        if (token.RevokedAtUtc != null)
        {
            _logger.LogCritical("SECURITY ALERT: Refresh token reuse detected for User {UserId}! Revoking all active sessions.", token.UserId);

            // Invalidate all active tokens for this user immediately
            var compromisedSessions = await _context.RefreshTokens
                .Where(t => t.UserId == token.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var s in compromisedSessions)
            {
                s.Revoke();
            }

            await _context.SaveChangesAsync(cancellationToken);

            throw new DomainException("Invalid or compromised refresh token. Please sign in again.");
        }

        // 2. Expiration check
        if (token.IsExpired)
        {
            _logger.LogInformation("Refresh token for User {UserId} is expired", token.UserId);
            throw new DomainException("Refresh token has expired. Please sign in again.");
        }

        // 3. User active check
        if (token.User == null || !token.User.IsActive)
        {
            _logger.LogWarning("Refresh token used for disabled/nonexistent user {UserId}", token.UserId);
            throw new DomainException("User account is inactive or disabled.");
        }

        // 4. Token Rotation: generate new refresh token and revoke the previous token
        var (newRawRefreshToken, newRefreshTokenEntity) = _jwtTokenService.GenerateRefreshToken(token.UserId, ipAddress);
        token.Revoke(newRefreshTokenEntity.Id);

        _context.RefreshTokens.Add(newRefreshTokenEntity);

        var newAccessToken = _jwtTokenService.GenerateAccessToken(token.User);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated successfully for User {UserId}", token.UserId);

        var profile = new UserProfileDto(token.User.Id, token.User.FullName, token.User.Email, token.User.CreatedAtUtc);
        return (new AuthResponse(newAccessToken, profile), newRawRefreshToken);
    }

    public async Task RevokeTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return;

        var tokenHash = _jwtTokenService.HashToken(rawRefreshToken);

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token != null && token.RevokedAtUtc == null)
        {
            token.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refresh token revoked for User {UserId}", token.UserId);
        }
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new EntityNotFoundException(nameof(User), userId);

        return new UserProfileDto(user.Id, user.FullName, user.Email, user.CreatedAtUtc);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
