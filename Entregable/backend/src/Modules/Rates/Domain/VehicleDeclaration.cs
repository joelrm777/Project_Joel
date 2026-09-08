namespace MileageClaims.Modules.Rates.Domain;

/// <summary>Vehículo declarado en una boleta (RN-4: uno solo por boleta).</summary>
public sealed record VehicleDeclaration(
    VehicleType VehicleType,
    FuelType FuelType,
    int EngineDisplacement,
    int ModelYear);
