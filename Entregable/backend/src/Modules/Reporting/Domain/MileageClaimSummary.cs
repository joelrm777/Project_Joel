namespace MileageClaims.Modules.Reporting.Domain;

/// <summary>
/// Lo que Boletas entrega antes de purgar el detalle a los 7 días (REG-2). Persiste en
/// ventana móvil de 12 meses — permite auditar boleta por boleta sin el detalle de tramos.
/// </summary>
public sealed class MileageClaimSummary
{
    public Guid MileageClaimId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNationalId { get; set; } = string.Empty;
    public string ApproverNationalId { get; set; } = string.Empty;
    public string ApproverEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalDistance { get; set; }
    public string AppliedRateSummary { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
}
