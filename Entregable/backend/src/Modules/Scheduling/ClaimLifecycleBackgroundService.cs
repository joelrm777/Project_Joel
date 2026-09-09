using MileageClaims.Infrastructure;
using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MileageClaims.Modules.Scheduling;

public sealed record TimerCycleResult(int RemindersSent, int ClaimsDiscarded, int ClaimsPurged);

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
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                // El ciclo periódico no debe morir por una falla puntual (RNF-2) — se
                // reintenta en el próximo tick. Un disparo manual (RunOnce vía endpoint),
                // en cambio, sí deja que la excepción suba para que quien lo pidió se entere.
                _logger.LogError(ex, "Falló un ciclo del Temporizador.");
            }

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

    /// <summary>Un ciclo completo, expuesto aparte para poder forzarlo desde pruebas o un endpoint de administrador.</summary>
    public async Task<TimerCycleResult> RunOnce(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var approvals = scope.ServiceProvider.GetRequiredService<IApprovalService>();
        var claims = scope.ServiceProvider.GetRequiredService<IMileageClaimStatusUpdater>();

        var reminders = await approvals.ProcessOverdueReminders(ct);
        var discards = await claims.ProcessOverdueDiscards(RejectedDiscardRetentionDays, ct);
        var purged = await claims.ProcessRetentionPurge(ApprovedPurgeRetentionDays, ct);

        _logger.LogInformation(
            "Temporizador: {Reminders} recordatorios, {Discards} descartes, {Purged} boletas purgadas.",
            reminders, discards, purged);

        return new TimerCycleResult(reminders, discards, purged);
    }

    private async Task<TimeSpan> GetInterval(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<ISystemConfigurationStore>();
        var minutes = await config.GetInt(TimerIntervalKey, DefaultTimerIntervalMinutes, ct);
        return TimeSpan.FromMinutes(minutes);
    }
}
