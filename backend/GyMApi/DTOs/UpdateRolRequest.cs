using System.ComponentModel.DataAnnotations;

namespace GymManager.API.DTOs;

public class UpdateRolRequest
{
    [Required]
    public string NuevoRol { get; set; } = string.Empty;
}
