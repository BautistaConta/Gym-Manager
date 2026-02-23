namespace GymManager.API.DTOs
{
    public class ActualizarCategoriaPagoRequest
    {
       public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
    public int MesesDuracion { get; set; }
    public bool Activa { get; set; }
    }
}