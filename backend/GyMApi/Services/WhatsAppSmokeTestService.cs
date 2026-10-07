using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Repositories;
using GymManager.API.Senders;
using GymManager.API.Tenancy;
using Microsoft.Extensions.Options;

namespace GymManager.API.Services;

public sealed class WhatsAppSmokeTestService(INotificacionRepository repository, IWhatsAppSender sender,
    IOptions<TwilioOptions> options, IGymContext gym, TimeProvider clock, IPilotEventRecorder events)
{
    public List<string> IntervencionesPendientes()
    {
        var o = options.Value;
        var missing = new List<string>();
        if (!o.Enabled) missing.Add("Habilitar explícitamente Twilio:Enabled con entorno/secret storage.");
        if (!o.SmokeTestEnabled) missing.Add("Habilitar Twilio:SmokeTestEnabled después de autorizar cargos.");
        if (o.WorkerEnabled) missing.Add("Apagar Twilio:WorkerEnabled durante la prueba controlada.");
        if (!o.PaidAccountConfirmed) missing.Add("Activar cuenta paga manualmente y confirmar Twilio:PaidAccountConfirmed.");
        if (!o.TemplatesApprovedConfirmed) missing.Add("Aprobar la plantilla PorVencer en WhatsApp y confirmar Twilio:TemplatesApprovedConfirmed.");
        if (string.IsNullOrWhiteSpace(o.AccountSid) || string.IsNullOrWhiteSpace(o.AuthToken)) missing.Add("Cargar Twilio:AccountSid/AuthToken mediante entorno o user-secrets; no JSON.");
        if (!NotificacionService.PhoneIsValid(o.WhatsAppFromNumber)) missing.Add("Configurar remitente WhatsApp aprobado en Twilio:WhatsAppFromNumber (E.164).");
        if (!System.Text.RegularExpressions.Regex.IsMatch(o.PorVencerContentSid, @"^HX[0-9a-fA-F]{32}$"))
            missing.Add("Configurar Twilio:PorVencerContentSid con un HX válido y aprobado.");
        if (!NotificacionService.PhoneIsValid(o.AuthorizedTestNumber)) missing.Add("Configurar Twilio:AuthorizedTestNumber autorizado con consentimiento (E.164).");
        return missing;
    }

    public async Task<NotificacionWhatsApp> EnviarAsync(TipoNotificacionWhatsApp tipo, bool confirmacion, CancellationToken cancellationToken)
    {
        if (!confirmacion) throw new DomainException("Se requiere confirmación explícita de cargos.");
        if (tipo != TipoNotificacionWhatsApp.PorVencer) throw new DomainException("El piloto solo permite el mensaje PorVencer.");
        var pending = IntervencionesPendientes();
        if (pending.Count > 0) throw new DomainException(string.Join(" ", pending));
        var now = clock.GetUtcNow().UtcDateTime;
        var n = await repository.CreateIfAbsentAsync(new NotificacionWhatsApp
        {
            GymId = gym.GymId, AlumnoId = "smoke-test", Tipo = tipo, EsPrueba = true,
            Telefono = options.Value.AuthorizedTestNumber, ClaveDeduplicacion = $"{gym.GymId}:smoke-test:{tipo}",
            ContentSid = options.Value.PorVencerContentSid,
            VariablesPlantilla = new() { ["1"] = "Prueba piloto", ["2"] = "30/09/2026" },
            Mensaje = "Prueba administrativa controlada de plantilla", Estado = EstadoNotificacionWhatsApp.Pendiente,
            FechaCreacion = now, FechaActualizacion = now
        });
        // Una prueba por GymId+Tipo; repetir/concurrencia no vuelve a generar cargos.
        if (!await repository.TransitionAsync(n.Id, EstadoNotificacionWhatsApp.Pendiente, EstadoNotificacionWhatsApp.Procesando, now))
            return (await repository.GetByIdAsync(n.Id))!;
        try
        {
            var result = await sender.SendAsync(n, cancellationToken);
            var state = result.Simulado ? EstadoNotificacionWhatsApp.Simulado :
                result.Exitoso && !string.IsNullOrWhiteSpace(result.ProviderMessageId) ? EstadoNotificacionWhatsApp.AceptadoPorTwilio :
                result.ResultadoDefinitivo ? EstadoNotificacionWhatsApp.Fallido : EstadoNotificacionWhatsApp.RequiereRevision;
            if (!await repository.TransitionAsync(n.Id, EstadoNotificacionWhatsApp.Procesando, state,
                clock.GetUtcNow().UtcDateTime, result.ErrorDetalle, result.ProviderMessageId))
                throw new InvalidOperationException("No se pudo guardar el resultado; no repetir el envío.");
            if (state == EstadoNotificacionWhatsApp.AceptadoPorTwilio)
                await events.RecordAsync(PilotEventTypes.NotificacionAceptada, n.Id, cancellationToken);
            else if (state == EstadoNotificacionWhatsApp.Fallido)
                await events.RecordAsync(PilotEventTypes.NotificacionFallida, n.Id, cancellationToken);
        }
        catch
        {
            await repository.TransitionAsync(n.Id, EstadoNotificacionWhatsApp.Procesando, EstadoNotificacionWhatsApp.RequiereRevision,
                clock.GetUtcNow().UtcDateTime, "Prueba interrumpida; verificar en Twilio antes de actuar.");
            throw;
        }
        return (await repository.GetByIdAsync(n.Id))!;
    }
}
