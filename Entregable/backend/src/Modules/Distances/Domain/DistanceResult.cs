namespace MileageClaims.Modules.Distances.Domain;

/// <summary>Un tramo del viaje. DistanceKm es null si ese tramo específico no está registrado (RN-5).</summary>
public sealed record LegAttempt(int SequenceNumber, int OriginStoreId, int DestinationStoreId, decimal? DistanceKm);

/// <summary>
/// Resultado de calcular la distancia total de un viaje. Intenta resolver todos los tramos,
/// no se detiene en el primero que falte — así Boletas puede guardar el viaje completo y
/// mostrar exactamente cuáles tramos hacen falta cargar.
/// </summary>
public sealed record DistanceResult(bool IsComplete, decimal TotalDistanceKm, IReadOnlyList<LegAttempt> Legs)
{
    public IEnumerable<LegAttempt> MissingLegs => Legs.Where(l => l.DistanceKm is null);
}
