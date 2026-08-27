using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.DTOs;

namespace GymManager.API.Services
{
    public class PagoService
    {
        private readonly PagoRepository _pagoRepo;
        private readonly AlumnoRepository _alumnoRepo;
        private readonly CategoriaPagoRepository _categoriaRepo;
        private readonly SucursalRepository _sucursalRepo;

        public PagoService(
            PagoRepository pagoRepo,
            AlumnoRepository alumnoRepo,
            CategoriaPagoRepository categoriaRepo,
            SucursalRepository sucursalRepo)
        {
            _pagoRepo = pagoRepo;
            _alumnoRepo = alumnoRepo;
            _categoriaRepo = categoriaRepo;
            _sucursalRepo = sucursalRepo;
        }

        public async Task<Pago> RegistrarPagoAsync(RegistrarPagoRequest request)
        {
            // 🔹 1. Validar alumno
            var alumno = await _alumnoRepo.GetByIdAsync(request.AlumnoId);
            if (alumno == null)
                throw new Exception("Alumno no encontrado");

            if (!alumno.Activo)
                throw new Exception("El alumno está inactivo");

            var sucursal = await _sucursalRepo.GetByIdAsync(request.SucursalId);
            if (sucursal is null)
                throw new DomainException("Sucursal no encontrada.");

            // 🔹 2. Validar categoría
            var categoria = await _categoriaRepo.GetByIdAsync(request.CategoriaPagoId);
            if (categoria == null || !categoria.Activa)
                throw new DomainException("La categoría no existe o está inactiva.");

            // 🔹 3. Validar descuento
            if (request.DescuentoPorcentaje < 0 || request.DescuentoPorcentaje > 100)
                throw new DomainException("El descuento debe estar entre 0 y 100.");

            // 🔹 4. Obtener último pago
            var ultimoPago = await _pagoRepo.GetUltimoPagoAsync(request.AlumnoId);

            var hoy = DateTime.UtcNow.Date;

            // 🔹 5. Calcular PeriodoDesde
            DateTime periodoDesde;

            if (ultimoPago != null)
                periodoDesde = ultimoPago.PeriodoHasta;
            else
                periodoDesde = hoy;

            // 🔹 6. Calcular PeriodoHasta
            DateTime periodoHasta;

            if (request.PeriodoHastaManual.HasValue)
            {
                periodoHasta = request.PeriodoHastaManual.Value.Date;

                if (periodoHasta <= periodoDesde)
                    throw new DomainException("La fecha manual debe ser posterior al inicio del período.");
            }
            else
            {
                periodoHasta = periodoDesde.AddMonths(categoria.MesesDuracion);
            }

            // 🔹 7. Crear pago
            var pago = new Pago
            {
                AlumnoId = request.AlumnoId,
                SucursalId = request.SucursalId,
                CategoriaPagoId = request.CategoriaPagoId,
                PrecioCategoria = categoria.Precio,
                MontoFinal = decimal.Round(categoria.Precio * (1 - request.DescuentoPorcentaje / 100), 2),
                MetodoPago = request.MetodoPago,
                DescuentoPorcentaje = request.DescuentoPorcentaje,
                FechaPago = hoy,
                PeriodoDesde = periodoDesde,
                PeriodoHasta = periodoHasta
            };

            await _pagoRepo.CreateAsync(pago);

            return pago;
        }
    }
}
