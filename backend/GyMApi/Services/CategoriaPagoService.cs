using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services;

public class CategoriaPagoService
{
    private readonly CategoriaPagoRepository _categorias;
    public CategoriaPagoService(CategoriaPagoRepository categorias) => _categorias = categorias;
    public Task<List<CategoriaPago>> GetAllAsync() => _categorias.GetAllAsync();
    public Task<CategoriaPago?> GetByIdAsync(string id) => _categorias.GetByIdAsync(id);

    public async Task<CategoriaPago> CreateAsync(CrearCategoriaPagoRequest request)
    {
        Validate(request.Nombre, request.Precio, request.MesesDuracion);
        var categoria = new CategoriaPago { Nombre = request.Nombre.Trim(), Precio = request.Precio, MesesDuracion = request.MesesDuracion, TipoAbono = request.TipoAbono, Activa = true };
        await _categorias.CreateAsync(categoria);
        return categoria;
    }

    public async Task<CategoriaPago> UpdateAsync(string id, ActualizarCategoriaPagoRequest request)
    {
        Validate(request.Nombre, request.Precio, request.MesesDuracion);
        var categoria = await _categorias.GetByIdAsync(id) ?? throw new DomainException("Categoría no encontrada.");
        categoria.Nombre = request.Nombre.Trim();
        categoria.Precio = request.Precio;
        categoria.MesesDuracion = request.MesesDuracion;
        categoria.TipoAbono = request.TipoAbono;
        categoria.Activa = request.Activa;
        await _categorias.UpdateAsync(categoria);
        return categoria;
    }

    public async Task DeactivateAsync(string id)
    {
        var categoria = await _categorias.GetByIdAsync(id) ?? throw new DomainException("Categoría no encontrada.");
        categoria.Activa = false;
        await _categorias.UpdateAsync(categoria);
    }

    public Task DeleteLegacyWithoutIdAsync() => _categorias.DeleteLegacyWithoutIdAsync();

    private static void Validate(string nombre, decimal precio, int meses)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("El nombre es obligatorio.");
        if (precio < 0) throw new DomainException("El precio no puede ser negativo.");
        if (meses <= 0) throw new DomainException("La duración debe ser mayor a cero.");
    }
}
