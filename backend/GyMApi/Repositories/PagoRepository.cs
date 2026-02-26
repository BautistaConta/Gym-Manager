using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories
{
    public class PagoRepository
    {
        private readonly IMongoCollection<Pago> _collection;

        public PagoRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<Pago>("Pagos");
        }

        public async Task CreateAsync(Pago pago)
        {
            await _collection.InsertOneAsync(pago);
        }

        public async Task<List<Pago>> GetByAlumnoIdAsync(string alumnoId)
        {
            return await _collection
                .Find(p => p.AlumnoId == alumnoId)
                .SortByDescending(p => p.FechaPago)
                .ToListAsync();
        }

        // 🔥 ESTE ES EL MÁS IMPORTANTE
        public async Task<Pago?> GetUltimoPagoAsync(string alumnoId)
        {
            return await _collection
                .Find(p => p.AlumnoId == alumnoId)
                .SortByDescending(p => p.PeriodoHasta)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Pago>> GetBySucursalIdAsync(string sucursalId)
        {
            return await _collection
                .Find(p => p.SucursalId == sucursalId)
                .ToListAsync();
        }

        public async Task<List<Pago>> GetByRangoFechasAsync(DateTime desde, DateTime hasta)
        {
            return await _collection
                .Find(p => p.FechaPago >= desde && p.FechaPago <= hasta)
                .ToListAsync();
        }
    }
}