using System.ComponentModel.DataAnnotations;

namespace GymManager.API.DTOs;

public class CrearUsuarioRequest
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = string.Empty;
}
