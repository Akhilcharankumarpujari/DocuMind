using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Configuration;
using DocuMind.Infrastructure.Security;
using DocuMind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly JwtOptions _options;

    public JwtTokenServiceTests()
    {
        _options = new JwtOptions
        {
            SecretKey = "SuperSecretTestKeyThatHasAtLeast32BytesOfEntropy!",
            Issuer = "DocuMind.TestIssuer",
            Audience = "DocuMind.TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        var dateTimeProvider = new DateTimeProvider();
        _service = new JwtTokenService(Options.Create(_options), dateTimeProvider, NullLogger<JwtTokenService>.Instance);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ShouldIncludeRequiredClaims()
    {
        // Arrange
        var user = new User("Jane Doe", "jane@example.com", "hash123");

        // Act
        var token = _service.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(_options.Issuer);
        jwt.Audiences.Should().Contain(_options.Audience);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        subClaim.Should().Be(user.Id.ToString());

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        emailClaim.Should().Be("jane@example.com");

        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
        nameClaim.Should().Be("Jane Doe");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceCryptographicTokenAndHashedEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var (rawToken, entity) = _service.GenerateRefreshToken(userId, "127.0.0.1");

        // Assert
        rawToken.Should().NotBeNullOrWhiteSpace();
        rawToken.Length.Should().BeGreaterThan(40);

        entity.Should().NotBeNull();
        entity.UserId.Should().Be(userId);
        entity.TokenHash.Should().Be(_service.HashToken(rawToken));
        entity.CreatedByIp.Should().Be("127.0.0.1");
        entity.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void HashToken_DeterministicSha256()
    {
        // Arrange
        var token = "my-test-refresh-token";

        // Act
        var hash1 = _service.HashToken(token);
        var hash2 = _service.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64); // 256 bits = 64 hex characters
    }
}
