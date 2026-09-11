namespace GymManager.API.Models
{   
public class Alumno : IGymOwned
{
    public string GymId { get; set; } = null!;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = null!;
    public string DNI { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public bool NotificacionesHabilitadas { get; set; } = true;
    public DateTime? UltimaNotificacionEnviada { get; set; }
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; }
    public string? SucursalPrincipalId { get; set; }
}
}
