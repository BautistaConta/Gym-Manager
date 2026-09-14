using System.ComponentModel.DataAnnotations;
namespace GymManager.API.DTOs
{   
public class CrearAlumnoRequest
{
    [Required]
    public string Nombre { get; set; } = null!;

    [Required]
    public string DNI { get; set; } = null!;

    [Required]
    public string Telefono { get; set; } = null!;

    public string? SucursalPrincipalId { get; set; }
    public bool NotificacionesHabilitadas { get; set; } = true;
    public bool ConsentimientoConfirmado { get; set; }
    public string? MedioConsentimiento { get; set; }
}
}
