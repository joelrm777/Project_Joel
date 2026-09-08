using MileageClaims.Modules.Distances.Domain;

namespace MileageClaims.Modules.Distances.Abstractions;

/// <summary>Contrato público del módulo Distancias. Boletas depende de esto, no de la implementación.</summary>
public interface IDistanceCalculator
{
    /// <summary>
    /// Dada la secuencia de tiendas de un viaje (en orden), suma la distancia de cada
    /// tramo consecutivo. Si falta algún tramo, devuelve IsComplete=false con el tramo
    /// faltante en vez de lanzar una excepción — es un resultado esperado, no un error.
    /// </summary>
    Task<DistanceResult> CalculateTotalDistance(IReadOnlyList<int> storeIdsInOrder, CancellationToken ct = default);
}
