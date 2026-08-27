using System.ComponentModel.DataAnnotations;
using GymManager.API.Models;

namespace GymManager.API.DTOs
{
    public class ActualizarCategoriaPagoRequest
    {
    [Required]
    public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
    public int MesesDuracion { get; set; }
    public TipoAbono TipoAbono { get; set; }
    public bool Activa { get; set; }
    }
}
