using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using MileageClaims.Infrastructure;
using MileageClaims.Tests.Infrastructure;

namespace MileageClaims.Tests.Api;

/// <summary>
/// Levanta la Api real (Program.cs, controladores, [Authorize] incluidos) contra SQLite en
/// memoria en vez de Azure SQL. Corre bajo el ambiente "Testing" — distinto de Development —
/// para que el auto-migrate/seed de Program.cs (pensado para SQL Server) no se dispare; este
/// fixture siembra lo que cada prueba necesita, igual que TestHost.
///
/// Cada request HTTP resuelve su propio AppDbContext con su propia conexión SQLite (no una
/// compartida): Microsoft.Data.Sqlite no es thread-safe si dos DbContext inicializan la misma
/// instancia de SqliteConnection al mismo tiempo. Todas las conexiones apuntan a la misma
/// base en memoria vía "cache=shared"; una conexión de referencia la mantiene viva.
/// </summary>
public sealed class ApiFactoryFixture : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=file:apitests_{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    /// <summary>Reloj fijo — sin esto, las pruebas de ventana de tiempo dependerían de la fecha real (regla "sin reloj").</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

    public ApiFactoryFixture()
    {
        // Program.cs exige estas dos claves no vacías antes de construir el host — se
        // proveen acá para que arranque; el DbContext real se reemplaza más abajo igual.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Data Source=ignored");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "clave-de-prueba-de-al-menos-32-caracteres-000000");

        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            // AddDbContext acumula configuraciones vía IDbContextOptionsConfiguration<T> en
            // vez de reemplazarlas — sin quitar esto, la de Program.cs (SqlServer) sigue
            // aplicándose junto a la de acá (Sqlite) y EF se queja de dos proveedores.
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o
                .UseSqlite(_connectionString)
                .ReplaceService<IModelCustomizer, SqliteDateTimeOffsetModelCustomizer>());

            // Pisa el TimeProvider.System que registra AddClaimsModule().
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    /// <summary>Crea el esquema (una sola vez) y devuelve un scope para sembrar datos de la prueba.</summary>
    public IServiceScope CreateSeedScope()
    {
        var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return scope;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}
