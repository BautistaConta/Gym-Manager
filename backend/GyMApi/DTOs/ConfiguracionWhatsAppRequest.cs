using System.ComponentModel.DataAnnotations;

namespace GymManager.API.DTOs;

public sealed class ConfiguracionWhatsAppRequest
{
    [Required, MaxLength(100)] public string NombreComercial { get; set; } = null!;
    [MaxLength(500)] public string? WhatsAppGroupInviteUrl { get; set; }
    [MaxLength(200)] public string? TextoInicialChat { get; set; }
}
