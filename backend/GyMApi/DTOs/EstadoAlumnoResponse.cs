namespace GymManager.API.DTOs
{
    public class EstadoAlumnoResponse
    {
       public string AlumnoId { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string DNI { get; set; } = null!;

    public string Estado { get; set; } = null!; 
    // SIN_PAGOS | VENCIDA | PROXIMO_A_VENCER | AL_DIA

    public DateTime? FechaVencimiento { get; set; }
    }
}
