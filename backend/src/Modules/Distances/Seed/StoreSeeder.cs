using MileageClaims.Infrastructure;
using MileageClaims.Modules.Distances.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Distances.Seed;

/// <summary>
/// Tiendas y distancias sintéticas para probar el recorrido de punta a punta. No son las
/// 40 tiendas reales de la compañía — datos de prueba únicamente.
/// </summary>
public static class StoreSeeder
{
    public static async Task SeedIfEmpty(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Set<Store>().AnyAsync(ct)) return;

        var storeNames = new[] { "Tienda Central", "Tienda Norte", "Tienda Sur", "Tienda Este", "Tienda Oeste" };
        var stores = storeNames.Select(n => new Store { Name = n }).ToList();
        db.Set<Store>().AddRange(stores);
        await db.SaveChangesAsync(ct);

        // Conecta cada tienda con la siguiente en un anillo, para que cualquier secuencia
        // corta de tiendas sembradas tenga distancia registrada.
        var distances = new List<StoreDistance>();
        for (var i = 0; i < stores.Count; i++)
        {
            var next = stores[(i + 1) % stores.Count];
            distances.Add(new StoreDistance
            {
                OriginStoreId = stores[i].Id,
                DestinationStoreId = next.Id,
                DistanceKm = 12m + i * 3m
            });
        }
        db.Set<StoreDistance>().AddRange(distances);
        await db.SaveChangesAsync(ct);
    }
}
