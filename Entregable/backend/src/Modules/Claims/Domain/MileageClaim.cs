using MileageClaims.Modules.Rates.Domain;

namespace MileageClaims.Modules.Claims.Domain;

/// <summary>
/// La boleta. Dueña de su ciclo de vida y de sus viajes (Trip). Copia del ERP los datos del
/// colaborador y de la jefatura al crearla — es la única copia que existe de esos datos
/// mientras la boleta vive (REG-1).
/// </summary>
public sealed class MileageClaim
{
    public Guid Id { get; set; }

    public string EmployeeNationalId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;

    public string ApproverNationalId { get; set; } = string.Empty;
    public string ApproverEmail { get; set; } = string.Empty;

    // Vehículo declarado (RN-4): uno solo por boleta.
    public VehicleType VehicleType { get; set; }
    public FuelType FuelType { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public DriveType DriveType { get; set; }
    public int ModelYear { get; set; }
    public int EngineDisplacement { get; set; }

    public MileageClaimStatus Status { get; set; } = MileageClaimStatus.Draft;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset? FinanceReceivedAt { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Cuántas veces se mandó recordatorio a la jefatura por esta boleta (RN-14).</summary>
    public int RemindersSent { get; set; }
    public DateTimeOffset? LastReminderAt { get; set; }

    public decimal TotalAmount { get; set; }

    public List<Trip> Trips { get; set; } = [];

    public void RecalculateTotal()
    {
        TotalAmount = Trips.Sum(t => t.TotalAmount);
    }
}
