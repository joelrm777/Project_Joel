namespace MileageClaims.Modules.Integrations.Domain;

/// <summary>
/// Cuenta del AD corporativo simulado (SSO). El correo institucional es el mismo que
/// entrega el ERP para un colaborador; jefatura/administrador/finanzas también tienen su
/// cuenta acá aunque no sean necesariamente "colaboradores" con boleta propia.
/// </summary>
public sealed class DirectoryAccount
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    public UserRole Role { get; set; }
    public string? LinkedEmployeeNationalId { get; set; }
}
