using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra el AppDbContext compartido. connectionString viene de configuración
    /// (User Secrets / variables de entorno) — nunca hardcodeado acá.
    /// </summary>
    public static IServiceCollection AddMileageClaimsDatabase(
        this IServiceCollection services,
        string connectionString,
        IReadOnlyList<Assembly> moduleAssemblies)
    {
        services.AddSingleton(new ModuleAssemblyRegistry(moduleAssemblies));
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        return services;
    }
}
