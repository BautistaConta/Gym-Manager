using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Data
{
    public class MongoDbContext
    {
        public IMongoCollection<Usuario> Usuarios { get; }
        public IMongoCollection<Alumno> Alumnos { get; }
        public IMongoCollection<Pago> Pagos { get; }
        public IMongoCollection<CategoriaPago> CategoriasPago { get; }
        public IMongoCollection<Sucursal> Sucursales { get; }

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB:ConnectionString"];
            var databaseName = configuration["MongoDB:DatabaseName"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("MongoDB connection string no configurada");

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("MongoDB database name no configurado");

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            Usuarios = database.GetCollection<Usuario>(
                configuration["MongoDB:UsersCollectionName"] ?? "Usuarios");
            Alumnos = database.GetCollection<Alumno>("Alumnos");
            Pagos = database.GetCollection<Pago>("Pagos");
            CategoriasPago = database.GetCollection<CategoriaPago>("CategoriasPago");
            Sucursales = database.GetCollection<Sucursal>("Sucursales");
        }
    }
}
