namespace MileageClaims.Modules.Integrations.Domain;

/// <summary>Fila del ERP de RH simulado. Datos sintéticos, nunca información real de la compañía.</summary>
public sealed class FakeEmployeeRecord
{
    public string NationalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ApproverNationalId { get; set; } = string.Empty;
    public string ApproverEmail { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
