using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Repositories;
using Microsoft.Extensions.Options;

namespace GymManager.API.Services;

public sealed record ResultadoBienvenida(string Estado, string? Motivo = null);

public sealed class BienvenidaWhatsAppService(
    INotificacionRepository notificaciones,
    IConfiguracionGymRepository configuraciones,
    IAuditoriaRepository auditoria,
    IOptions<TwilioOptions> options,
    TimeProvider clock)
{
    private readonly TwilioOptions _options = options.Value;

    public async Task<ResultadoBienvenida> EncolarAsync(Alumno alumno)
    {
        if (!alumno.Activo || !alumno.NotificacionesHabilitadas || alumno.FechaConsentimientoWhatsApp is null)
            return new("Omitida", "El alumno no tiene consentimiento vigente.");
        if (!NotificacionService.PhoneIsValid(alumno.Telefono))
            return new("Omitida", "El teléfono no tiene formato E.164 válido.");
        var config = await configuraciones.GetAsync();
        if (config is null || string.IsNullOrWhiteSpace(config.WhatsAppGroupInviteUrl))
            return new("Omitida", "No está configurado el enlace de invitación al grupo de WhatsApp.");
        if (string.IsNullOrWhiteSpace(_options.WelcomeContentSid))
            return new("Omitida", "No está configurado Twilio:WelcomeContentSid.");
        var now = clock.GetUtcNow().UtcDateTime;
        var value = await notificaciones.CreateIfAbsentAsync(new NotificacionWhatsApp
        {
            GymId = alumno.GymId, AlumnoId = alumno.Id,
            ClaveDeduplicacion = $"{alumno.GymId}:bienvenida:{alumno.Id}",
            Tipo = TipoNotificacionWhatsApp.Bienvenida, Telefono = alumno.Telefono,
            ContentSid = _options.WelcomeContentSid,
            VariablesPlantilla = new() { ["1"] = alumno.Nombre, ["2"] = config.NombreComercial, ["3"] = config.WhatsAppGroupInviteUrl },
            Mensaje = $"Hola {alumno.Nombre}, te damos la bienvenida a {config.NombreComercial}. Grupo: {config.WhatsAppGroupInviteUrl}",
            Estado = EstadoNotificacionWhatsApp.Pendiente, FechaCreacion = now, FechaActualizacion = now
        });
        await auditoria.RegistrarAsync("BienvenidaEncolada", alumno.Id, detalle: value.Id);
        return new("Encolada");
    }
}
