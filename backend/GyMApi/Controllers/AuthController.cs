using Microsoft.AspNetCore.Mvc;
using GymManager.API.Models;
using GymManager.API.Services;

namespace GymManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtService _jwtService;

        public AuthController(UserService userService, JwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _userService.LoginAsync(request);
                var token = _jwtService.GenerateToken(user);
                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        nombre = user.Nombre,
                        email = user.Email,
                        rol = user.Rol.ToString()
                    }
                });
            }
            catch (TimeoutException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "La conexión con la base de datos agotó el tiempo de espera."
                });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
