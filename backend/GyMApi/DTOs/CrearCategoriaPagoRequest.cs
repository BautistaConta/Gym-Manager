using GyMApi.Models;
namespace GymManager.API.DTOs
{
    public class CrearCategoriaPagoRequest
    {
        public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
    public int MesesDuracion { get; set; }
    public TipoAbono TipoAbono { get; set; }
    }
}