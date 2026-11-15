using DocuMind.Application.Common.Models;
using DocuMind.Domain.Exceptions;
using DocuMind.Infrastructure.Configuration;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Security;
using DocuMind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class SecurityAuthenticationTests
{
    private readonly DocuMindDbContext _dbContext;
    private readonly Argon2idPasswordHasher _hasher;
    private readonly JwtTokenService _jwtService;
    private readonly AuthService _authService;

    public SecurityAuthenticationTests()
    {
        var dbOptions = new DbContextOptionsBuilder<DocuMindDbContext>()
            .UseInMemoryDatabase(databaseName: $"DocuMind_AuthTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new DocuMindDbContext(dbOptions);

        var argonOptions = Options.Create(new Argon2Options
        {
            MemorySize = 1024,
            Iterations = 2,
            DegreeOfParallelism = 1,
            SaltSize = 16,
            HashLength = 32
        });
        _hasher = new Argon2idPasswordHasher(argonOptions, NullLogger<Argon2idPasswordHasher>.Instance);

        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "SuperSecretTestKeyThatHasAtLeast32BytesOfEntropy!",
            Issuer = "DocuMind.TestIssuer",
            Audience = "DocuMind.TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });
        var dateTime = new DateTimeProvider();
        _jwtService = new JwtTokenService(jwtOptions, dateTime, NullLogger<JwtTokenService>.Instance);

        _authService = new AuthService(_dbContext, _hasher, _jwtService, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ShouldCreateUserAndIssueTokens()
    {
        // Arrange
        var request = new RegisterRequest("Alice Smith", "alice@example.com", "SecurePassword123!");

        // Act
        var (response, rawRefreshToken) = await _authService.RegisterAsync(request, "192.168.1.1");

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.User.FullName.Should().Be("Alice Smith");
        response.User.Email.Should().Be("alice@example.com");
        rawRefreshToken.Should().NotBeNullOrWhiteSpace();

        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "alice@example.com");
        userInDb.Should().NotBeNull();
        userInDb!.NormalizedEmail.Should().Be("ALICE@EXAMPLE.COM");
        userInDb.PasswordHash.Should().StartWith("$argon2id$");

        var tokenInDb = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == userInDb.Id);
        tokenInDb.Should().NotBeNull();
        tokenInDb!.TokenHash.Should().Be(_jwtService.HashToken(rawRefreshToken));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowDomainException()
    {
        // Arrange
        var request = new RegisterRequest("Bob Jones", "bob@example.com", "ValidPass123#");
        await _authService.RegisterAsync(request);

        // Act: Try registering with same email in different casing
        var duplicateRequest = new RegisterRequest("Bob Jones", "BOB@example.com", "AnotherPass456!");
        var act = async () => await _authService.RegisterAsync(duplicateRequest);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new RegisterRequest("Charlie", "charlie@example.com", "weak");

        // Act
        var act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least 8 characters*");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldAuthenticateAndIssueNewTokens()
    {
        // Arrange
        var reg = new RegisterRequest("David Miller", "david@example.com", "MyStrongPass1!");
        await _authService.RegisterAsync(reg);

        // Act
        var loginReq = new LoginRequest("david@example.com", "MyStrongPass1!");
        var (authResponse, rawRefreshToken) = await _authService.LoginAsync(loginReq);

        // Assert
        authResponse.Should().NotBeNull();
        authResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResponse.User.Email.Should().Be("david@example.com");
        rawRefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_ShouldThrowDomainException()
    {
        // Arrange
        var reg = new RegisterRequest("Eve Adams", "eve@example.com", "RightPassword123!");
        await _authService.RegisterAsync(reg);

        // Act
        var loginReq = new LoginRequest("eve@example.com", "WrongPassword123!");
        var act = async () => await _authService.LoginAsync(loginReq);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ShouldRotateRefreshTokenAndRevokeOldToken()
    {
        // Arrange
        var reg = new RegisterRequest("Frank White", "frank@example.com", "FrankPass123!");
        var (regResp, initialRefreshToken) = await _authService.RegisterAsync(reg);

        // Act: Refresh
        var (refreshResp, rotatedRefreshToken) = await _authService.RefreshTokenAsync(initialRefreshToken);

        // Assert
        refreshResp.Should().NotBeNull();
        refreshResp.AccessToken.Should().NotBeNullOrWhiteSpace();
        rotatedRefreshToken.Should().NotBeNullOrWhiteSpace();
        rotatedRefreshToken.Should().NotBe(initialRefreshToken);

        // Verify old token is revoked
        var oldHash = _jwtService.HashToken(initialRefreshToken);
        var oldTokenEntity = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == oldHash);
        oldTokenEntity.Should().NotBeNull();
        oldTokenEntity!.RevokedAtUtc.Should().NotBeNull();
        oldTokenEntity.ReplacedByTokenId.Should().NotBeNull();

        // Verify new token is active
        var newHash = _jwtService.HashToken(rotatedRefreshToken);
        var newTokenEntity = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == newHash);
        newTokenEntity.Should().NotBeNull();
        newTokenEntity!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_ReusedRevokedToken_ShouldRevokeAllSessionsDueToReuseDetection()
    {
        // Arrange
        var reg = new RegisterRequest("Grace Hopper", "grace@example.com", "GracePass123!");
        var (_, initialRefreshToken) = await _authService.RegisterAsync(reg);

        // First rotation (valid)
        var (_, rotatedToken) = await _authService.RefreshTokenAsync(initialRefreshToken);

        // Act: Attempt to reuse the INITIAL (already revoked) token!
        var reuseAct = async () => await _authService.RefreshTokenAsync(initialRefreshToken);

        // Assert: Throws domain exception alerting reuse
        await reuseAct.Should().ThrowAsync<DomainException>()
            .WithMessage("*compromised refresh token*");

        // Verify that the active rotated token was ALSO immediately invalidated as a protective measure
        var rotatedHash = _jwtService.HashToken(rotatedToken);
        var rotatedEntity = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == rotatedHash);
        rotatedEntity.Should().NotBeNull();
        rotatedEntity!.RevokedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeTokenAsync_Logout_ShouldRevokeToken()
    {
        // Arrange
        var reg = new RegisterRequest("Henry Ford", "henry@example.com", "HenryPass123!");
        var (_, rawRefreshToken) = await _authService.RegisterAsync(reg);

        // Act: Logout / Revoke
        await _authService.RevokeTokenAsync(rawRefreshToken);

        // Assert
        var tokenHash = _jwtService.HashToken(rawRefreshToken);
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        token.Should().NotBeNull();
        token!.RevokedAtUtc.Should().NotBeNull();
        token.IsActive.Should().BeFalse();
    }
}
