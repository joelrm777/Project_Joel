using MileageClaims.Modules.Reporting.Abstractions;
using MileageClaims.Modules.Reporting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Modules.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<ReportingService>();
        services.AddScoped<IMileageClaimSummaryStore>(sp => sp.GetRequiredService<ReportingService>());
        services.AddScoped<IReportingQueryService>(sp => sp.GetRequiredService<ReportingService>());
        return services;
    }
}
