using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Infrastructure;

/// <summary>
/// DbContext único y compartido por todos los módulos. Cada módulo define sus propias
/// entidades y su propio IEntityTypeConfiguration&lt;T&gt;; este contexto no conoce ningún
/// tipo de entidad concreto, solo escanea los ensamblados que se le indiquen al arrancar
/// la aplicación (ver ModuleAssemblies).
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly IReadOnlyList<Assembly> _moduleAssemblies;

    public AppDbContext(DbContextOptions<AppDbContext> options, ModuleAssemblyRegistry registry)
        : base(options)
    {
        _moduleAssemblies = registry.Assemblies;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // El propio ensamblado de Infrastructure siempre se escanea (ahí vive
        // SystemConfigurationEntry, que no pertenece a ningún módulo de negocio).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var assembly in _moduleAssemblies.Distinct())
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}

/// <summary>
/// Lista de ensamblados de módulos que tienen configuraciones de entidades para registrar.
/// La arma el proyecto Api al arrancar, porque es el único que conoce a todos los módulos.
/// </summary>
public sealed class ModuleAssemblyRegistry
{
    public ModuleAssemblyRegistry(IReadOnlyList<Assembly> assemblies)
    {
        Assemblies = assemblies;
    }

    public IReadOnlyList<Assembly> Assemblies { get; }
}
