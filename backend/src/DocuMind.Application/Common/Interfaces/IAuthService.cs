using DocuMind.Application.Common.Models;

namespace DocuMind.Application.Common.Interfaces;

public interface IAuthService
{
    Task<(AuthResponse Response, string RawRefreshToken)> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<(AuthResponse Response, string RawRefreshToken)> LoginAsync(
        LoginRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<(AuthResponse Response, string NewRawRefreshToken)> RefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<UserProfileDto> GetCurrentUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
