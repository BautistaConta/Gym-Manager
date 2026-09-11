using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public class AlumnoRepository
{
    private readonly IMongoCollection<Alumno> _collection;
    private readonly IGymContext _gymContext;

    public AlumnoRepository(MongoDbContext context, IGymContext gymContext)
    {
        _collection = context.Alumnos;
        _gymContext = gymContext;
    }

    public Task CreateAsync(Alumno alumno)
    {
        TenantFilters.Stamp(alumno, _gymContext.GymId);
        return _collection.InsertOneAsync(alumno);
    }

    public Task<List<Alumno>> GetAllAsync() =>
        _collection.Find(TenantFilters.ForGym<Alumno>(_gymContext.GymId)).ToListAsync();

    public async Task<Alumno?> GetByIdAsync(string id) => await _collection.Find(ById(id)).FirstOrDefaultAsync();

    public async Task<Alumno?> GetByDniAsync(string dni) =>
        await _collection.Find(TenantFilters.And<Alumno>(_gymContext.GymId, Builders<Alumno>.Filter.Eq(a => a.DNI, dni))).FirstOrDefaultAsync();

    public Task<List<Alumno>> SearchByNombreAsync(string nombre)
    {
        var escaped = System.Text.RegularExpressions.Regex.Escape(nombre);
        var filter = Builders<Alumno>.Filter.Regex(a => a.Nombre, new BsonRegularExpression(escaped, "i"));
        return _collection.Find(TenantFilters.And<Alumno>(_gymContext.GymId, filter)).ToListAsync();
    }

    public Task UpdateAsync(Alumno alumno)
    {
        TenantFilters.EnsureOwned(alumno, _gymContext.GymId);
        return _collection.ReplaceOneAsync(ById(alumno.Id), alumno);
    }

    private FilterDefinition<Alumno> ById(string id) =>
        TenantFilters.And<Alumno>(_gymContext.GymId, Builders<Alumno>.Filter.Eq(a => a.Id, id));
}
