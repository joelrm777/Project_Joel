using MileageClaims.Modules.Rates.Domain;

namespace MileageClaims.Modules.Rates.Abstractions;

/// <summary>Contrato público del módulo Tarifas. Boletas depende de esto, no de la implementación.</summary>
public interface IRateCalculator
{
    /// <summary>
    /// Calcula el costo por km vigente para un vehículo, cruzando tipo, combustible,
    /// cilindraje y antigüedad (RN-1, RN-2, RN-3) contra la tabla vigente.
    /// </summary>
    Task<RateQuote> CalculateRate(VehicleDeclaration vehicle, DateOnly asOfDate, CancellationToken ct = default);
}
