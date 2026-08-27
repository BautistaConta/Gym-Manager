using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services;

public class SucursalService
{
    private readonly SucursalRepository _sucursales;
    private readonly PagoRepository _pagos;
    public SucursalService(SucursalRepository sucursales, PagoRepository pagos) { _sucursales = sucursales; _pagos = pagos; }
    public Task<List<Sucursal>> GetAllAsync() => _sucursales.GetAllAsync();
    public Task<Sucursal?> GetByIdAsync(string id) => _sucursales.GetByIdAsync(id);

    public async Task<Sucursal> CreateAsync(CrearSucursalRequest request)
    {
        Validate(request.Nombre, request.Direccion);
        var sucursal = new Sucursal { Nombre = request.Nombre.Trim(), Direccion = request.Direccion.Trim(), FechaAlta = DateTime.UtcNow };
        await _sucursales.CreateAsync(sucursal);
        return sucursal;
    }

    public async Task<Sucursal> UpdateAsync(string id, ActualizarSucursalRequest request)
    {
        Validate(request.Nombre, request.Direccion);
        var sucursal = await _sucursales.GetByIdAsync(id) ?? throw new DomainException("Sucursal no encontrada.");
        sucursal.Nombre = request.Nombre.Trim(); sucursal.Direccion = request.Direccion.Trim();
        await _sucursales.UpdateAsync(sucursal);
        return sucursal;
    }

    public async Task DeleteAsync(string id)
    {
        if (await _sucursales.GetByIdAsync(id) is null) throw new DomainException("Sucursal no encontrada.");
        if (await _pagos.CountBySucursalIdAsync(id) > 0) throw new DomainException("No se puede eliminar una sucursal con pagos registrados.");
        await _sucursales.DeleteAsync(id);
    }

    public Task DeleteLegacyWithoutIdAsync() => _sucursales.DeleteLegacyWithoutIdAsync();

    private static void Validate(string nombre, string direccion)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(direccion)) throw new DomainException("Nombre y dirección son obligatorios.");
    }
}
