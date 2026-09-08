namespace MileageClaims.Modules.Claims.Domain;

public enum MileageClaimStatus
{
    /// <summary>El colaborador todavía la está armando; no se envió a la jefatura.</summary>
    Draft,
    Pending,
    Approved,
    Rejected,
    Discarded
}
