public class Pago
{
    public string Id { get; set; } = null!;

    public string UsuarioId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;

    public string CategoriaPagoId { get; set; } = null!;

    public MetodoPago MetodoPago { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    public DateTime FechaPago { get; set; }

    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
}

