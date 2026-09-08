namespace MileageClaims.Modules.Approvals.Abstractions;

/// <summary>Contrato público del módulo Aprobación.</summary>
public interface IApprovalService
{
    /// <summary>Todo o nada (RN-9). Solo la jefatura correspondiente puede decidir — se valida por correo.</summary>
    Task Approve(Guid claimId, string approverEmail, CancellationToken ct = default);

    /// <summary>Exige motivo (RN-11).</summary>
    Task Reject(Guid claimId, string approverEmail, string reason, CancellationToken ct = default);

    /// <summary>RN-14: manda recordatorio a las boletas Pending vencidas. Devuelve cuántos recordatorios mandó.</summary>
    Task<int> ProcessOverdueReminders(CancellationToken ct = default);
}

public sealed class ApprovalNotOwnedException(Guid claimId) : Exception($"Esta jefatura no puede decidir sobre la boleta {claimId}.");
public sealed class RejectionReasonRequiredException() : Exception("El rechazo requiere un motivo (RN-11).");
