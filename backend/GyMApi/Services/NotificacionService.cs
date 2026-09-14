using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.Senders;

namespace GymManager.API.Services;

public class NotificacionService
{
    private readonly INotificacionRepository _notificaciones;
    private readonly AlumnoRepository _alumnos;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly CuotaCalculator _cuotas;
    public NotificacionService(INotificacionRepository notificaciones, AlumnoRepository alumnos, IWhatsAppSender whatsAppSender, CuotaCalculator cuotas) { _notificaciones = notificaciones; _alumnos = alumnos; _whatsAppSender = whatsAppSender; _cuotas = cuotas; }
    public Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId) => _notificaciones.GetAllAsync(estado, alumnoId);
    public Task<NotificacionWhatsApp?> GetByIdAsync(string id) => _notificaciones.GetByIdAsync(id);
    public Task<bool> ExisteDesdeAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde) => _notificaciones.ExistsSinceAsync(alumnoId, tipo, desde);
    public Task<bool> ExisteEnviadaDesdeAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde) => _notificaciones.ExistsEnviadaDesdeAsync(alumnoId, tipo, desde);

    public Task<NotificacionWhatsApp> EncolarPagoConfirmadoAsync(Alumno alumno, Pago pago) => EncolarAsync(alumno, TipoNotificacionWhatsApp.PagoConfirmado, $"Hola {alumno.Nombre}, registramos tu pago de ${pago.MontoFinal:0.00}. ¡Gracias!");
    public Task<NotificacionWhatsApp> EncolarPorVencerAsync(Alumno alumno, DateTime fechaVencimiento) => EncolarAsync(alumno, TipoNotificacionWhatsApp.PorVencer, $"Hola {alumno.Nombre}, tu cuota vence el {fechaVencimiento:dd/MM/yyyy}. ¡No te quedes sin entrenar!");
    public Task<NotificacionWhatsApp> EncolarVencidoAsync(Alumno alumno, DateTime fechaVencimiento) => EncolarAsync(alumno, TipoNotificacionWhatsApp.Vencido, $"Hola {alumno.Nombre}, tu cuota venció el {fechaVencimiento:dd/MM/yyyy}. Regularizá tu pago para seguir entrenando.");

    public async Task<NotificacionWhatsApp> EnviarRecordatorioManualAsync(Alumno alumno, DateTime fechaVencimiento)
    {
        var tipo = _cuotas.Evaluar(fechaVencimiento).Estado == EstadoCuota.VENCIDA ? TipoNotificacionWhatsApp.Vencido : TipoNotificacionWhatsApp.PorVencer;
        var mensaje = tipo == TipoNotificacionWhatsApp.Vencido
            ? $"Hola {alumno.Nombre}, tu cuota venció el {fechaVencimiento:dd/MM/yyyy}. Regularizá tu pago para seguir entrenando."
            : $"Hola {alumno.Nombre}, te recordamos que tu cuota vence el {fechaVencimiento:dd/MM/yyyy}. ¡No te quedes sin entrenar!";
        var notificacion = await EncolarAsync(alumno, tipo, mensaje);
        var resultado = await _whatsAppSender.SendAsync(notificacion.Telefono, notificacion.Mensaje, notificacion.Tipo);
        if (resultado.Exitoso) await MarcarEnviadaAsync(notificacion);
        else await MarcarFallidaAsync(notificacion, resultado.ErrorDetalle ?? "No se pudo enviar el recordatorio.");
        return notificacion;
    }

    public async Task MarcarEnviadaAsync(NotificacionWhatsApp notificacion)
    {
        notificacion.Estado = EstadoNotificacionWhatsApp.Enviado;
        notificacion.FechaEnvio = DateTime.UtcNow;
        notificacion.ErrorDetalle = null;
        await _notificaciones.UpdateAsync(notificacion);
        var alumno = await _alumnos.GetByIdAsync(notificacion.AlumnoId);
        if (alumno is not null) { alumno.UltimaNotificacionEnviada = notificacion.FechaEnvio; await _alumnos.UpdateAsync(alumno); }
    }

    public async Task MarcarFallidaAsync(NotificacionWhatsApp notificacion, string errorDetalle)
    {
        notificacion.Estado = EstadoNotificacionWhatsApp.Fallido;
        notificacion.ErrorDetalle = errorDetalle.Length > 1000 ? errorDetalle[..1000] : errorDetalle;
        await _notificaciones.UpdateAsync(notificacion);
    }

    private async Task<NotificacionWhatsApp> EncolarAsync(Alumno alumno, TipoNotificacionWhatsApp tipo, string mensaje)
    {
        if (!alumno.NotificacionesHabilitadas) throw new DomainException("El alumno no tiene notificaciones habilitadas.");
        ValidatePhone(alumno.Telefono);
        var notificacion = new NotificacionWhatsApp { GymId = alumno.GymId, AlumnoId = alumno.Id, Tipo = tipo, Telefono = alumno.Telefono, Mensaje = mensaje, Estado = EstadoNotificacionWhatsApp.Pendiente, FechaCreacion = DateTime.UtcNow };
        await _notificaciones.CreateAsync(notificacion);
        return notificacion;
    }

    public async Task<NotificacionWhatsApp> ReenviarAsync(string id)
    {
        var notificacion = await _notificaciones.GetByIdAsync(id) ?? throw new DomainException("Notificación no encontrada.");
        if (notificacion.Estado != EstadoNotificacionWhatsApp.Fallido) throw new DomainException("Solo se pueden reenviar notificaciones fallidas.");
        notificacion.Estado = EstadoNotificacionWhatsApp.Pendiente;
        notificacion.FechaEnvio = null;
        notificacion.ErrorDetalle = null;
        await _notificaciones.UpdateAsync(notificacion);
        return notificacion;
    }

    public static void ValidatePhone(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono) || !System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^\+[1-9]\d{7,14}$"))
            throw new DomainException("El teléfono debe estar en formato E.164, por ejemplo +5493811234567.");
    }
}
