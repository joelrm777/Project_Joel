using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Approvals.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Modules.Approvals;

public static class DependencyInjection
{
    public static IServiceCollection AddApprovalsModule(this IServiceCollection services)
    {
        services.AddScoped<IApprovalService, ApprovalService>();
        return services;
    }
}
