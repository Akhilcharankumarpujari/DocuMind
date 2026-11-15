namespace DocuMind.Infrastructure.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret signing key for HMAC-SHA256. Loaded via JWT_SECRET or Jwt:SecretKey.
    /// Must be at least 32 characters (256 bits).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "DocuMind.WebApi";

    public string Audience { get; set; } = "DocuMind.Client";

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
