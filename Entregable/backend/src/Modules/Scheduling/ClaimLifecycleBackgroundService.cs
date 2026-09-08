using MileageClaims.Infrastructure;
using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MileageClaims.Modules.Scheduling;

/// <summary>
/// Único disparador periódico del sistema (Temporizador). No contiene ninguna regla de
/// negocio — "qué es vencido" vive en Aprobación y Boletas; acá solo se dispara el ciclo.
/// Si el proceso se reinicia, vuelve a evaluar contra la base de datos: no pierde ni
/// duplica nada (RNF-2).
/// </summary>
public sealed class ClaimLifecycleBackgroundService : BackgroundService
{
    private const string TimerIntervalKey = "TimerIntervalMinutes";
    private const int DefaultTimerIntervalMinutes = 60;
    private const int RejectedDiscardRetentionDays = 7;
    private const int ApprovedPurgeRetentionDays = 7;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClaimLifecycleBackgroundService> _logger;

    public ClaimLifecycleBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ClaimLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnce(stoppingToken);

            var interval = await GetInterval(stoppingToken);
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Apagado normal del servicio.
            }
        }
    }

    /// <summary>Un ciclo completo, expuesto aparte para poder forzarlo desde pruebas.</summary>
    public async Task RunOnce(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var approvals = scope.ServiceProvider.GetRequiredService<IApprovalService>();
        var claims = scope.ServiceProvider.GetRequiredService<IMileageClaimStatusUpdater>();

        try
        {
            var reminders = await approvals.ProcessOverdueReminders(ct);
            var discards = await claims.ProcessOverdueDiscards(RejectedDiscardRetentionDays, ct);
            var purged = await claims.ProcessRetentionPurge(ApprovedPurgeRetentionDays, ct);

            _logger.LogInformation(
                "Temporizador: {Reminders} recordatorios, {Discards} descartes, {Purged} boletas purgadas.",
                reminders, discards, purged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló un ciclo del Temporizador.");
        }
    }

    private async Task<TimeSpan> GetInterval(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<ISystemConfigurationStore>();
        var minutes = await config.GetInt(TimerIntervalKey, DefaultTimerIntervalMinutes, ct);
        return TimeSpan.FromMinutes(minutes);
    }
}
