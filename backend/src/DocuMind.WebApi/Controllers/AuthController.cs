using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using DocuMind.Domain.Exceptions;
using DocuMind.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DocuMind.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth-policy")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthController> _logger;

    public const string RefreshTokenCookieName = "refreshToken";

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account and sets a secure HttpOnly refresh token cookie.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "Registration payload cannot be empty." });

        try
        {
            var ip = GetClientIpAddress();
            var (response, rawRefreshToken) = await _authService.RegisterAsync(request, ip, cancellationToken);

            SetRefreshTokenCookie(rawRefreshToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user with email and password and sets a secure HttpOnly refresh token cookie.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        try
        {
            var ip = GetClientIpAddress();
            var (response, rawRefreshToken) = await _authService.LoginAsync(request, ip, cancellationToken);

            SetRefreshTokenCookie(rawRefreshToken);
            return Ok(response);
        }
        catch (DomainException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refreshes an expired access token using the HttpOnly refresh token cookie with automatic rotation.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RefreshTokenRequest? body,
        CancellationToken cancellationToken)
    {
        // Try reading from HttpOnly cookie first; fall back to request body if client provides it
        var rawToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(rawToken) && body != null)
        {
            rawToken = body.RefreshToken;
        }

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Unauthorized(new { message = "Refresh token is missing. Please sign in again." });
        }

        try
        {
            var ip = GetClientIpAddress();
            var (response, newRawRefreshToken) = await _authService.RefreshTokenAsync(rawToken, ip, cancellationToken);

            SetRefreshTokenCookie(newRawRefreshToken);
            return Ok(response);
        }
        catch (DomainException ex)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Revokes the current session's refresh token and clears the authentication cookie.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RefreshTokenRequest? body,
        CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(rawToken) && body != null)
        {
            rawToken = body.RefreshToken;
        }

        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            var ip = GetClientIpAddress();
            await _authService.RevokeTokenAsync(rawToken, ip, cancellationToken);
        }

        ClearRefreshTokenCookie();
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Retrieves the authenticated user's profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(new { message = "User identity could not be verified." });

        try
        {
            var profile = await _authService.GetCurrentUserProfileAsync(userId.Value, cancellationToken);
            return Ok(profile);
        }
        catch (EntityNotFoundException)
        {
            return Unauthorized(new { message = "User account not found." });
        }
    }

    private void SetRefreshTokenCookie(string rawRefreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            Path = "/api/auth"
        };

        Response.Cookies.Append(RefreshTokenCookieName, rawRefreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth"
        });
    }

    private string? GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var firstIp = forwarded.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(firstIp)) return firstIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
