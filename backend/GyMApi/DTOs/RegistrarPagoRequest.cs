using GymManager.API.Models;
namespace GymManager.API.DTOs
{
public class RegistrarPagoRequest
{
    public string UsuarioId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }

    // cantidad de meses que cubre el pago
    public int Meses { get; set; }
}
}
