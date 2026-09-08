using MileageClaims.Modules.Notifications.Abstractions;
using MileageClaims.Modules.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Modules.Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationSender, NotificationSender>();
        return services;
    }
}
