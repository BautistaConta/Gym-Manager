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
        private readonly NotificacionService _notificacionService;
        private readonly ILogger<PagoService> _logger;

        public PagoService(
            PagoRepository pagoRepo,
            AlumnoRepository alumnoRepo,
            CategoriaPagoRepository categoriaRepo,
            SucursalRepository sucursalRepo,
            NotificacionService notificacionService,
            ILogger<PagoService> logger)
        {
            _pagoRepo = pagoRepo;
            _alumnoRepo = alumnoRepo;
            _categoriaRepo = categoriaRepo;
            _sucursalRepo = sucursalRepo;
            _notificacionService = notificacionService;
            _logger = logger;
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

            if (alumno.NotificacionesHabilitadas)
            {
                try { await _notificacionService.EncolarPagoConfirmadoAsync(alumno, pago); }
                catch (DomainException ex) { _logger.LogWarning(ex, "El pago {PagoId} fue registrado, pero no se pudo encolar su notificación.", pago.Id); }
            }

            return pago;
        }

        public async Task<List<PagoListadoResponse>> GetAllAsync()
        {
            var pagos = await _pagoRepo.GetAllAsync();
            var alumnos = (await _alumnoRepo.GetAllAsync()).ToDictionary(a => a.Id);
            var categorias = (await _categoriaRepo.GetAllAsync()).ToDictionary(c => c.Id);
            var sucursales = (await _sucursalRepo.GetAllAsync()).ToDictionary(s => s.Id);
            return pagos.Select(p => ToListadoResponse(p, alumnos.GetValueOrDefault(p.AlumnoId), sucursales.GetValueOrDefault(p.SucursalId), categorias.GetValueOrDefault(p.CategoriaPagoId))).ToList();
        }

        public async Task<NotificacionWhatsApp> EnviarRecordatorioAsync(string pagoId)
        {
            var pago = await _pagoRepo.GetByIdAsync(pagoId) ?? throw new DomainException("Pago no encontrado.");
            var alumno = await _alumnoRepo.GetByIdAsync(pago.AlumnoId) ?? throw new DomainException("Alumno no encontrado.");
            if (!alumno.Activo) throw new DomainException("No se puede enviar un recordatorio a un alumno inactivo.");
            if (await _notificacionService.ExisteEnviadaDesdeAsync(alumno.Id, TipoNotificacionWhatsApp.PorVencer, DateTime.UtcNow.AddHours(-24)) || await _notificacionService.ExisteEnviadaDesdeAsync(alumno.Id, TipoNotificacionWhatsApp.Vencido, DateTime.UtcNow.AddHours(-24)))
                throw new DomainException("Ya se envió un recordatorio a este alumno en las últimas 24 horas.");
            return await _notificacionService.EnviarRecordatorioManualAsync(alumno, pago.PeriodoHasta);
        }

        private static PagoListadoResponse ToListadoResponse(Pago pago, Alumno? alumno, Sucursal? sucursal, CategoriaPago? categoria) => new()
        {
            Id = pago.Id, AlumnoId = pago.AlumnoId, AlumnoNombre = alumno?.Nombre ?? "Alumno eliminado", AlumnoDni = alumno?.DNI ?? "-",
            SucursalId = pago.SucursalId, SucursalNombre = sucursal?.Nombre ?? "Sucursal eliminada",
            CategoriaPagoId = pago.CategoriaPagoId, CategoriaPagoNombre = categoria?.Nombre ?? "Categoría eliminada",
            FechaPago = pago.FechaPago, PeriodoDesde = pago.PeriodoDesde, PeriodoHasta = pago.PeriodoHasta,
            DescuentoPorcentaje = pago.DescuentoPorcentaje, PrecioCategoria = pago.PrecioCategoria,
            MontoFinal = pago.MontoFinal, MetodoPago = pago.MetodoPago.ToString()
        };
    }
}
