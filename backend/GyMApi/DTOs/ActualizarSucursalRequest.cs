using System.ComponentModel.DataAnnotations;

namespace GymManager.API.DTOs;

public class ActualizarSucursalRequest
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Direccion { get; set; } = null!;
}
