using MileageClaims.Modules.Claims.Dtos;

namespace MileageClaims.Modules.Claims.Abstractions;

/// <summary>Contrato público del módulo Boletas para el colaborador dueño de la boleta.</summary>
public interface IMileageClaimService
{
    /// <summary>Crea la boleta (Draft) trayendo los datos del colaborador del ERP (RN-17) y declarando el vehículo (RN-4).</summary>
    Task<MileageClaimDto> Create(string employeeNationalId, CreateMileageClaimRequest request, CancellationToken ct = default);

    /// <summary>Solo mientras Draft o Pending (RN-8).</summary>
    Task<MileageClaimDto> UpdateVehicle(Guid claimId, string requestingEmployeeNationalId, UpdateVehicleRequest request, CancellationToken ct = default);

    /// <summary>Valida ventana de tiempo (RN-6) y duplicados (RN-7); calcula tarifa y distancia de inmediato.</summary>
    Task<MileageClaimDto> AddTrip(Guid claimId, string requestingEmployeeNationalId, AddTripRequest request, CancellationToken ct = default);

    Task<MileageClaimDto> RemoveTrip(Guid claimId, string requestingEmployeeNationalId, Guid tripId, CancellationToken ct = default);

    /// <summary>Pasa de Draft/Rejected a Pending. Falla si algún viaje tiene distancia incompleta (RN-5), reintentando el cálculo primero.</summary>
    Task<MileageClaimDto> Submit(Guid claimId, string requestingEmployeeNationalId, CancellationToken ct = default);

    /// <summary>Retira la boleta (RN-8) — solo mientras Draft o Pending.</summary>
    Task Withdraw(Guid claimId, string requestingEmployeeNationalId, CancellationToken ct = default);

    Task<MileageClaimDto> Get(Guid claimId, CancellationToken ct = default);

    Task<IReadOnlyList<MileageClaimDto>> GetForEmployee(string employeeNationalId, CancellationToken ct = default);

    Task<IReadOnlyList<MileageClaimDto>> GetForApprover(string approverEmail, CancellationToken ct = default);

    /// <summary>Boletas que esa jefatura ya aprobó (para su propio historial, no la cola de pendientes).</summary>
    Task<IReadOnlyList<MileageClaimDto>> GetApprovedByApprover(string approverEmail, CancellationToken ct = default);

    /// <summary>RF-10: lo que consume Finanzas, en tiempo real.</summary>
    Task<IReadOnlyList<MileageClaimDto>> GetApproved(CancellationToken ct = default);
}
