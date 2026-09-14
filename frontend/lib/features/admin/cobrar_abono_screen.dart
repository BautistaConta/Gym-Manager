import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/services/alumnos_service.dart';
import '../../core/services/categorias_pago_service.dart';
import '../../core/services/pagos_service.dart';
import '../../core/services/sucursales_service.dart';
import '../../core/theme/app_theme.dart';
import '../../models/alumno_model.dart';
import '../../models/categoria_pago_model.dart';
import '../../models/estado_alumno.dart';
import '../../models/sucursal_model.dart';
import '../../widgets/app_ui.dart';

class CobrarAbonoScreen extends StatefulWidget {
  const CobrarAbonoScreen({super.key});

  @override
  State<CobrarAbonoScreen> createState() => _CobrarAbonoScreenState();
}

class _CobrarAbonoScreenState extends State<CobrarAbonoScreen> {
  final _alumnosService = AlumnosService();
  final _categoriasService = CategoriasPagoService();
  final _sucursalesService = SucursalesService();
  final _pagosService = PagosService();
  final _busquedaController = TextEditingController();
  final _descuentoController = TextEditingController(text: '0');

  List<CategoriaPagoModel> _categorias = [];
  List<SucursalModel> _sucursales = [];
  List<AlumnoModel> _resultados = [];
  AlumnoModel? _alumno;
  EstadoAlumno? _estado;
  CategoriaPagoModel? _categoria;
  SucursalModel? _sucursal;
  DateTime? _vencimientoManual;
  int _metodoPago = 0;
  bool _cargando = false;
  bool _buscando = false;
  bool _busquedaRealizada = false;

  @override
  void initState() {
    super.initState();
    _cargarOpciones();
  }

  @override
  void dispose() {
    _busquedaController.dispose();
    _descuentoController.dispose();
    super.dispose();
  }

  Future<void> _cargarOpciones() async {
    try {
      final values = await Future.wait([
        _categoriasService.fetchAll(),
        _sucursalesService.fetchAll(),
      ]);
      if (!mounted) return;
      setState(() {
        _categorias = values[0] as List<CategoriaPagoModel>;
        _sucursales = values[1] as List<SucursalModel>;
      });
    } catch (_) {}
  }

  Future<void> _buscarAlumno() async {
    final query = _busquedaController.text.trim();
    if (query.isEmpty) {
      _mostrarError('Ingresá un nombre o DNI para buscar.');
      return;
    }
    setState(() {
      _buscando = true;
      _busquedaRealizada = false;
      _resultados = [];
    });
    try {
      var coincidencias = await _alumnosService.search(query);
      if (coincidencias.isEmpty) {
        final alumno = await _alumnosService.getByDni(query);
        if (alumno != null) coincidencias = [alumno];
      }
      if (!mounted) return;
      setState(() {
        _resultados = coincidencias;
        _busquedaRealizada = true;
      });
    } catch (error) {
      if (mounted) setState(() => _busquedaRealizada = true);
      _mostrarError(error);
    } finally {
      if (mounted) setState(() => _buscando = false);
    }
  }

  Future<void> _seleccionarAlumno(AlumnoModel alumno) async {
    setState(() {
      _alumno = alumno;
      _estado = null;
    });
    try {
      final estado = await _alumnosService.getEstado(alumno.id);
      if (mounted && _alumno?.id == alumno.id) setState(() => _estado = estado);
    } catch (_) {
      // La selección no depende de la consulta complementaria del estado.
    }
  }

