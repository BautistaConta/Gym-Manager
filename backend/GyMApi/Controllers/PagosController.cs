using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManager.API.Services;
using GymManager.API.DTOs;

namespace GymManager.API.Controllers
{
    [ApiController]
    [Route("api/pagos")]
    [Authorize(Roles = "Admin,Gestor")]
    public class PagosController : ControllerBase
    {
        private readonly PagoService _service;

        public PagosController(PagoService service)
        {
            _service = service;
        }

        // POST api/pagos
        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromBody] RegistrarPagoRequest request)
        {
            try
            {
                var pago = await _service.RegistrarPagoAsync(request);

                var response = new RegistrarPagoResponse
                {
                    Id = pago.Id,
                    AlumnoId = pago.AlumnoId,
                    SucursalId = pago.SucursalId,
                    CategoriaPagoId = pago.CategoriaPagoId,
                    PeriodoDesde = pago.PeriodoDesde,
                    PeriodoHasta = pago.PeriodoHasta,
                    DescuentoPorcentaje = pago.DescuentoPorcentaje,
                    PrecioCategoria = pago.PrecioCategoria,
                    MontoFinal = pago.MontoFinal,
                    MetodoPago = pago.MetodoPago.ToString()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
