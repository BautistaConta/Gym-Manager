using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories
{
    public class AlumnoRepository
    {
        private readonly IMongoCollection<Alumno> _collection;

        public AlumnoRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<Alumno>("Alumnos");
        }

        public async Task CreateAsync(Alumno alumno)
        {
            await _collection.InsertOneAsync(alumno);
        }

        public async Task<List<Alumno>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<Alumno?> GetByIdAsync(string id)
        {
            return await _collection.Find(a => a.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Alumno?> GetByDniAsync(string dni)
        {
            return await _collection.Find(a => a.DNI == dni).FirstOrDefaultAsync();
        }

        public async Task<List<Alumno>> SearchByNombreAsync(string nombre)
        {
            return await _collection
                .Find(a => a.Nombre.ToLower().Contains(nombre.ToLower()))
                .ToListAsync();
        }

        public async Task UpdateAsync(Alumno alumno)
        {
            await _collection.ReplaceOneAsync(a => a.Id == alumno.Id, alumno);
        }
    }
}