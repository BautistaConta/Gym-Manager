namespace GymManager.API.DTOs;

public class PagoListadoResponse
{
    public string Id { get; set; } = null!;
    public string AlumnoId { get; set; } = null!;
    public string AlumnoNombre { get; set; } = "Alumno eliminado";
    public string AlumnoDni { get; set; } = "-";
    public string SucursalId { get; set; } = null!;
    public string SucursalNombre { get; set; } = "Sucursal eliminada";
    public string CategoriaPagoId { get; set; } = null!;
    public string CategoriaPagoNombre { get; set; } = "Categoría eliminada";
    public DateTime FechaPago { get; set; }
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal PrecioCategoria { get; set; }
    public decimal MontoFinal { get; set; }
    public string MetodoPago { get; set; } = null!;
}
