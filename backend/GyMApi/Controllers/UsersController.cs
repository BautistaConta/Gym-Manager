using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManager.API.Repositories;
using GymManager.API.Models;

namespace GymManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _repo;

        public UsersController(UserRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _repo.GetByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(new { user.Id, user.Nombre, user.Email, user.Rol, user.FechaAlta });
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _repo.GetAllAsync();
            var response = users.Select(u => new { u.Id, u.Nombre, u.Email, u.Rol, u.FechaAlta });
            return Ok(response);
        }
    }
}
