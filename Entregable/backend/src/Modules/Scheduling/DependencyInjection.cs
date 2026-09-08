using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MileageClaims.Modules.Scheduling;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services)
    {
        services.AddSingleton<ClaimLifecycleBackgroundService>();
        services.AddHostedService(sp => sp.GetRequiredService<ClaimLifecycleBackgroundService>());
        return services;
    }
}
