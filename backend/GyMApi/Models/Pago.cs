namespace GymManager.API.Models
{
public class Pago
{
    public string Id { get; set; } = null!;

    public string UsuarioId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;

    public decimal Monto { get; set; }

    public MetodoPago MetodoPago { get; set; }

    public DateTime FechaPago { get; set; }

    // Período que cubre el pago
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
}
}
