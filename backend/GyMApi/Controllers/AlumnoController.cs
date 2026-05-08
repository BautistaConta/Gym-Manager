using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManager.API.Repositories;
using GymManager.API.Models;
using GymManager.API.DTOs;

namespace GymManager.API.Controllers
{
    [ApiController]
    [Route("api/alumnos")]
    [Authorize(Roles = "Admin,Gestor")]
    public class AlumnosController : ControllerBase
    {
        private readonly AlumnoRepository _repo;
        private readonly PagoRepository _pagoRepository;

        public AlumnosController(AlumnoRepository repo, PagoRepository pagoRepository)
        {
            _repo = repo;
            _pagoRepository = pagoRepository;
        }

        // POST api/alumnos
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearAlumnoRequest request)
        {
            var existente = await _repo.GetByDniAsync(request.DNI);
            if (existente != null)
                return BadRequest("Ya existe un alumno con ese DNI.");

            var alumno = new Alumno
            {
                Nombre = request.Nombre,
                DNI = request.DNI,
                Telefono = request.Telefono,
                FechaAlta = DateTime.UtcNow,
                Activo = true
            };

            await _repo.CreateAsync(alumno);

            return Ok(alumno);
        }

        // GET api/alumnos
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _repo.GetAllAsync());
        }

        // GET api/alumnos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var alumno = await _repo.GetByIdAsync(id);
            if (alumno == null)
                return NotFound();

            return Ok(alumno);
        }

        // GET api/alumnos/dni/{dni}
        [HttpGet("dni/{dni}")]
        public async Task<IActionResult> GetByDni(string dni)
        {
            var alumno = await _repo.GetByDniAsync(dni);
            if (alumno == null)
                return NotFound();

            return Ok(alumno);
        }

        // GET api/alumnos/search?nombre=juan
        [HttpGet("search")]
        public async Task<IActionResult> SearchByNombre([FromQuery] string nombre)
        {
            var alumnos = await _repo.SearchByNombreAsync(nombre);
            return Ok(alumnos);
        }

        // PUT api/alumnos/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ActualizarAlumnoRequest request)
        {
            var alumno = await _repo.GetByIdAsync(id);
            if (alumno == null)
                return NotFound();

            alumno.Nombre = request.Nombre;
            alumno.Telefono = request.Telefono;
            alumno.Activo = request.Activo;

            await _repo.UpdateAsync(alumno);

            return Ok(alumno);
        }
        [HttpGet("{id}/estado")]
public async Task<IActionResult> GetEstado(string id)
{
    var alumno = await _repo.GetByIdAsync(id);
    if (alumno == null)
        return NotFound("Alumno no encontrado.");

    var ultimoPago = await _pagoRepository.GetUltimoPagoAsync(id);

    if (ultimoPago == null)
    {
        return Ok(new EstadoAlumnoResponse
        {
            AlumnoId = alumno.Id,
            Nombre = alumno.Nombre,
            DNI = alumno.DNI,
            Estado = "SIN_PAGOS",
            FechaVencimiento = null
        });
    }

    var hoy = DateTime.UtcNow.Date;

    var estado = ultimoPago.PeriodoHasta.Date >= hoy
        ? "ACTIVO"
        : "VENCIDO";

    return Ok(new EstadoAlumnoResponse
    {
        AlumnoId = alumno.Id,
        Nombre = alumno.Nombre,
        DNI = alumno.DNI,
        Estado = estado,
        FechaVencimiento = ultimoPago.PeriodoHasta
    });
}
    }
}