  Future<void> _seleccionarVencimiento() async {
    final fecha = await showDatePicker(
      context: context,
      initialDate: _vencimientoManual ?? DateTime.now(),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 3650)),
    );
    if (fecha != null && mounted) setState(() => _vencimientoManual = fecha);
  }

  Future<void> _confirmarCobro() async {
    if (_alumno == null || _categoria == null || _sucursal == null) {
      _mostrarError('Seleccioná alumno, categoría y sucursal.');
      return;
    }
    final descuento = double.tryParse(
      _descuentoController.text.replaceAll(',', '.'),
    );
    if (descuento == null || descuento < 0 || descuento > 100) {
      _mostrarError('Ingresá un descuento entre 0 y 100.');
      return;
    }
    setState(() => _cargando = true);
    try {
      final pago = await _pagosService.registrar(
        alumnoId: _alumno!.id,
        sucursalId: _sucursal!.id,
        categoriaPagoId: _categoria!.id,
        metodoPago: _metodoPago,
        descuentoPorcentaje: descuento,
        periodoHastaManual: _vencimientoManual,
      );
      if (!mounted) return;
      EstadoAlumno? estadoActualizado;
      try {
        estadoActualizado = await _alumnosService.getEstado(_alumno!.id);
      } catch (_) {
        // El cobro ya se registró: un fallo al refrescar el estado no debe
        // presentarse como un fallo de pago ni incentivar un segundo cobro.
      }
      if (!mounted) return;
      setState(() {
        _estado = estadoActualizado;
        _vencimientoManual = null;
      });
      await showDialog<void>(
        context: context,
        builder: (context) => AlertDialog(
          icon: const Icon(
            Icons.check_circle_rounded,
            color: AppTheme.primaryGreen,
            size: 46,
          ),
          title: const Text('Cobro registrado'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                _alumno!.nombre,
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 12),
              SurfaceCard(
                color: AppTheme.surfaceHigh,
                child: Column(
                  children: [
                    _ResumenFila(
                      label: 'Importe',
                      value: '\$${pago.montoFinal.toStringAsFixed(2)}',
                    ),
                    const SizedBox(height: 8),
                    _ResumenFila(
                      label: 'Vencimiento',
                      value: DateFormat('dd/MM/yyyy').format(pago.periodoHasta),
                    ),
                  ],
                ),
              ),
            ],
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Listo'),
            ),
          ],
        ),
      );
    } catch (error) {
      _mostrarError(error);
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  double? get _montoCalculado {
    if (_categoria == null) return null;
    final descuento =
        double.tryParse(_descuentoController.text.replaceAll(',', '.')) ?? 0;
    return _categoria!.precio * (1 - descuento.clamp(0, 100) / 100);
  }

  void _mostrarError(Object error) {
    if (mounted)
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(error.toString().replaceFirst('Exception: ', '')),
        ),
      );
  }

  @override
  Widget build(BuildContext context) {
    final vencimiento = _estado?.fechaVencimiento;
    return Scaffold(
      appBar: AppBar(title: const Text('Cobrar abono')),
      body: AppPage(
        maxWidth: 920,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SectionHeader(
              icon: Icons.point_of_sale_rounded,
              title: 'Nuevo cobro',
              subtitle: 'Buscá al alumno y completá los datos del abono',
            ),
            const SizedBox(height: 24),
            SurfaceCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _StepTitle(
                    number: '1',
                    title: 'Seleccioná un alumno',
                    subtitle: 'Buscá por nombre completo o número de DNI',
                  ),
                  const SizedBox(height: 18),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _busquedaController,
                          textInputAction: TextInputAction.search,
                          decoration: InputDecoration(
                            labelText: 'Nombre o DNI',
                            hintText: 'Ej. Ana Pérez o 32123456',
                            prefixIcon: const Icon(Icons.search),
                            suffixIcon: _busquedaController.text.isEmpty
                                ? null
                                : IconButton(
                                    onPressed: () => setState(() {
                                      _busquedaController.clear();
                                      _resultados = [];
                                      _busquedaRealizada = false;
                                    }),
                                    icon: const Icon(Icons.close),
                                  ),
                          ),
                          onChanged: (_) => setState(() {}),
                          onSubmitted: (_) => _buscarAlumno(),
                        ),
                      ),
                      const SizedBox(width: 12),
                      SizedBox(
                        height: 58,
                        child: FilledButton.icon(
                          onPressed: _buscando ? null : _buscarAlumno,
                          icon: _buscando
                              ? const SizedBox.square(
                                  dimension: 17,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Icon(Icons.search),
                          label: const Text('Buscar'),
                        ),
                      ),
                    ],
                  ),
                  if (_buscando) ...[
                    const SizedBox(height: 18),
                    const LinearProgressIndicator(minHeight: 2),
                  ],
                  if (_busquedaRealizada && _resultados.isEmpty && !_buscando)
                    const EmptyState(
                      icon: Icons.person_search_outlined,
                      title: 'Sin coincidencias',
                      message:
                          'Probá con otro nombre o verificá el DNI ingresado.',
                    ),
                  if (_resultados.isNotEmpty) ...[
                    const SizedBox(height: 20),
                    Text(
                      '${_resultados.length} ${_resultados.length == 1 ? 'resultado' : 'resultados'} · Tocá una opción para elegirla',
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                    const SizedBox(height: 10),
                    ..._resultados.map(
                      (alumno) => Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: _AlumnoOption(
                          alumno: alumno,
                          selected: _alumno?.id == alumno.id,
                          onTap: () => _seleccionarAlumno(alumno),
                        ),
                      ),
                    ),
                  ],
                  if (_alumno != null) ...[
                    const SizedBox(height: 8),
                    SurfaceCard(
                      color: AppTheme.primaryGreen.withValues(alpha: .07),
                      borderColor: AppTheme.primaryGreen.withValues(alpha: .45),
                      child: Row(
                        children: [
                          const CircleAvatar(
                            backgroundColor: AppTheme.primaryGreen,
                            foregroundColor: AppTheme.primaryDark,
                            child: Icon(Icons.person_rounded),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _alumno!.nombre,
                                  style: Theme.of(
                                    context,
                                  ).textTheme.titleMedium,
                                ),
                                const SizedBox(height: 3),
                                Text(
                                  'DNI ${_alumno!.dni}${vencimiento == null ? '' : ' · Vence ${DateFormat('dd/MM/yyyy').format(vencimiento)}'}',
                                  style: Theme.of(context).textTheme.bodySmall,
                                ),
                              ],
                            ),
                          ),
                          StatusBadge(
                            label: _estado?.estado ?? 'Seleccionado',
                            color: _estado?.estado == 'VENCIDA'
                                ? AppTheme.danger
                                : AppTheme.primaryGreen,
                            icon: Icons.check,
                          ),
                        ],
                      ),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 18),
            SurfaceCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const _StepTitle(
                    number: '2',
                    title: 'Datos del abono',
                    subtitle: 'Definí el plan, la sede y la forma de pago',
                  ),
                  const SizedBox(height: 20),
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final wide = constraints.maxWidth >= 620;
                      final width = wide
                          ? (constraints.maxWidth - 14) / 2
                          : constraints.maxWidth;
                      return Wrap(
                        spacing: 14,
                        runSpacing: 16,
                        children: [
                          SizedBox(
                            width: width,
                            child: DropdownMenu<CategoriaPagoModel>(
                              initialSelection: _categoria,
                              expandedInsets: EdgeInsets.zero,
                              menuHeight: 320,
                              label: const Text('Categoría de pago'),
                              leadingIcon: const Icon(
                                Icons.card_membership_outlined,
                              ),
                              dropdownMenuEntries: _categorias
                                  .where((c) => c.activa && c.id.isNotEmpty)
                                  .map(
                                    (c) => DropdownMenuEntry(
                                      value: c,
                                      label:
                                          '${c.nombre} · \$${c.precio.toStringAsFixed(2)}',
                                    ),
                                  )
                                  .toList(),
                              onSelected: (value) =>
                                  setState(() => _categoria = value),
                            ),
                          ),
                          SizedBox(
                            width: width,
                            child: DropdownMenu<SucursalModel>(
                              initialSelection: _sucursal,
                              expandedInsets: EdgeInsets.zero,
                              menuHeight: 320,
                              label: const Text('Sucursal'),
                              leadingIcon: const Icon(Icons.store_outlined),
                              dropdownMenuEntries: _sucursales
                                  .where((s) => s.id.isNotEmpty)
                                  .map(
                                    (s) => DropdownMenuEntry(
                                      value: s,
                                      label: s.nombre,
                                    ),
                                  )
                                  .toList(),
                              onSelected: (value) =>
                                  setState(() => _sucursal = value),
                            ),
                          ),
                          SizedBox(
                            width: width,
                            child: DropdownMenu<int>(
                              initialSelection: _metodoPago,
                              expandedInsets: EdgeInsets.zero,
                              label: const Text('Método de pago'),
                              leadingIcon: const Icon(Icons.payments_outlined),
                              dropdownMenuEntries: const [
                                DropdownMenuEntry(value: 0, label: 'Efectivo'),
                                DropdownMenuEntry(
                                  value: 1,
                                  label: 'Transferencia',
                                ),
                                DropdownMenuEntry(value: 2, label: 'Tarjeta'),
                              ],
                              onSelected: (value) =>
                                  setState(() => _metodoPago = value ?? 0),
                            ),
                          ),
                          SizedBox(
                            width: width,
                            child: TextField(
                              controller: _descuentoController,
                              keyboardType:
                                  const TextInputType.numberWithOptions(
                                    decimal: true,
                                  ),
                              decoration: const InputDecoration(
                                labelText: 'Descuento',
                                hintText: '0',
                                suffixText: '%',
                                prefixIcon: Icon(Icons.percent_rounded),
                              ),
                              onChanged: (_) => setState(() {}),
                            ),
                          ),
                        ],
                      );
                    },
                  ),
                  const SizedBox(height: 16),
                  InkWell(
                    borderRadius: BorderRadius.circular(14),
                    onTap: _seleccionarVencimiento,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 14,
                      ),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceHigh,
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(color: AppTheme.border),
                      ),
                      child: Row(
                        children: [
                          const Icon(
                            Icons.event_outlined,
                            color: AppTheme.textSecondary,
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _vencimientoManual == null
                                      ? 'Vencimiento automático'
                                      : DateFormat(
                                          'dd/MM/yyyy',
                                        ).format(_vencimientoManual!),
                                  style: Theme.of(
                                    context,
                                  ).textTheme.titleMedium,
                                ),
                                Text(
                                  _vencimientoManual == null
                                      ? 'Se calculará según la duración del plan'
                                      : 'Fecha personalizada seleccionada',
                                  style: Theme.of(context).textTheme.bodySmall,
                                ),
                              ],
                            ),
                          ),
                          if (_vencimientoManual != null)
                            IconButton(
                              tooltip: 'Usar vencimiento automático',
                              onPressed: () =>
                                  setState(() => _vencimientoManual = null),
                              icon: const Icon(Icons.close),
                            )
                          else
                            const Icon(Icons.chevron_right),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 18),
            SurfaceCard(
              color: AppTheme.surfaceHigh,
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Total a cobrar',
                          style: Theme.of(context).textTheme.bodyMedium
                              ?.copyWith(color: AppTheme.textSecondary),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          _montoCalculado == null
                              ? '—'
                              : '\$${_montoCalculado!.toStringAsFixed(2)}',
                          style: Theme.of(context).textTheme.displaySmall
                              ?.copyWith(color: AppTheme.primaryGreen),
                        ),
                      ],
                    ),
                  ),
                  SizedBox(
                    height: 54,
                    child: FilledButton.icon(
                      onPressed: _cargando ? null : _confirmarCobro,
                      icon: _cargando
                          ? const SizedBox.square(
                              dimension: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.check_circle_outline),
                      label: Text(
                        _cargando ? 'Procesando...' : 'Confirmar cobro',
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AlumnoOption extends StatelessWidget {
  const _AlumnoOption({
    required this.alumno,
    required this.selected,
    required this.onTap,
  });
  final AlumnoModel alumno;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => MouseRegion(
    cursor: SystemMouseCursors.click,
    child: Material(
      color: selected
          ? AppTheme.primaryGreen.withValues(alpha: .10)
          : AppTheme.surfaceHigh,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(
          color: selected ? AppTheme.primaryGreen : AppTheme.border,
          width: selected ? 1.5 : 1,
        ),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: selected
                    ? AppTheme.primaryGreen
                    : AppTheme.border,
                foregroundColor: selected
                    ? AppTheme.primaryDark
                    : AppTheme.textPrimary,
                child: Text(
                  alumno.nombre.isEmpty ? '?' : alumno.nombre[0].toUpperCase(),
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
              const SizedBox(width: 13),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      alumno.nombre,
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 3),
                    Text(
                      'DNI ${alumno.dni}${alumno.telefono.isEmpty ? '' : ' · ${alumno.telefono}'}',
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Icon(
                selected
                    ? Icons.check_circle_rounded
                    : Icons.arrow_forward_rounded,
                color: selected
                    ? AppTheme.primaryGreen
                    : AppTheme.textSecondary,
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class _StepTitle extends StatelessWidget {
  const _StepTitle({
    required this.number,
    required this.title,
    required this.subtitle,
  });
  final String number;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Container(
        width: 32,
        height: 32,
        alignment: Alignment.center,
        decoration: const BoxDecoration(
          color: AppTheme.primaryGreen,
          shape: BoxShape.circle,
        ),
        child: Text(
          number,
          style: const TextStyle(
            color: AppTheme.primaryDark,
            fontWeight: FontWeight.w900,
          ),
        ),
      ),
      const SizedBox(width: 12),
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleMedium),
            Text(subtitle, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ),
    ],
  );
}

class _ResumenFila extends StatelessWidget {
  const _ResumenFila({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Text(label, style: Theme.of(context).textTheme.bodySmall),
      const Spacer(),
      Text(value, style: Theme.of(context).textTheme.titleMedium),
    ],
  );
}
