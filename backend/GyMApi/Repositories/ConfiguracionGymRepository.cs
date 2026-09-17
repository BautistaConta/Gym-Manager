using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public interface IConfiguracionGymRepository
{
    Task<ConfiguracionGym?> GetAsync();
    Task UpsertAsync(ConfiguracionGym value);
}

public sealed class ConfiguracionGymRepository(MongoDbContext context, IGymContext gym) : IConfiguracionGymRepository
{
    public async Task<ConfiguracionGym?> GetAsync() => await context.ConfiguracionesGym
        .Find(c => c.GymId == gym.GymId).FirstOrDefaultAsync();

    public Task UpsertAsync(ConfiguracionGym value)
    {
        value.GymId = gym.GymId;
        return context.ConfiguracionesGym.ReplaceOneAsync(c => c.GymId == gym.GymId, value,
            new ReplaceOptions { IsUpsert = true });
    }
}
