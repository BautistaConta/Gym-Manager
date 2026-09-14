using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.Services;

namespace GymManager.API.Jobs;

public class VencimientosNotificacionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VencimientosNotificacionJob> _logger;

    public VencimientosNotificacionJob(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<VencimientosNotificacionJob> logger) { _scopeFactory = scopeFactory; _configuration = configuration; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EjecutarSeguroAsync(stoppingToken);
        var horas = Math.Max(1, _configuration.GetValue<int?>("Notificaciones:VencimientosIntervalHours") ?? 24);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(horas));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await EjecutarSeguroAsync(stoppingToken);
    }

    private async Task EjecutarSeguroAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Etapa piloto: el IGymContext scoped usa este tenant. En multi-tenant real se reemplaza por iteración de tenants.
            var pilotGymId = _configuration["MultiTenancy:PilotGymId"]
                ?? throw new InvalidOperationException("MultiTenancy:PilotGymId no está configurado.");
            _logger.LogDebug("Evaluando vencimientos para el gimnasio piloto {GymId}.", pilotGymId);
            using var scope = _scopeFactory.CreateScope();
            var alumnos = scope.ServiceProvider.GetRequiredService<AlumnoRepository>();
            var pagos = scope.ServiceProvider.GetRequiredService<PagoRepository>();
            var notificaciones = scope.ServiceProvider.GetRequiredService<NotificacionService>();
            var cuotas = scope.ServiceProvider.GetRequiredService<CuotaCalculator>();

            var elegibles = await alumnos.GetAllAsync();
            var ultimos = await pagos.GetUltimosPorAlumnoAsync(elegibles.Select(a => a.Id));
            foreach (var alumno in elegibles)
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    if (!ultimos.TryGetValue(alumno.Id, out var ultimoPago)) continue;
                    var cuota = cuotas.Evaluar(ultimoPago.PeriodoHasta);
                    if (cuota.Estado == EstadoCuota.PROXIMO_A_VENCER)
                        await notificaciones.EncolarSiCorrespondeAsync(alumno, ultimoPago, TipoNotificacionWhatsApp.PorVencer);
                    else if (cuota.Estado == EstadoCuota.VENCIDA)
                        await notificaciones.EncolarSiCorrespondeAsync(alumno, ultimoPago, TipoNotificacionWhatsApp.Vencido);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex) { _logger.LogError(ex, "No se pudieron evaluar los vencimientos del alumno {AlumnoId}", alumno.Id); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError(ex, "Falló la ejecución del job de vencimientos."); }
    }
}
