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
        private readonly IUserRepository _repo;
        private readonly UserService _userService;
        public UsersController(IUserRepository repo, UserService userService)
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
            try
            {
                var user = await _userService.CambiarRolAsync(id, request.NuevoRol);
                return Ok(new { message = "Rol actualizado correctamente", nuevoRol = user.Rol.ToString() });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        
        // POST api/users/crear-empleado
        [HttpPost("crear-empleado")]
        [Authorize(Roles = "Admin")]
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
    catch (DomainException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

    }
}
