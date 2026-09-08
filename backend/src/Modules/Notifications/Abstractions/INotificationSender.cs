using MileageClaims.Modules.Notifications.Domain;

namespace MileageClaims.Modules.Notifications.Abstractions;

/// <summary>
/// Contrato público del módulo Notificaciones. No decide cuándo se dispara un correo —
/// solo formatea y "envía" (simulado) lo que otro módulo le pide.
/// </summary>
public interface INotificationSender
{
    Task Send(NotificationType type, string recipientEmail, Guid? mileageClaimId, string details, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationLog>> GetLog(CancellationToken ct = default);
}
