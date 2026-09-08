namespace MileageClaims.Modules.Notifications.Domain;

/// <summary>
/// Cada correo simulado. El sistema no se conecta a un servidor de correo real (supuesto
/// declarado del proyecto) — esto es lo que se muestra en la demo como "se mandó".
/// </summary>
public sealed class NotificationLog
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public Guid? MileageClaimId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
}
