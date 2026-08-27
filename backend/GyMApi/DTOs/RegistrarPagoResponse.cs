namespace GymManager.API.DTOs
{
public class RegistrarPagoResponse
{
    public string Id { get; set; } = null!;
    public string AlumnoId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;
    public string CategoriaPagoId { get; set; } = null!;

    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    public decimal DescuentoPorcentaje { get; set; }
    public decimal PrecioCategoria { get; set; }
    public decimal MontoFinal { get; set; }

    public string MetodoPago { get; set; } = null!;
}
}
