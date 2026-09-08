using MileageClaims.Modules.Rates.Abstractions;
using MileageClaims.Modules.Rates.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Modules.Rates;

public static class DependencyInjection
{
    public static IServiceCollection AddRatesModule(this IServiceCollection services)
    {
        services.AddScoped<RateCalculator>();
        services.AddScoped<IRateCalculator>(sp => sp.GetRequiredService<RateCalculator>());
        services.AddScoped<IRateTableAdmin>(sp => sp.GetRequiredService<RateCalculator>());
        return services;
    }
}
