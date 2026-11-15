using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocuMind.Application.Common.Interfaces;
using DocuMind.Infrastructure.Configuration;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Security;

/// <summary>
/// Cryptographic implementation of password hashing using Argon2id with constant-time verification.
/// Conforms to the standard Argon2 modular crypt format:
/// $argon2id$v=19$m=&lt;memory&gt;,t=&lt;iterations&gt;,p=&lt;parallelism&gt;$&lt;salt_b64&gt;$&lt;hash_b64&gt;
/// </summary>
public class Argon2idPasswordHasher : IPasswordHasher
{
    private readonly Argon2Options _options;
    private readonly ILogger<Argon2idPasswordHasher> _logger;

    public Argon2idPasswordHasher(IOptions<Argon2Options> options, ILogger<Argon2idPasswordHasher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public (bool IsValid, string? ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Password is required.");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters long.");

        if (password.Length > 128)
            return (false, "Password cannot exceed 128 characters.");

        if (!password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            return (false, "Password must contain at least one numeric digit.");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return (false, "Password must contain at least one special character.");

        return (true, null);
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        // 1. Generate cryptographically secure random salt
        var salt = new byte[_options.SaltSize];
        RandomNumberGenerator.Fill(salt);

        // 2. Hash password bytes with Argon2id
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            MemorySize = _options.MemorySize,
            Iterations = _options.Iterations,
            DegreeOfParallelism = _options.DegreeOfParallelism
        };

        var hashBytes = argon2.GetBytes(_options.HashLength);

        // 3. Format as standard modular crypt string
        var saltB64 = Convert.ToBase64String(salt);
        var hashB64 = Convert.ToBase64String(hashBytes);

        return $"$argon2id$v=19$m={_options.MemorySize},t={_options.Iterations},p={_options.DegreeOfParallelism}${saltB64}${hashB64}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            // Expected format: $argon2id$v=19$m=65536,t=3,p=1$<saltB64>$<hashB64>
            var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
            {
                _logger.LogWarning("Invalid stored hash format.");
                return false;
            }

            var algorithm = parts[0];
            if (!string.Equals(algorithm, "argon2id", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unsupported password hash algorithm: {Algorithm}", algorithm);
                return false;
            }

            // Parse params (m=...,t=...,p=...)
            var paramPart = parts[2];
            var paramMatches = Regex.Matches(paramPart, @"(m|t|p)=(\d+)");
            int memorySize = _options.MemorySize;
            int iterations = _options.Iterations;
            int parallelism = _options.DegreeOfParallelism;

            foreach (Match match in paramMatches)
            {
                var key = match.Groups[1].Value;
                var val = int.Parse(match.Groups[2].Value);
                if (key == "m") memorySize = val;
                else if (key == "t") iterations = val;
                else if (key == "p") parallelism = val;
            }

            var salt = Convert.FromBase64String(parts[3]);
            var expectedHash = Convert.FromBase64String(parts[4]);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                MemorySize = memorySize,
                Iterations = iterations,
                DegreeOfParallelism = parallelism
            };

            var computedHash = argon2.GetBytes(expectedHash.Length);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Argon2id password verification.");
            return false;
        }
    }
}
