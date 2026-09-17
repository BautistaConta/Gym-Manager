using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GymManager.API.Migrations;

public sealed class PilotGymMigration
{
    private readonly MongoDbContext _context;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<PilotGymMigration> _logger;

    public PilotGymMigration(MongoDbContext context, IOptions<MultiTenancyOptions> options, ILogger<PilotGymMigration> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PilotGymId))
            throw new InvalidOperationException("MultiTenancy:PilotGymId no está configurado.");

        var results = new Dictionary<string, long>
        {
            ["Usuarios"] = await BackfillAsync(_context.Usuarios, cancellationToken),
            ["Alumnos"] = await BackfillAsync(_context.Alumnos, cancellationToken),
            ["Sucursales"] = await BackfillAsync(_context.Sucursales, cancellationToken),
            ["CategoriasPago"] = await BackfillAsync(_context.CategoriasPago, cancellationToken),
            ["Pagos"] = await BackfillAsync(_context.Pagos, cancellationToken),
            ["NotificacionesWhatsApp"] = await BackfillAsync(_context.NotificacionesWhatsApp, cancellationToken)
        };

        var emailFilter = Builders<Usuario>.Filter.And(
            Builders<Usuario>.Filter.Eq(u => u.GymId, _options.PilotGymId),
            Builders<Usuario>.Filter.Or(
                Builders<Usuario>.Filter.Exists(u => u.EmailNormalizado, false),
                Builders<Usuario>.Filter.Eq(u => u.EmailNormalizado, null),
                Builders<Usuario>.Filter.Eq(u => u.EmailNormalizado, string.Empty)));
        var emailPipeline = new EmptyPipelineDefinition<Usuario>()
            .AppendStage<Usuario, Usuario, Usuario>(new BsonDocument("$set", new BsonDocument(
                "emailNormalizado", new BsonDocument("$toLower", new BsonDocument("$trim", new BsonDocument("input", "$email"))))));
        var normalized = await _context.Usuarios.UpdateManyAsync(emailFilter, emailPipeline, cancellationToken: cancellationToken);

        var consentFilter = new BsonDocumentFilterDefinition<Alumno>(new BsonDocument
        {
            { "FechaConsentimientoNotificacionesUtc", new BsonDocument("$exists", true) },
            { "FechaConsentimientoWhatsApp", new BsonDocument("$exists", false) }
        });
        var consentRename = await _context.Alumnos.UpdateManyAsync(consentFilter,
            new BsonDocumentUpdateDefinition<Alumno>(new BsonDocument("$rename",
                new BsonDocument("FechaConsentimientoNotificacionesUtc", "FechaConsentimientoWhatsApp"))),
            cancellationToken: cancellationToken);

        foreach (var result in results)
            _logger.LogInformation("Migración PilotGymId: {Collection} actualizó {Count} documentos.", result.Key, result.Value);
        _logger.LogInformation("Migración PilotGymId: normalizó {Count} emails.", normalized.ModifiedCount);
        _logger.LogInformation("Migración consentimiento WhatsApp: renombró {Count} fechas históricas.", consentRename.ModifiedCount);
    }

    private async Task<long> BackfillAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken) where T : IGymOwned
    {
        var missingGym = Builders<T>.Filter.Or(
            Builders<T>.Filter.Exists(x => x.GymId, false),
            Builders<T>.Filter.Eq(x => x.GymId, null),
            Builders<T>.Filter.Eq(x => x.GymId, string.Empty));
        var result = await collection.UpdateManyAsync(
            missingGym,
            Builders<T>.Update.Set(x => x.GymId, _options.PilotGymId),
            cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }
}
