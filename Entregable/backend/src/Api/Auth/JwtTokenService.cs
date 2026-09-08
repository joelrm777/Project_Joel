using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MileageClaims.Modules.Integrations.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MileageClaims.Api.Auth;

public static class ClaimTypesCustom
{
    public const string NationalId = "national_id";
}

public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(AuthResult auth)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, auth.UserId),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role.ToString())
        };
        if (!string.IsNullOrEmpty(auth.LinkedEmployeeNationalId))
        {
            claims.Add(new Claim(ClaimTypesCustom.NationalId, auth.LinkedEmployeeNationalId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
