using GymManager.API.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GymManager.API.Data;

public sealed class MongoIndexInitializer
{
    private readonly MongoDbContext _context;

    public MongoIndexInitializer(MongoDbContext context) => _context = context;

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        var tenantDocument = new BsonDocument("GymId", new BsonDocument("$type", "string"));

        await _context.Alumnos.Indexes.CreateOneAsync(
            new CreateIndexModel<Alumno>(
                Builders<Alumno>.IndexKeys.Ascending(a => a.GymId).Ascending(a => a.DNI),
                new CreateIndexOptions<Alumno>
                {
                    Name = "ux_alumnos_gym_dni",
                    Unique = true,
                    PartialFilterExpression = new BsonDocumentFilterDefinition<Alumno>(tenantDocument)
                }), cancellationToken: cancellationToken);

        await _context.Usuarios.Indexes.CreateOneAsync(
            new CreateIndexModel<Usuario>(
                Builders<Usuario>.IndexKeys.Ascending(u => u.GymId).Ascending(u => u.EmailNormalizado),
                new CreateIndexOptions<Usuario>
                {
                    Name = "ux_usuarios_gym_email_normalizado",
                    Unique = true,
                    PartialFilterExpression = new BsonDocumentFilterDefinition<Usuario>(new BsonDocument
                    {
                        { "GymId", new BsonDocument("$type", "string") },
                        { "emailNormalizado", new BsonDocument("$type", "string") }
                    })
                }), cancellationToken: cancellationToken);

        await _context.Pagos.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Pago>(Builders<Pago>.IndexKeys.Ascending(p => p.GymId).Descending(p => p.FechaPago), new CreateIndexOptions { Name = "ix_pagos_gym_fecha" }),
            new CreateIndexModel<Pago>(Builders<Pago>.IndexKeys.Ascending(p => p.GymId).Ascending(p => p.AlumnoId).Descending(p => p.PeriodoHasta), new CreateIndexOptions { Name = "ix_pagos_gym_alumno_periodo" }),
            new CreateIndexModel<Pago>(Builders<Pago>.IndexKeys.Ascending(p => p.GymId).Ascending(p => p.SucursalId), new CreateIndexOptions { Name = "ix_pagos_gym_sucursal" })
        }, cancellationToken);

        await _context.NotificacionesWhatsApp.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<NotificacionWhatsApp>(
                Builders<NotificacionWhatsApp>.IndexKeys.Ascending(n => n.GymId).Ascending(n => n.PagoId).Ascending(n => n.Tipo),
                new CreateIndexOptions<NotificacionWhatsApp>
                {
                    Name = "ux_notificaciones_gym_pago_tipo",
                    Unique = true,
                    // Las notificaciones anteriores al cambio no tienen PagoId.
                    PartialFilterExpression = new BsonDocumentFilterDefinition<NotificacionWhatsApp>(new BsonDocument
                    {
                        { "GymId", new BsonDocument("$type", "string") },
                        { "PagoId", new BsonDocument("$type", "string") }
                    })
                }),
            new CreateIndexModel<NotificacionWhatsApp>(Builders<NotificacionWhatsApp>.IndexKeys.Ascending(n => n.GymId).Ascending(n => n.Estado).Descending(n => n.FechaCreacion), new CreateIndexOptions { Name = "ix_notificaciones_gym_estado_fecha" }),
            new CreateIndexModel<NotificacionWhatsApp>(Builders<NotificacionWhatsApp>.IndexKeys.Ascending(n => n.GymId).Ascending(n => n.AlumnoId).Ascending(n => n.Tipo).Descending(n => n.FechaCreacion), new CreateIndexOptions { Name = "ix_notificaciones_gym_alumno_tipo_fecha" })
        }, cancellationToken);
    }
}
