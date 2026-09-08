using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Modules.Claims;

public static class DependencyInjection
{
    public static IServiceCollection AddClaimsModule(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<MileageClaimService>();
        services.AddScoped<IMileageClaimService>(sp => sp.GetRequiredService<MileageClaimService>());
        services.AddScoped<IMileageClaimStatusUpdater>(sp => sp.GetRequiredService<MileageClaimService>());
        return services;
    }
}
