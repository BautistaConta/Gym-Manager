using GymManager.API.Data;
using GymManager.API.DTOs;
using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public interface IPilotEventRepository
{
    Task InsertAsync(PilotEvent value, CancellationToken cancellationToken = default);
    Task<List<PilotEventCount>> SummaryAsync(
        string gymId,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);
}

public sealed class PilotEventRepository(MongoDbContext context) : IPilotEventRepository
{
    public Task InsertAsync(PilotEvent value, CancellationToken cancellationToken = default) =>
        context.PilotEvents.InsertOneAsync(value, cancellationToken: cancellationToken);

    public async Task<List<PilotEventCount>> SummaryAsync(
        string gymId,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
    {
        var filter = TenantFilters.ForGym<PilotEvent>(gymId) &
            Builders<PilotEvent>.Filter.Gte(e => e.FechaUtc, desdeUtc) &
            Builders<PilotEvent>.Filter.Lt(e => e.FechaUtc, hastaUtc);
        var values = await context.PilotEvents.Find(filter)
            .Project(e => new { e.FechaUtc, e.Tipo })
            .ToListAsync(cancellationToken);
        return values
            .GroupBy(e => new { Dia = e.FechaUtc.Date, e.Tipo })
            .Select(group => new PilotEventCount(group.Key.Dia, group.Key.Tipo, group.LongCount()))
            .OrderBy(item => item.DiaUtc)
            .ThenBy(item => item.Tipo)
            .ToList();
    }
}
