using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;

namespace GymManager.API.Repositories;

public interface IAuditoriaRepository
{
    Task RegistrarAsync(string tipo, string entidadId, string? userId = null, string? detalle = null);
}

public sealed class AuditoriaRepository(MongoDbContext context, IGymContext gym, TimeProvider clock) : IAuditoriaRepository
{
    public Task RegistrarAsync(string tipo, string entidadId, string? userId = null, string? detalle = null)
    {
        var value = new EventoAuditoria { Tipo = tipo, EntidadId = entidadId, UserId = userId,
            Detalle = detalle, FechaUtc = clock.GetUtcNow().UtcDateTime };
        TenantFilters.Stamp(value, gym.GymId);
        return context.EventosAuditoria.InsertOneAsync(value);
    }
}
