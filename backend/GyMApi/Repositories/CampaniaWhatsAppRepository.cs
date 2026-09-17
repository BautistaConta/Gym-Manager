using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public sealed class CampaniaWhatsAppRepository(MongoDbContext context, IGymContext gym)
{
    private FilterDefinition<CampaniaWhatsApp> ById(string id) => TenantFilters.And<CampaniaWhatsApp>(gym.GymId,
        Builders<CampaniaWhatsApp>.Filter.Eq(c => c.Id, id));
    public Task CreateAsync(CampaniaWhatsApp value) { TenantFilters.Stamp(value, gym.GymId); return context.CampaniasWhatsApp.InsertOneAsync(value); }
    public Task<List<CampaniaWhatsApp>> GetAllAsync() => context.CampaniasWhatsApp
        .Find(TenantFilters.ForGym<CampaniaWhatsApp>(gym.GymId)).SortByDescending(c => c.FechaCreacion).ToListAsync();
    public async Task<CampaniaWhatsApp?> GetByIdAsync(string id) => await context.CampaniasWhatsApp.Find(ById(id)).FirstOrDefaultAsync();
    public Task UpdateAsync(CampaniaWhatsApp value)
    {
        TenantFilters.EnsureOwned(value, gym.GymId);
        return context.CampaniasWhatsApp.ReplaceOneAsync(ById(value.Id), value);
    }
}
