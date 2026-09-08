namespace MileageClaims.Modules.Rates.Domain;

/// <summary>Una fila de la tabla vigente de tarifas (RN-1). La mantiene el administrador.</summary>
public sealed class RateTableEntry
{
    public Guid Id { get; set; }
    public VehicleType VehicleType { get; set; }
    public FuelType FuelType { get; set; }
    public int EngineDisplacementMin { get; set; }
    public int EngineDisplacementMax { get; set; }

    /// <summary>Antigüedad exacta que cubre esta fila (RN-2).</summary>
    public int VehicleAgeYears { get; set; }

    public decimal RatePerKm { get; set; }
}
