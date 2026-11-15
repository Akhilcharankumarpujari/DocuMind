using DocuMind.Infrastructure.Configuration;
using DocuMind.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class Argon2idPasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher;

    public Argon2idPasswordHasherTests()
    {
        // Use fast test configuration for unit tests
        var options = Options.Create(new Argon2Options
        {
            MemorySize = 1024, // 1 MB for fast unit testing
            Iterations = 2,
            DegreeOfParallelism = 1,
            SaltSize = 16,
            HashLength = 32
        });

        _hasher = new Argon2idPasswordHasher(options, NullLogger<Argon2idPasswordHasher>.Instance);
    }

    [Fact]
    public void HashPassword_ValidPassword_ShouldReturnValidModularFormat()
    {
        // Act
        var hash = _hasher.HashPassword("SecurePassword123!");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$argon2id$v=19$");
        hash.Split('$').Length.Should().Be(6); // empty, argon2id, v=19, m=..., salt, hash
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ShouldProduceDifferentHashesDueToSalt()
    {
        // Act
        var hash1 = _hasher.HashPassword("SecurePassword123!");
        var hash2 = _hasher.HashPassword("SecurePassword123!");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "CorrectHorseBatteryStaple99!";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("RightPassword123!");

        // Act
        var result = _hasher.VerifyPassword("WrongPassword123!", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("short1!", false, "at least 8 characters")]
    [InlineData("nouppercase123!", false, "uppercase")]
    [InlineData("NOLOWERCASE123!", false, "lowercase")]
    [InlineData("NoNumericDigit!", false, "numeric")]
    [InlineData("NoSpecialChar123", false, "special")]
    [InlineData("ValidP@ssw0rd!", true, null)]
    public void ValidatePassword_ChecksComplexityRules(string password, bool expectedValid, string? errorSubstr)
    {
        // Act
        var (isValid, errorMessage) = _hasher.ValidatePassword(password);

        // Assert
        isValid.Should().Be(expectedValid);
        if (!expectedValid && errorSubstr != null)
        {
            errorMessage.Should().Contain(errorSubstr);
        }
    }
}
