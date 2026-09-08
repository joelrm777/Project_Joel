namespace MileageClaims.Modules.Claims.Domain;

/// <summary>Un viaje puntual dentro de una boleta (RN-6, RN-7).</summary>
public sealed class Trip
{
    public Guid Id { get; set; }
    public Guid MileageClaimId { get; set; }
    public MileageClaim? MileageClaim { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>Congelada al agregar el viaje (RN-1, RN-16) — no se vuelve a recalcular.</summary>
    public decimal AppliedRatePerKm { get; set; }
    public string RateSummary { get; set; } = string.Empty;

    public decimal TotalDistance { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsDistanceComplete { get; set; }

    public List<Leg> Legs { get; set; } = [];

    public void Recalculate()
    {
        IsDistanceComplete = Legs.Count > 0 && Legs.All(l => l.AppliedDistanceKm.HasValue);
        TotalDistance = Legs.Sum(l => l.AppliedDistanceKm ?? 0m);
        TotalAmount = IsDistanceComplete ? Math.Round(AppliedRatePerKm * TotalDistance, 2) : 0m;
    }
}
