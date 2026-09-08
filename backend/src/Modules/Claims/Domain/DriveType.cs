namespace MileageClaims.Modules.Claims.Domain;

/// <summary>Tracción del vehículo. Informativa: nunca participa del cálculo de costo (RN-3).</summary>
public enum DriveType
{
    Single,
    DoubleTraction,
    NotApplicable
}
