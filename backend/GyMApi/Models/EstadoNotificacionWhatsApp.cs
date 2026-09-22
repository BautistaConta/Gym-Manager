namespace GymManager.API.Models;

public enum EstadoNotificacionWhatsApp
{
    Pendiente,
    AceptadoPorTwilio,
    Fallido,
    Procesando,
    RequiereRevision,
    Descartado,
    Simulado
}
