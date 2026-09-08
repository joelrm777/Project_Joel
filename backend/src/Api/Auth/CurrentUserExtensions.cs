using System.Security.Claims;

namespace MileageClaims.Api.Auth;

public static class CurrentUserExtensions
{
    public static string? GetEmail(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Email);

    public static string? GetNationalId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypesCustom.NationalId);

    public static string? GetRole(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role);
}
