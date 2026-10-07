using GymManager.API.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;

namespace GymManager.API.Health;

public sealed class MongoHealthCheck(MongoDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await context.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB disponible.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("MongoDB no disponible.");
        }
    }
}
