using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace MileageClaims.Infrastructure;

/// <summary>
/// Configuración clave/valor mantenida por el administrador (ej. ReminderIntervalBusinessDays,
/// TimerIntervalMinutes). Vive en Infrastructure porque es transversal — no es una regla de
/// negocio de un módulo en particular, solo un valor que varios módulos necesitan leer.
/// </summary>
public sealed class SystemConfigurationEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class SystemConfigurationEntryConfiguration : IEntityTypeConfiguration<SystemConfigurationEntry>
{
    public void Configure(EntityTypeBuilder<SystemConfigurationEntry> builder)
    {
        builder.ToTable("SystemConfiguration");
        builder.HasKey(x => x.Key);
    }
}

public interface ISystemConfigurationStore
{
    Task<string?> Get(string key, CancellationToken ct = default);
    Task<int> GetInt(string key, int defaultValue, CancellationToken ct = default);
    Task Set(string key, string value, CancellationToken ct = default);
}

public sealed class SystemConfigurationStore : ISystemConfigurationStore
{
    private readonly AppDbContext _db;

    public SystemConfigurationStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> Get(string key, CancellationToken ct = default)
    {
        var entry = await _db.Set<SystemConfigurationEntry>().FindAsync([key], ct);
        return entry?.Value;
    }

    public async Task<int> GetInt(string key, int defaultValue, CancellationToken ct = default)
    {
        var value = await Get(key, ct);
        return value is not null && int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    public async Task Set(string key, string value, CancellationToken ct = default)
    {
        var entry = await _db.Set<SystemConfigurationEntry>().FindAsync([key], ct);
        if (entry is null)
        {
            _db.Set<SystemConfigurationEntry>().Add(new SystemConfigurationEntry { Key = key, Value = value });
        }
        else
        {
            entry.Value = value;
        }
        await _db.SaveChangesAsync(ct);
    }
}

public static class SystemConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddSystemConfiguration(this IServiceCollection services)
    {
        services.AddScoped<ISystemConfigurationStore, SystemConfigurationStore>();
        return services;
    }
}
