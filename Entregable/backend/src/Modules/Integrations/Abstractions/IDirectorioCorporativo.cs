using MileageClaims.Modules.Integrations.Domain;

namespace MileageClaims.Modules.Integrations.Abstractions;

/// <summary>
/// Única puerta hacia el AD corporativo (correo institucional y SSO). Hoy es un fake
/// sembrado; migrar a AD real es reemplazar la implementación (RF-16).
/// </summary>
public interface IDirectorioCorporativo
{
    /// <summary>Null si el correo no existe o la contraseña no coincide.</summary>
    Task<AuthResult?> ValidarCredenciales(string email, string password, CancellationToken ct = default);

    Task<string?> ObtenerCorreoPorId(string userId, CancellationToken ct = default);

    /// <summary>Correos de todas las cuentas con ese rol (ej. avisar a todo administrador y finanzas — RN-5).</summary>
    Task<IReadOnlyList<string>> ObtenerCorreosPorRol(UserRole role, CancellationToken ct = default);
}
