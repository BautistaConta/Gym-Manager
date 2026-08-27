using GymManager.API.Models;
using System.ComponentModel.DataAnnotations;
namespace GymManager.API.DTOs
{
    public class CrearCategoriaPagoRequest
    {
    [Required]
    public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
    public int MesesDuracion { get; set; }
    public TipoAbono TipoAbono { get; set; }
    }
}
