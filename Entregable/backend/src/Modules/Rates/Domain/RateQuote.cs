namespace MileageClaims.Modules.Rates.Domain;

/// <summary>
/// Resultado congelado del cálculo de tarifa (RN-16): el monto se guarda en el viaje tal
/// cual, sin referencia viva a la fila de RateTable que lo originó.
/// </summary>
public sealed record RateQuote(decimal RatePerKm, string RateSummary);
