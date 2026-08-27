using GymManager.API.Models;
using System.ComponentModel.DataAnnotations;
namespace GymManager.API.DTOs
{
public class RegistrarPagoRequest
{
    [Required]
    public string AlumnoId { get; set; } = null!;
    [Required]
    public string SucursalId { get; set; } = null!;
    [Required]
    public string CategoriaPagoId { get; set; } = null!;

    public MetodoPago MetodoPago { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    // null → automático
    // valor → manual
    public DateTime? PeriodoHastaManual { get; set; }
}
}
