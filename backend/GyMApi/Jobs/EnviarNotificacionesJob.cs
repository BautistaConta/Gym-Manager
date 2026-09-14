using GymManager.API.Services;

namespace GymManager.API.Jobs;

public class EnviarNotificacionesJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnviarNotificacionesJob> _logger;

    public EnviarNotificacionesJob(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<EnviarNotificacionesJob> logger) { _scopeFactory = scopeFactory; _configuration = configuration; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Con exactamente una réplica, ningún envío anterior sigue en vuelo al arrancar.
        using (var scope = _scopeFactory.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<NotificacionService>();
            var ambiguas = await service.RevisarProcesandoAlIniciarAsync();
            if (ambiguas > 0) _logger.LogWarning("{Cantidad} notificaciones quedaron para revisión manual tras reiniciar.", ambiguas);
        }
        await EjecutarSeguroAsync(stoppingToken);
        var minutos = Math.Max(1, _configuration.GetValue<int?>("Notificaciones:EnvioIntervalMinutes") ?? 5);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutos));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await EjecutarSeguroAsync(stoppingToken);
    }

    private async Task EjecutarSeguroAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Etapa piloto: el IGymContext scoped usa este tenant. En multi-tenant real se reemplaza por iteración de tenants.
            var pilotGymId = _configuration["MultiTenancy:PilotGymId"]
                ?? throw new InvalidOperationException("MultiTenancy:PilotGymId no está configurado.");
            _logger.LogDebug("Ejecutando envío de notificaciones para el gimnasio piloto {GymId}.", pilotGymId);
            using var scope = _scopeFactory.CreateScope();
            var notificaciones = scope.ServiceProvider.GetRequiredService<NotificacionService>();
            var limite = Math.Clamp(_configuration.GetValue<int?>("Notificaciones:EnvioBatchSize") ?? 50, 1, 200);
            await notificaciones.ProcesarPendientesAsync(limite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError(ex, "Falló la ejecución del job de envío de notificaciones."); }
    }
}
