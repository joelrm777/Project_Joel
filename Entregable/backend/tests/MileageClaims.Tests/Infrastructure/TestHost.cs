using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using MileageClaims.Infrastructure;
using MileageClaims.Modules.Approvals;
using MileageClaims.Modules.Claims;
using MileageClaims.Modules.Distances;
using MileageClaims.Modules.Integrations;
using MileageClaims.Modules.Notifications;
using MileageClaims.Modules.Rates;
using MileageClaims.Modules.Reporting;

namespace MileageClaims.Tests.Infrastructure;

/// <summary>
/// Levanta el mismo grafo de módulos que arma Program.cs (llamando los mismos
/// AddXModule() de producción), pero contra SQLite en memoria y con un reloj
/// controlable — sin red, sin estado compartido entre pruebas. Cada prueba crea el
/// suyo con <see cref="Create"/> y lo descarta al terminar.
/// </summary>
public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public FakeTimeProvider Clock { get; }

    private TestHost(SqliteConnection connection, ServiceProvider provider, IServiceScope scope, FakeTimeProvider clock)
    {
        _connection = connection;
        _provider = provider;
        _scope = scope;
        Clock = clock;
    }

    public static TestHost Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // Referencia fija: mitad de año, con margen de sobra para RN-6 (ventana de
        // 2 meses hacia atrás) y RN-13/RN-14 (plazos de días) en cualquier dirección.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRatesModule();
        services.AddDistancesModule();
        services.AddIntegrationsModule();
        services.AddNotificationsModule();
        services.AddClaimsModule();
        services.AddApprovalsModule();
        services.AddSystemConfiguration();
        services.AddReportingModule();

        var moduleAssemblies = new[]
        {
            typeof(Modules.Rates.Domain.RateTableEntry).Assembly,
            typeof(Modules.Distances.Domain.Store).Assembly,
            typeof(Modules.Integrations.Domain.DirectoryAccount).Assembly,
            typeof(Modules.Notifications.Domain.NotificationLog).Assembly,
            typeof(Modules.Claims.Domain.MileageClaim).Assembly,
            typeof(Modules.Reporting.Domain.MileageClaimSummary).Assembly,
        };
        // Mismo AppDbContext de producción, pero apuntando a SQLite en memoria en vez de
        // SQL Server — sin llamar a AddMileageClaimsDatabase (ese registra UseSqlServer).
        services.AddSingleton(new ModuleAssemblyRegistry(moduleAssemblies));
        services.AddDbContext<AppDbContext>(o => o
            .UseSqlite(connection)
            // SQLite no ordena por DateTimeOffset (ver SqliteDateTimeOffsetModelCustomizer) —
            // solo afecta a esta prueba, SQL Server no tiene ese problema.
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCustomizer, SqliteDateTimeOffsetModelCustomizer>());
        // Pisa el TimeProvider.System que registra AddClaimsModule().
        services.AddSingleton<TimeProvider>(clock);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return new TestHost(connection, provider, scope, clock);
    }

    public T Get<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    public AppDbContext Db => Get<AppDbContext>();

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }
}
