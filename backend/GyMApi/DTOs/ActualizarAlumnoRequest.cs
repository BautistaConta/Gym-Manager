using System.ComponentModel.DataAnnotations;
namespace GymManager.API.DTOs
{   
public class ActualizarAlumnoRequest
{
    [Required]
    public string Nombre { get; set; } = null!;

    [Required]
    public string Telefono { get; set; } = null!;

    public bool Activo { get; set; }
}
}