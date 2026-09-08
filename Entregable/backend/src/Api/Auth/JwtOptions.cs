namespace MileageClaims.Api.Auth;

/// <summary>Se llena desde configuración (User Secrets / variables de entorno) — nunca hardcodeada.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
}
