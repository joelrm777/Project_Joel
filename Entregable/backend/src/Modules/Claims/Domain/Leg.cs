namespace MileageClaims.Modules.Claims.Domain;

/// <summary>Un tramo entre dos tiendas consecutivas dentro de un viaje.</summary>
public sealed class Leg
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public int SequenceNumber { get; set; }
    public int OriginStoreId { get; set; }
    public int DestinationStoreId { get; set; }

    /// <summary>Null si este tramo específico no tiene distancia registrada (RN-5).</summary>
    public decimal? AppliedDistanceKm { get; set; }
}
