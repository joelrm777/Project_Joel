using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MileageClaims.Infrastructure;

/// <summary>
/// Solo para las herramientas de diseño de EF Core (`dotnet ef migrations add`). No participa
/// en tiempo de ejecución — ahí el connection string real lo arma Program.cs desde
/// configuración. Acá alcanza cualquier connection string sintácticamente válido: generar la
/// migración no necesita conectarse a la base.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Nombres explícitos: no depender de qué haya cargado el CLR "por casualidad" al momento
    // de escanear (AppDomain.CurrentDomain.GetAssemblies() es no determinista para esto).
    private static readonly string[] ModuleAssemblyNames =
    [
        "MileageClaims.Modules.Rates",
        "MileageClaims.Modules.Distances",
        "MileageClaims.Modules.Integrations",
        "MileageClaims.Modules.Notifications",
        "MileageClaims.Modules.Claims",
        "MileageClaims.Modules.Reporting"
    ];

    public AppDbContext CreateDbContext(string[] args)
    {
        var moduleAssemblies = ModuleAssemblyNames
            .Select(name => System.Reflection.Assembly.Load(name))
            .ToList();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=MileageClaims;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new AppDbContext(options, new ModuleAssemblyRegistry(moduleAssemblies));
    }
}
