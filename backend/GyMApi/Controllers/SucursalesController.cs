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
[Route("api/sucursales")]
[Authorize(Roles = "Admin")]
public class SucursalesController : ControllerBase
{
    private readonly SucursalRepository _repo;

    public SucursalesController(SucursalRepository repo)
    {
        _repo = repo;
    }

    [HttpPost]
    public async Task<IActionResult> CrearSucursal([FromBody] CrearSucursalRequest request)
    {
        var sucursal = new Sucursal
        {
            Nombre = request.Nombre,
            Direccion = request.Direccion,
            FechaAlta = DateTime.UtcNow
        };

        await _repo.CreateAsync(sucursal);

        return Ok(sucursal);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _repo.GetAllAsync());
    }
}
}