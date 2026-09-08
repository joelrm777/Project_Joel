namespace MileageClaims.Modules.Distances.Domain;

/// <summary>
/// Distancia registrada entre dos tiendas (RN-5). Se guarda una sola vez por par; el
/// cálculo de un viaje busca la fila sin importar en qué orden se declararon origen/destino.
/// </summary>
public sealed class StoreDistance
{
    public int Id { get; set; }
    public int OriginStoreId { get; set; }
    public int DestinationStoreId { get; set; }
    public decimal DistanceKm { get; set; }
}
