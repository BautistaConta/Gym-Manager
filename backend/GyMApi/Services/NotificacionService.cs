using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.Senders;

namespace GymManager.API.Services;

public sealed class NotificacionService(
    INotificacionRepository notificaciones,
    INotificacionDatos datos,
    IWhatsAppSender sender,
    CuotaCalculator cuotas,
    TimeProvider clock)
{
    public Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId) =>
        notificaciones.GetAllAsync(estado, alumnoId);

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
            Mensaje = tipo == TipoNotificacionWhatsApp.PorVencer
                ? $"Hola {alumno.Nombre}, tu cuota vence el {pago.PeriodoHasta:dd/MM/yyyy}. ¡No te quedes sin entrenar!"
                : $"Hola {alumno.Nombre}, tu cuota venció el {pago.PeriodoHasta:dd/MM/yyyy}. Regularizá tu pago para seguir entrenando.",
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
        var tipo = estado switch
        {
            EstadoCuota.PROXIMO_A_VENCER => TipoNotificacionWhatsApp.PorVencer,
            EstadoCuota.VENCIDA => TipoNotificacionWhatsApp.Vencido,
            _ => throw new DomainException("El pago todavía no está en la ventana de recordatorio.")
        };
        var notificacion = await EncolarSiCorrespondeAsync(alumno, pago, tipo)
            ?? throw new DomainException("El alumno no está habilitado para recibir este recordatorio.");
        if (notificacion.Estado != EstadoNotificacionWhatsApp.Pendiente)
            throw new DomainException($"Ya existe un recordatorio {tipo} para este pago (estado: {notificacion.Estado}).");
        return notificacion;
    }

    public bool EsElegible(Alumno alumno, Pago pago, TipoNotificacionWhatsApp tipo)
    {
        if (tipo is not (TipoNotificacionWhatsApp.PorVencer or TipoNotificacionWhatsApp.Vencido)) return false;
        if (!alumno.Activo || !alumno.NotificacionesHabilitadas ||
            alumno.FechaConsentimientoNotificacionesUtc is null ||
            string.IsNullOrWhiteSpace(alumno.Telefono) || !PhoneIsValid(alumno.Telefono)) return false;
        if (alumno.GymId != pago.GymId || alumno.Id != pago.AlumnoId) return false;
        var estado = cuotas.Evaluar(pago.PeriodoHasta).Estado;
        return tipo == TipoNotificacionWhatsApp.PorVencer
            ? estado == EstadoCuota.PROXIMO_A_VENCER
            : estado == EstadoCuota.VENCIDA;
    }

    public Task<long> RevisarProcesandoAlIniciarAsync() =>
        notificaciones.MarkProcessingForReviewAsync(clock.GetUtcNow().UtcDateTime);

    public async Task ProcesarPendientesAsync(int limite, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < limite; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var notificacion = await notificaciones.ClaimNextAsync(clock.GetUtcNow().UtcDateTime);
            if (notificacion is null) return;
            try
            {
                var alumno = await datos.GetAlumnoAsync(notificacion.AlumnoId);
                var ultimoPago = await datos.GetUltimoPagoAsync(notificacion.AlumnoId);
                if (alumno is null || ultimoPago is null || ultimoPago.Id != notificacion.PagoId ||
                    ultimoPago.SucursalId != notificacion.SucursalId ||
                    alumno.Telefono != notificacion.Telefono ||
                    !EsElegible(alumno, ultimoPago, notificacion.Tipo))
                {
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.Descartado,
                        "El pago o la elegibilidad del alumno cambió antes del envío.");
                    continue;
                }

                var resultado = await sender.SendAsync(notificacion.Telefono, notificacion.Mensaje,
                    notificacion.Tipo, cancellationToken);
                if (resultado.Exitoso && !string.IsNullOrWhiteSpace(resultado.ProviderMessageId))
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.Enviado,
                        providerMessageId: resultado.ProviderMessageId);
                else if (!resultado.Exitoso && resultado.ResultadoDefinitivo)
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.Fallido,
                        resultado.ErrorDetalle ?? "El proveedor rechazó el mensaje.");
                else
                    await TransicionarAsync(notificacion, EstadoNotificacionWhatsApp.RequiereRevision,
                        resultado.ErrorDetalle ?? "No se pudo confirmar si el proveedor aceptó el mensaje.");
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
        if (notificacion.Estado != EstadoNotificacionWhatsApp.Fallido)
            throw new DomainException("Solo se pueden reintentar manualmente rechazos definitivos; los casos ambiguos requieren revisión.");
        var alumno = await datos.GetAlumnoAsync(notificacion.AlumnoId);
        var ultimoPago = await datos.GetUltimoPagoAsync(notificacion.AlumnoId);
        if (alumno is null || ultimoPago?.Id != notificacion.PagoId ||
            alumno.Telefono != notificacion.Telefono || !EsElegible(alumno, ultimoPago, notificacion.Tipo))
            throw new DomainException("El alumno o el período ya no es elegible para el recordatorio.");
        if (!await notificaciones.TransitionAsync(id, EstadoNotificacionWhatsApp.Fallido,
            EstadoNotificacionWhatsApp.Pendiente, clock.GetUtcNow().UtcDateTime))
            throw new DomainException("El estado de la notificación cambió; actualizá la pantalla.");
        return (await notificaciones.GetByIdAsync(id))!;
    }

    private async Task TransicionarAsync(NotificacionWhatsApp notificacion, EstadoNotificacionWhatsApp next,
        string? error = null, string? providerMessageId = null)
    {
        if (!await notificaciones.TransitionAsync(notificacion.Id, EstadoNotificacionWhatsApp.Procesando,
            next, clock.GetUtcNow().UtcDateTime, error is { Length: > 1000 } ? error[..1000] : error, providerMessageId))
            throw new InvalidOperationException("No se pudo registrar el resultado del envío.");
    }

    public static bool PhoneIsValid(string telefono) =>
        System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^\+[1-9]\d{7,14}$");

    public static void ValidatePhone(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono) || !PhoneIsValid(telefono))
            throw new DomainException("El teléfono debe estar en formato E.164, por ejemplo +5493811234567.");
    }
}
