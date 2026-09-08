using MileageClaims.Modules.Integrations.Abstractions;
using MileageClaims.Modules.Integrations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Modules.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection AddIntegrationsModule(this IServiceCollection services)
    {
        services.AddScoped<IErpRH, FakeErpRH>();
        services.AddScoped<IDirectorioCorporativo, FakeDirectorioCorporativo>();
        return services;
    }
}
