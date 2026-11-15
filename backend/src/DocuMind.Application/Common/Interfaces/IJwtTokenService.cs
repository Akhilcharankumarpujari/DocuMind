using System.Security.Claims;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT access token containing user claims.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random refresh token and returns both the plaintext token (for cookie)
    /// and the hashed RefreshToken entity (for database persistence).
    /// </summary>
    (string RawToken, RefreshToken Entity) GenerateRefreshToken(Guid userId, string? ipAddress = null);

    /// <summary>
    /// Computes the SHA-256 hash of a raw token for secure database lookup.
    /// </summary>
    string HashToken(string rawToken);

    /// <summary>
    /// Extracts ClaimsPrincipal from an expired or valid JWT token without lifetime validation.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
