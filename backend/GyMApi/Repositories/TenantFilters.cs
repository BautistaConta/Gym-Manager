using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public static class TenantFilters
{
    public static FilterDefinition<T> ForGym<T>(string gymId) where T : IGymOwned =>
        Builders<T>.Filter.Eq(entity => entity.GymId, gymId);

    public static FilterDefinition<T> And<T>(string gymId, FilterDefinition<T> filter) where T : IGymOwned =>
        Builders<T>.Filter.And(ForGym<T>(gymId), filter);

    public static void Stamp<T>(T entity, string gymId) where T : IGymOwned => entity.GymId = gymId;

    public static void EnsureOwned<T>(T entity, string gymId) where T : IGymOwned
    {
        if (!string.Equals(entity.GymId, gymId, StringComparison.Ordinal))
            throw new InvalidOperationException("No se puede modificar un documento de otro gimnasio.");
    }
}
