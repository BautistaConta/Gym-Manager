using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.Senders;
using GymManager.API.Options;
using Microsoft.Extensions.Options;
using GymManager.API.DTOs;

namespace GymManager.API.Services;

public sealed class NotificacionService(
    INotificacionRepository notificaciones,
    INotificacionDatos datos,
    IWhatsAppSender sender,
    CuotaCalculator cuotas,
    TimeProvider clock,
    IOptions<TwilioOptions> twilio,
    IPilotEventRecorder events)
{
    public async Task<NotificacionHistorialResponse> GetHistorialAsync(NotificacionHistorialQuery query)
    {
        if (query.Pagina < 1 || query.TamanoPagina is < 1 or > 100) throw new DomainException("Página inválida; el tamaño debe estar entre 1 y 100.");
        if (query.DesdeUtc > query.HastaUtc) throw new DomainException("El rango de fechas es inválido.");
        var page = await notificaciones.SearchAsync(query);
        var alumnos = (await datos.GetAlumnosAsync(page.Items.Select(n => n.AlumnoId).Distinct())).ToDictionary(a => a.Id);
        var sucursales = (await datos.GetSucursalesAsync(page.Items.Where(n => n.SucursalId is not null).Select(n => n.SucursalId!).Distinct())).ToDictionary(s => s.Id);
        var items = page.Items.Select(n =>
        {
            alumnos.TryGetValue(n.AlumnoId, out var alumno);
            var sucursal = n.SucursalId is not null ? sucursales.GetValueOrDefault(n.SucursalId) : null;
            return new NotificacionHistorialItem(n.Id, n.AlumnoId, alumno?.Nombre ?? "Alumno no disponible",
                n.Telefono, n.SucursalId, sucursal?.Nombre, n.Tipo, n.FechaVencimiento, n.FechaCreacion,
                n.FechaEnvio, n.Estado, n.Estado.ToString(), n.Intentos, n.ProviderMessageId,
                n.CorrelationId, SafeError(n.ErrorDetalle), n.Tipo == TipoNotificacionWhatsApp.PorVencer &&
                    n.Estado is EstadoNotificacionWhatsApp.Fallido or EstadoNotificacionWhatsApp.RequiereRevision);
        }).ToList();
        return new(items, page.Total, query.Pagina, query.TamanoPagina,
            (int)Math.Ceiling(page.Total / (double)query.TamanoPagina), page.Contadores);
    }

    public Task<NotificacionWhatsApp?> GetByIdAsync(string id) => notificaciones.GetByIdAsync(id);

    public async Task<NotificacionWhatsApp?> EncolarSiCorrespondeAsync(Alumno alumno, Pago pago, TipoNotificacionWhatsApp tipo)
    {
        if (!EsElegible(alumno, pago, tipo)) return null;
        var now = clock.GetUtcNow().UtcDateTime;
        var notificacion = new NotificacionWhatsApp
        {
            GymId = alumno.GymId,
            AlumnoId = alumno.Id,
            PagoId = pago.Id,
            SucursalId = pago.SucursalId,
            FechaVencimiento = pago.PeriodoHasta,
            ClaveDeduplicacion = $"{alumno.GymId}:{pago.Id}:{tipo}",
            Tipo = tipo,
            Telefono = alumno.Telefono,
            ContentSid = twilio.Value.PorVencerContentSid,
            VariablesPlantilla = new() { ["1"] = alumno.Nombre, ["2"] = pago.PeriodoHasta.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) },
            Mensaje = $"Hola {alumno.Nombre}, tu cuota vence el {pago.PeriodoHasta:dd/MM/yyyy}. ¡No te quedes sin entrenar!",
            Estado = EstadoNotificacionWhatsApp.Pendiente,
            FechaCreacion = now,
            FechaActualizacion = now
        };
        return await notificaciones.CreateIfAbsentAsync(notificacion);
    }

    public async Task<NotificacionWhatsApp> EncolarRecordatorioManualAsync(Alumno alumno, Pago pago)
    {
        var ultimoPago = await datos.GetUltimoPagoAsync(alumno.Id);
        if (ultimoPago?.Id != pago.Id)
            throw new DomainException("Ese pago es histórico; usá el período más reciente del alumno.");
        var estado = cuotas.Evaluar(pago.PeriodoHasta).Estado;
        if (estado != EstadoCuota.PROXIMO_A_VENCER)
            throw new DomainException("Solo se envían avisos antes del vencimiento, dentro de la ventana configurada.");
        var tipo = TipoNotificacionWhatsApp.PorVencer;
        var notificacion = await EncolarSiCorrespondeAsync(alumno, pago, tipo)
            ?? throw new DomainException("El alumno no está habilitado para recibir este recordatorio.");
        if (notificacion.Estado != EstadoNotificacionWhatsApp.Pendiente)
            throw new DomainException($"Ya existe un recordatorio {tipo} para este pago (estado: {notificacion.Estado}).");
        return notificacion;
    }

    public bool EsElegible(Alumno alumno, Pago pago, TipoNotificacionWhatsApp tipo)
    {
        if (tipo != TipoNotificacionWhatsApp.PorVencer) return false;
        if (!alumno.Activo || !alumno.NotificacionesHabilitadas ||
            alumno.FechaConsentimientoWhatsApp is null ||
            string.IsNullOrWhiteSpace(alumno.Telefono) || !PhoneIsValid(alumno.Telefono)) return false;
        if (alumno.GymId != pago.GymId || alumno.Id != pago.AlumnoId) return false;
        var estado = cuotas.Evaluar(pago.PeriodoHasta).Estado;
        return estado == EstadoCuota.PROXIMO_A_VENCER;
    }

    public Task<long> RevisarProcesandoAlIniciarAsync() =>
        notificaciones.MarkProcessingForReviewAsync(clock.GetUtcNow().UtcDateTime);

    public async Task ProcesarPendientesAsync(int limite, int campaignDelayMilliseconds = 0, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < limite; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var notificacion = await notificaciones.ClaimNextAsync(clock.GetUtcNow().UtcDateTime);
            if (notificacion is null) return;
            try
            {
                var alumno = await datos.GetAlumnoAsync(notificacion.AlumnoId);
                var ultimoPago = notificacion.PagoId is null ? null : await datos.GetUltimoPagoAsync(notificacion.AlumnoId);
                var elegible = alumno is not null && alumno.Activo && alumno.NotificacionesHabilitadas &&
                    alumno.FechaConsentimientoWhatsApp is not null && alumno.Telefono == notificacion.Telefono &&
                    PhoneIsValid(alumno.Telefono);
                if (notificacion.PagoId is not null)
                    elegible = elegible && ultimoPago?.Id == notificacion.PagoId &&
                        ultimoPago.SucursalId == notificacion.SucursalId && EsElegible(alumno!, ultimoPago, notificacion.Tipo);
                else if (notificacion.Tipo is TipoNotificacionWhatsApp.PorVencer or TipoNotificacionWhatsApp.Vencido)
                    elegible = false;
                if (!elegible)
                {
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.Descartado,
                        "El pago o la elegibilidad del alumno cambió antes del envío.");
                    continue;
                }

                var resultado = await sender.SendAsync(notificacion, cancellationToken);
                if (resultado.Simulado)
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.Simulado, resultado.ErrorDetalle);
                else if (resultado.Exitoso && !string.IsNullOrWhiteSpace(resultado.ProviderMessageId))
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.AceptadoPorTwilio,
                        providerMessageId: resultado.ProviderMessageId);
                else if (!resultado.Exitoso && resultado.ResultadoDefinitivo)
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.Fallido,
                        resultado.ErrorDetalle ?? "El proveedor rechazó el mensaje.");
                else
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.RequiereRevision,
                        resultado.ErrorDetalle ?? "No se pudo confirmar si el proveedor aceptó el mensaje.");
                if (notificacion.CampaniaId is not null && campaignDelayMilliseconds > 0)
                    await Task.Delay(campaignDelayMilliseconds, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch
            {
                // Un error después del reclamo puede ocurrir tras el envío: nunca reintentar a ciegas.
                await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.RequiereRevision,
                    "Error inesperado durante el procesamiento; verificar en el proveedor.");
            }
        }
    }

    public async Task<NotificacionWhatsApp> ReenviarAsync(string id)
    {
        var notificacion = await notificaciones.GetByIdAsync(id) ?? throw new DomainException("Notificación no encontrada.");
        if (notificacion.Tipo != TipoNotificacionWhatsApp.PorVencer)
            throw new DomainException("El piloto solo permite avisos previos al vencimiento.");
        if (notificacion.Estado is not (EstadoNotificacionWhatsApp.Fallido or EstadoNotificacionWhatsApp.RequiereRevision))
            throw new DomainException("Solo se pueden reintentar notificaciones fallidas o en revisión.");
        var alumno = await datos.GetAlumnoAsync(notificacion.AlumnoId);
        var elegible = alumno is not null && alumno.Activo && alumno.NotificacionesHabilitadas &&
            alumno.FechaConsentimientoWhatsApp is not null && alumno.Telefono == notificacion.Telefono &&
            PhoneIsValid(alumno.Telefono);
        if (notificacion.PagoId is not null)
        {
            var ultimoPago = await datos.GetUltimoPagoAsync(notificacion.AlumnoId);
            elegible = elegible && ultimoPago?.Id == notificacion.PagoId && EsElegible(alumno!, ultimoPago, notificacion.Tipo);
        }
        if (!elegible)
            throw new DomainException("El alumno o el período ya no es elegible para el recordatorio.");
        if (!await notificaciones.TransitionAsync(id, notificacion.Estado,
            EstadoNotificacionWhatsApp.Pendiente, clock.GetUtcNow().UtcDateTime))
            throw new DomainException("El estado de la notificación cambió; actualizá la pantalla.");
        await events.RecordAsync(PilotEventTypes.NotificacionReenviada, id);
        return (await notificaciones.GetByIdAsync(id))!;
    }

    private async Task TransicionarAsync(NotificacionWhatsApp notificacion, EstadoNotificacionWhatsApp next,
        string? error = null, string? providerMessageId = null)
    {
        if (!await notificaciones.TransitionAsync(notificacion.Id, EstadoNotificacionWhatsApp.Procesando,
            next, clock.GetUtcNow().UtcDateTime, error is { Length: > 1000 } ? error[..1000] : error, providerMessageId))
            throw new InvalidOperationException("No se pudo registrar el resultado del envío.");
        if (next == EstadoNotificacionWhatsApp.AceptadoPorTwilio)
            await events.RecordAsync(PilotEventTypes.NotificacionAceptada, notificacion.Id);
        else if (next == EstadoNotificacionWhatsApp.Fallido)
            await events.RecordAsync(PilotEventTypes.NotificacionFallida, notificacion.Id);
    }

    public static bool PhoneIsValid(string telefono) =>
        System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^\+[1-9]\d{7,14}$");

    public static void ValidatePhone(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono) || !PhoneIsValid(telefono))
            throw new DomainException("El teléfono debe estar en formato E.164, por ejemplo +5493811234567.");
    }

    private static string? SafeError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error)) return null;
        var singleLine = System.Text.RegularExpressions.Regex.Replace(error, @"\s+", " ").Trim();
        singleLine = System.Text.RegularExpressions.Regex.Replace(singleLine,
            @"(?i)(auth(?:orization|token)?|password|secret|token)\s*[:=]\s*[^\s;,]+", "$1=[oculto]");
        singleLine = System.Text.RegularExpressions.Regex.Replace(singleLine,
            @"(?i)mongodb(?:\+srv)?://[^@\s]+@", "mongodb://[oculto]@");
        return singleLine.Length <= 240 ? singleLine : singleLine[..240] + "…";
    }
}
