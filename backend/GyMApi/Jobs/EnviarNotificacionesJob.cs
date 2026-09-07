using GymManager.API.Models;
using GymManager.API.Senders;
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
        await EjecutarSeguroAsync(stoppingToken);
        var minutos = Math.Max(1, _configuration.GetValue<int?>("Notificaciones:EnvioIntervalMinutes") ?? 5);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutos));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await EjecutarSeguroAsync(stoppingToken);
    }

    private async Task EjecutarSeguroAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notificaciones = scope.ServiceProvider.GetRequiredService<NotificacionService>();
            var sender = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();
            var limite = Math.Clamp(_configuration.GetValue<int?>("Notificaciones:EnvioBatchSize") ?? 50, 1, 200);
            var pendientes = (await notificaciones.GetAllAsync(EstadoNotificacionWhatsApp.Pendiente, null)).Take(limite);

            foreach (var notificacion in pendientes)
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    var resultado = await sender.SendAsync(notificacion.Telefono, notificacion.Mensaje, notificacion.Tipo, stoppingToken);
                    if (resultado.Exitoso) await notificaciones.MarcarEnviadaAsync(notificacion);
                    else await notificaciones.MarcarFallidaAsync(notificacion, resultado.ErrorDetalle ?? "Twilio no informó el motivo del fallo.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo enviar la notificación {NotificacionId}", notificacion.Id);
                    try { await notificaciones.MarcarFallidaAsync(notificacion, "Error inesperado al enviar la notificación."); }
                    catch (Exception updateEx) { _logger.LogError(updateEx, "No se pudo actualizar el estado de la notificación {NotificacionId}", notificacion.Id); }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError(ex, "Falló la ejecución del job de envío de notificaciones."); }
    }
}
