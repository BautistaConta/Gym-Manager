using GymManager.API.Models;
using MongoDB.Driver;
using GymManager.API.Options;
using Microsoft.Extensions.Options;

namespace GymManager.API.Data
{
    public class MongoDbContext
    {
        public IMongoCollection<Usuario> Usuarios { get; }
        public IMongoCollection<Alumno> Alumnos { get; }
        public IMongoCollection<Pago> Pagos { get; }
        public IMongoCollection<CategoriaPago> CategoriasPago { get; }
        public IMongoCollection<Sucursal> Sucursales { get; }
        public IMongoCollection<NotificacionWhatsApp> NotificacionesWhatsApp { get; }
        public IMongoCollection<ConfiguracionGym> ConfiguracionesGym { get; }
        public IMongoCollection<CampaniaWhatsApp> CampaniasWhatsApp { get; }
        public IMongoCollection<EventoAuditoria> EventosAuditoria { get; }

        public MongoDbContext(IOptions<MongoDbOptions> options)
        {
            var settings = options.Value;
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            Usuarios = database.GetCollection<Usuario>(
                settings.UsersCollectionName);
            Alumnos = database.GetCollection<Alumno>("Alumnos");
            Pagos = database.GetCollection<Pago>("Pagos");
            CategoriasPago = database.GetCollection<CategoriaPago>("CategoriasPago");
            Sucursales = database.GetCollection<Sucursal>("Sucursales");
            NotificacionesWhatsApp = database.GetCollection<NotificacionWhatsApp>("NotificacionesWhatsApp");
            ConfiguracionesGym = database.GetCollection<ConfiguracionGym>("ConfiguracionesGym");
            CampaniasWhatsApp = database.GetCollection<CampaniaWhatsApp>("CampaniasWhatsApp");
            EventosAuditoria = database.GetCollection<EventoAuditoria>("EventosAuditoria");
        }
    }
}
