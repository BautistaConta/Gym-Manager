import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/services/alumnos_service.dart';
import '../../core/services/categorias_pago_service.dart';
import '../../core/services/pagos_service.dart';
import '../../core/services/sucursales_service.dart';
import '../../models/alumno_model.dart';
import '../../models/categoria_pago_model.dart';
import '../../models/estado_alumno.dart';
import '../../models/sucursal_model.dart';

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
    } catch (_) {
      // Los mensajes de conexión se muestran al intentar confirmar el cobro.
    }
  }

  Future<void> _buscarAlumno() async {
    final query = _busquedaController.text.trim();
    if (query.isEmpty) return;
    setState(() => _cargando = true);
    try {
      final coincidencias = await _alumnosService.search(query);
      if (coincidencias.length > 1) {
        if (mounted) setState(() => _resultados = coincidencias);
        return;
      }
      final alumno = coincidencias.isEmpty ? await _alumnosService.getByDni(query) : coincidencias.first;
      if (alumno == null) throw Exception('Alumno no encontrado.');
      await _seleccionarAlumno(alumno);
    } catch (error) {
      _mostrarError(error);
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  Future<void> _seleccionarAlumno(AlumnoModel alumno) async {
    final estado = await _alumnosService.getEstado(alumno.id);
    if (!mounted) return;
    setState(() { _alumno = alumno; _estado = estado; _resultados = []; });
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
    final descuento = double.tryParse(_descuentoController.text.replaceAll(',', '.'));
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
      setState(() {
        _estado = EstadoAlumno(
          alumnoId: _alumno!.id,
          estado: 'ACTIVO',
          fechaVencimiento: pago.periodoHasta,
        );
        _vencimientoManual = null;
      });
      await showDialog<void>(
        context: context,
        builder: (_) => AlertDialog(
          title: const Text('Pago registrado'),
          content: Text(
            'Vence: ${DateFormat('dd/MM/yyyy').format(pago.periodoHasta)}\n'
            'Importe: \$${pago.montoFinal.toStringAsFixed(2)}',
          ),
          actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Aceptar'))],
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
    final descuento = double.tryParse(_descuentoController.text.replaceAll(',', '.')) ?? 0;
    return _categoria!.precio * (1 - descuento / 100);
  }

  void _mostrarError(Object error) {
    if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(error.toString())));
  }

  @override
  Widget build(BuildContext context) {
    final vencimiento = _estado?.fechaVencimiento;
    return Scaffold(
      appBar: AppBar(title: const Text('Cobrar abono')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(
            controller: _busquedaController,
            decoration: InputDecoration(
              labelText: 'Buscar alumno por nombre o DNI',
              suffixIcon: IconButton(icon: const Icon(Icons.search), onPressed: _cargando ? null : _buscarAlumno),
            ),
            onSubmitted: (_) => _buscarAlumno(),
          ),
          if (_resultados.isNotEmpty) ...[
            const SizedBox(height: 8),
            const Text('Elegí el alumno'),
            ..._resultados.map((alumno) => Card(child: ListTile(title: Text(alumno.nombre), subtitle: Text('DNI: ${alumno.dni}'), trailing: const Icon(Icons.chevron_right), onTap: () => _seleccionarAlumno(alumno)))),
          ],
          if (_alumno != null) Card(child: ListTile(title: Text(_alumno!.nombre), subtitle: Text('DNI: ${_alumno!.dni}\n${_estado?.estado ?? ''}${vencimiento == null ? '' : ' · Vence ${DateFormat('dd/MM/yyyy').format(vencimiento)}'}'))),
          const SizedBox(height: 12),
          DropdownButtonFormField<CategoriaPagoModel>(
            value: _categoria,
            decoration: const InputDecoration(labelText: 'Categoría de pago'),
            items: _categorias.where((c) => c.activa && c.id.isNotEmpty).map((c) => DropdownMenuItem(value: c, child: Text('${c.nombre} · \$${c.precio.toStringAsFixed(2)}'))).toList(),
            onChanged: (value) => setState(() => _categoria = value),
          ),
          const SizedBox(height: 12),
          DropdownButtonFormField<SucursalModel>(value: _sucursal, decoration: const InputDecoration(labelText: 'Sucursal'), items: _sucursales.where((s) => s.id.isNotEmpty).map((s) => DropdownMenuItem(value: s, child: Text(s.nombre))).toList(), onChanged: (value) => setState(() => _sucursal = value)),
          const SizedBox(height: 12),
          DropdownButtonFormField<int>(value: _metodoPago, decoration: const InputDecoration(labelText: 'Método de pago'), items: const [DropdownMenuItem(value: 0, child: Text('Efectivo')), DropdownMenuItem(value: 1, child: Text('Transferencia')), DropdownMenuItem(value: 2, child: Text('Tarjeta'))], onChanged: (value) => setState(() => _metodoPago = value ?? 0)),
          TextField(controller: _descuentoController, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: const InputDecoration(labelText: 'Descuento (%)'), onChanged: (_) => setState(() {})),
          ListTile(title: Text(_vencimientoManual == null ? 'Vencimiento automático' : 'Vencimiento manual: ${DateFormat('dd/MM/yyyy').format(_vencimientoManual!)}'), trailing: IconButton(icon: const Icon(Icons.calendar_month), onPressed: _seleccionarVencimiento)),
          if (_montoCalculado != null) Padding(padding: const EdgeInsets.symmetric(vertical: 12), child: Text('Importe calculado: \$${_montoCalculado!.toStringAsFixed(2)}', style: Theme.of(context).textTheme.titleLarge)),
          FilledButton.icon(onPressed: _cargando ? null : _confirmarCobro, icon: _cargando ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check), label: const Text('Confirmar cobro')),
        ],
      ),
    );
  }
}
