namespace MileageClaims.Modules.Integrations.Domain;

/// <summary>Resultado de validar credenciales contra el AD simulado (SSO).</summary>
public sealed record AuthResult(string UserId, string Email, UserRole Role, string? LinkedEmployeeNationalId);
