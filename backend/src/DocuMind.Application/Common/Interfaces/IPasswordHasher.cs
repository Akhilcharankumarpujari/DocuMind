namespace DocuMind.Application.Common.Interfaces;

/// <summary>
/// Cryptographic password hashing and verification abstraction using Argon2id.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password using Argon2id with a cryptographically secure random salt.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies whether the provided plaintext password matches the stored Argon2id hash.
    /// </summary>
    bool VerifyPassword(string password, string storedHash);

    /// <summary>
    /// Validates password complexity: minimum 8 characters, with uppercase, lowercase, digit, and special character.
    /// </summary>
    (bool IsValid, string? ErrorMessage) ValidatePassword(string password);
}
