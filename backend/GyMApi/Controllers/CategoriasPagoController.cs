using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers
{
    [ApiController]
    [Route("api/categorias-pago")]
    [Authorize(Roles = "Admin,Gestor")]
    public class CategoriasPagoController : ControllerBase
    {
        private readonly CategoriaPagoRepository _repo;

        public CategoriasPagoController(CategoriaPagoRepository repo)
        {
            _repo = repo;
        }

        // GET api/categorias-pago
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categorias = await _repo.GetAllAsync();
            return Ok(categorias);
        }

        // POST api/categorias-pago
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearCategoriaPagoRequest request)
        {
            var categoria = new CategoriaPago
            {
                Nombre = request.Nombre,
                Precio = request.Precio,
                MesesDuracion = request.MesesDuracion,
                TipoAbono = request.TipoAbono,
                Activa = true
            };

            await _repo.CreateAsync(categoria);

            return Ok(categoria);
        }

        // PUT api/categorias-pago/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ActualizarCategoriaPagoRequest request)
        {
            var categoria = await _repo.GetByIdAsync(id);
            if (categoria == null)
                return NotFound();

            categoria.Nombre = request.Nombre;
            categoria.Precio = request.Precio;
            categoria.MesesDuracion = request.MesesDuracion;
            categoria.Activa = request.Activa;

            await _repo.UpdateAsync(categoria);

            return Ok(categoria);
        }
    }
}