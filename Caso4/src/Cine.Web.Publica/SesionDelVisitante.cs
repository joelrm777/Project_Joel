namespace Cine.Web.Publica;

/// <summary>
/// Identifica al visitante con un token opaco en una cookie de sesión, sin cuenta ni registro:
/// hace falta saber de quién es cada apartado (RN-19) sin crear cuentas de comprador, que están
/// fuera de alcance (DISENO.md, componente 2 y otras decisiones).
/// </summary>
public static class SesionDelVisitante
{
    public const string NombreCookie = "cine_sesion";

    public static string Token(HttpContext contexto)
    {
        if (contexto.Request.Cookies.TryGetValue(NombreCookie, out var token) && !string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        var nuevo = Guid.NewGuid().ToString("N");

        contexto.Response.Cookies.Append(NombreCookie, nuevo, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });

        return nuevo;
    }
}
