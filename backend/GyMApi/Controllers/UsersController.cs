using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManager.API.Repositories;
using GymManager.API.Models;
using GymManager.API.DTOs;
using GymApi.Models.Roles;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using GymManager.API.Services;

namespace GymManager.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _repo;
        private readonly UserService _userService;
        public UsersController(UserRepository repo, UserService userService)
        {
            _repo = repo;
            _userService = userService;
        }

        
        //GET api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> ObtenerUsuarioActual()
        {
           var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _repo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Nombre,
                user.Email,
                Rol = user.Rol.ToString(),
                user.FechaAlta
            });
        }

        
        // GET api/users
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var rawToken = Request.Headers["Authorization"].ToString();
Console.WriteLine("TOKEN RECIBIDO POR .NET: " + rawToken);
            var users = await _repo.GetAllAsync();

            var response = users.Select(u => new
            {
                u.Id,
                u.Nombre,
                u.Email,
                Rol = u.Rol.ToString(),
                u.FechaAlta
            });

            return Ok(response);
        }

        
        // PUT api/users/{id}/rol
        [HttpPut("{id}/rol")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarRol(string id, [FromBody] UpdateRolRequest request)
        {
           if (!Enum.TryParse<RolUsuario>(request.NuevoRol, true, out var nuevoRol))
    {
        return BadRequest("Rol inválido");
    }

    var user = await _repo.GetByIdAsync(id);
    if (user == null)
        return NotFound(new { message = "Usuario no encontrado" });

    user.Rol = nuevoRol;
    await _repo.UpdateAsync(user);

    return Ok(new
    {
        message = "Rol actualizado correctamente",
        nuevoRol = user.Rol.ToString()
    });
        }

        
        // POST api/users/crear-empleado
        [HttpPost("crear-empleado")]
        [Authorize(Roles = "Admin,Gestor")]
        public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioRequest request)
{
    try
    {
        var usuario = await _userService.CrearUsuarioAsync(request);

        return Ok(new
        {
            message = "Usuario creado correctamente",
            usuario.Id,
            usuario.Email,
            Rol = usuario.Rol.ToString()
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

        [HttpPost("seed-admin")]
        [AllowAnonymous]
public async Task<IActionResult> SeedAdmin()
{
    var existing = await _repo.GetByEmailAsync("admin@seed.com");
    if (existing != null)
        return BadRequest("El usuario admin ya existe.");

    var user = new Usuario
    {
        Nombre = "AdminInicial",
        Email = "admin@seed.com",
        Rol = RolUsuario.Admin,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
        FechaAlta = DateTime.UtcNow
    };

    await _repo.CreateAsync(user);

    return Ok(new
    {
        message = "Admin creado correctamente",
        user.Id,
        user.Email,
        Rol = user.Rol.ToString()
    });
}

    }
}
