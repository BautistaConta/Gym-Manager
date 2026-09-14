namespace GymManager.API.DTOs;

public class ActualizarNotificacionesAlumnoRequest
{
    public bool NotificacionesHabilitadas { get; set; }
    public string? MedioConsentimiento { get; set; }
    public bool ConsentimientoConfirmado { get; set; }
}
