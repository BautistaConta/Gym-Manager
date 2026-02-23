using GymManager.API.Models;
namespace GymManager.API.DTOs
{
public class RegistrarPagoRequest
{
    public string UsuarioId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;
    public string CategoriaPagoId { get; set; } = null!;

    public MetodoPago MetodoPago { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    // null = automático
    // con valor = modo manual
    public DateTime? PeriodoHastaManual { get; set; }
}
}
