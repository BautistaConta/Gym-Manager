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
}
}