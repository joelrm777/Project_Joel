namespace MileageClaims.Modules.Distances.Domain;

/// <summary>Una de las 40 tiendas de la compañía.</summary>
public sealed class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
