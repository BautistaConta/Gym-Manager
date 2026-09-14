namespace GymManager.API.DTOs;

public sealed class AlumnoListadoResponse
{
    public string Id { get; init; } = null!;
    public string Nombre { get; init; } = null!;
    public string DNI { get; init; } = null!;
    public string Telefono { get; init; } = null!;
    public bool Activo { get; init; }
    public string? SucursalPrincipalId { get; init; }
    public bool NotificacionesHabilitadas { get; init; }
    public DateTime? FechaConsentimientoNotificacionesUtc { get; init; }
    public string? MedioConsentimientoNotificaciones { get; init; }
    public string Estado { get; init; } = null!;
    public DateTime? FechaVencimiento { get; init; }
}
