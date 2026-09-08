using MileageClaims.Modules.Distances.Abstractions;
using MileageClaims.Modules.Distances.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Modules.Distances;

public static class DependencyInjection
{
    public static IServiceCollection AddDistancesModule(this IServiceCollection services)
    {
        services.AddScoped<DistanceCalculator>();
        services.AddScoped<IDistanceCalculator>(sp => sp.GetRequiredService<DistanceCalculator>());
        services.AddScoped<IStoreAdmin>(sp => sp.GetRequiredService<DistanceCalculator>());
        services.AddScoped<IStoreDistanceAdmin>(sp => sp.GetRequiredService<DistanceCalculator>());
        return services;
    }
}
