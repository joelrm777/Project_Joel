namespace MileageClaims.Modules.Claims.Abstractions;

public sealed record PendingReminderCandidate(
    Guid ClaimId,
    string ApproverEmail,
    DateTimeOffset SubmittedAt,
    int RemindersSent,
    DateTimeOffset? LastReminderAt);

/// <summary>
/// Contrato que Boletas expone a Aprobación y al Temporizador para cambiar el estado de una
/// boleta y consultar vencidos — ninguno de los dos conoce el monto ni los viajes.
/// </summary>
public interface IMileageClaimStatusUpdater
{
    Task MarkApproved(Guid claimId, CancellationToken ct = default);
    Task MarkRejected(Guid claimId, string reason, CancellationToken ct = default);

    Task<IReadOnlyList<PendingReminderCandidate>> FindPendingForReminderCheck(CancellationToken ct = default);
    Task RecordReminderSent(Guid claimId, CancellationToken ct = default);

    /// <summary>RN-13: descarta automáticamente rechazadas sin corregir hace más de retentionDays. Devuelve cuántas descartó.</summary>
    Task<int> ProcessOverdueDiscards(int retentionDays, CancellationToken ct = default);

    /// <summary>REG-2: purga el detalle de aprobadas hace más de retentionDays, entregando el resumen a Auditoría antes. Devuelve cuántas purgó.</summary>
    Task<int> ProcessRetentionPurge(int retentionDays, CancellationToken ct = default);
}
