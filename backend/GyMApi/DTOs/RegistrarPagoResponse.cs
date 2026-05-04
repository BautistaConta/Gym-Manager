namespace GymManager.API.DTOs
{
public class RegistrarPagoResponse
{
    public string AlumnoId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;

    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    public string MetodoPago { get; set; } = null!;
}
}