namespace DocuMind.Application.Common.Models;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string? RefreshToken = null);

public record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    DateTime CreatedAtUtc);

public record AuthResponse(
    string AccessToken,
    UserProfileDto User);
