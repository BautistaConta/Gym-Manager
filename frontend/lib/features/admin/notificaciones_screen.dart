import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/services/alumnos_service.dart';
import '../../core/services/notificaciones_service.dart';
import '../../models/alumno_model.dart';
import '../../widgets/app_ui.dart';

class NotificacionesScreen extends StatefulWidget {
  const NotificacionesScreen({super.key});

  @override
  State<NotificacionesScreen> createState() => _NotificacionesScreenState();
}

class _NotificacionesScreenState extends State<NotificacionesScreen> {
  final NotificacionesService _service = NotificacionesService();
  final AlumnosService _alumnosService = AlumnosService();
  NotificacionPage? _data;
  List<AlumnoModel> _alumnos = [];
  bool _loading = true;
  String? _error;
  int _pagina = 1;
  int? _estado;
  int? _tipo;
  String? _alumnoId;
  DateTime? _desde;
  DateTime? _hasta;

  @override
  void initState() {
    super.initState();
    _loadAlumnos();
    _load();
  }

  Future<void> _loadAlumnos() async {
    try {
      final alumnos = await _alumnosService.fetchAll();
      if (mounted) setState(() => _alumnos = alumnos);
    } catch (_) {
      // El historial sigue disponible aunque falle este selector auxiliar.
    }
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final value = await _service.getPage(
        pagina: _pagina,
        estado: _estado,
        tipo: _tipo,
        alumnoId: _alumnoId,
        desde: _desde == null
            ? null
            : DateTime(_desde!.year, _desde!.month, _desde!.day),
        hasta: _hasta == null
            ? null
            : DateTime(
                _hasta!.year,
                _hasta!.month,
                _hasta!.day,
                23,
                59,
                59,
                999,
              ),
      );
      if (mounted) setState(() => _data = value);
    } catch (exception) {
      if (mounted) setState(() => _error = exception.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pickDate({required bool isFrom}) async {
    final selected = await showDatePicker(
      context: context,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      initialDate: (isFrom ? _desde : _hasta) ?? DateTime.now(),
    );
    if (selected == null) return;
    setState(() {
      if (isFrom) {
        _desde = selected;
      } else {
        _hasta = selected;
      }
      _pagina = 1;
    });
    await _load();
  }

  Future<void> _retry(NotificacionItem item) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Reintentar notificación'),
        content: const Text(
          'El reintento puede generar un cargo y, si el resultado anterior fue '
          'ambiguo, podría duplicar el mensaje. ¿Confirmás?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Confirmar reintento'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    try {
      await _service.retry(item.id);
      await _load();
    } catch (exception) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(exception.toString())));
      }
    }
  }

  void _clearFilters() {
    setState(() {
      _estado = null;
      _tipo = null;
      _alumnoId = null;
      _desde = null;
      _hasta = null;
      _pagina = 1;
    });
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Notificaciones'),
        actions: [
          IconButton(
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(_error!),
                  FilledButton(
                    onPressed: _load,
                    child: const Text('Reintentar'),
                  ),
                ],
              ),
            )
          : AppPage(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SectionHeader(
                    icon: Icons.notifications_outlined,
                    title: 'Historial de notificaciones',
                    subtitle:
                        'Aceptada por Twilio no significa entregada. El piloto solo envía avisos antes del vencimiento.',
                  ),
                  const SizedBox(height: 16),
                  _counters(),
                  const SizedBox(height: 16),
                  _filters(),
                  const SizedBox(height: 18),
                  if (_data!.items.isEmpty)
                    const EmptyState(
                      icon: Icons.notifications_off_outlined,
                      title: 'Sin notificaciones',
                      message:
                          'No hay resultados para los filtros seleccionados.',
                    )
                  else
                    ..._data!.items.map(_notificationCard),
                  if (_data!.totalPaginas > 1) _pagination(),
                ],
              ),
            ),
    );
  }

  Widget _counters() => Wrap(
    spacing: 10,
    runSpacing: 8,
    children: [
      _counter('Pendientes', _data!.contadores['pendientes']),
      _counter('Aceptadas por Twilio', _data!.contadores['aceptadasPorTwilio']),
      _counter('Fallidas', _data!.contadores['fallidas']),
      _counter('En revisión', _data!.contadores['enRevision']),
    ],
  );

  Widget _filters() => Wrap(
    spacing: 12,
    runSpacing: 12,
    crossAxisAlignment: WrapCrossAlignment.center,
    children: [
      SizedBox(
        width: 220,
        child: DropdownButtonFormField<int?>(
          initialValue: _estado,
          decoration: const InputDecoration(labelText: 'Estado'),
          items: const [
            DropdownMenuItem(value: null, child: Text('Todos')),
            DropdownMenuItem(value: 0, child: Text('Pendiente')),
            DropdownMenuItem(value: 1, child: Text('Aceptada por Twilio')),
            DropdownMenuItem(value: 2, child: Text('Fallida')),
            DropdownMenuItem(value: 4, child: Text('En revisión')),
          ],
          onChanged: (value) {
            _estado = value;
            _pagina = 1;
            _load();
          },
        ),
      ),
      SizedBox(
        width: 220,
        child: DropdownButtonFormField<int?>(
          initialValue: _tipo,
          decoration: const InputDecoration(labelText: 'Tipo'),
          items: const [
            DropdownMenuItem(value: null, child: Text('Todos')),
            DropdownMenuItem(value: 0, child: Text('Por vencer')),
            DropdownMenuItem(value: 1, child: Text('Vencido (histórico)')),
          ],
          onChanged: (value) {
            _tipo = value;
            _pagina = 1;
            _load();
          },
        ),
      ),
      SizedBox(
        width: 260,
        child: DropdownButtonFormField<String?>(
          initialValue: _alumnoId,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'Alumno'),
          items: [
            const DropdownMenuItem(value: null, child: Text('Todos')),
            ..._alumnos.map(
              (alumno) => DropdownMenuItem(
                value: alumno.id,
                child: Text(alumno.nombre, overflow: TextOverflow.ellipsis),
              ),
            ),
          ],
          onChanged: (value) {
            _alumnoId = value;
            _pagina = 1;
            _load();
          },
        ),
      ),
      OutlinedButton.icon(
        onPressed: () => _pickDate(isFrom: true),
        icon: const Icon(Icons.date_range),
        label: Text(
          _desde == null
              ? 'Desde'
              : 'Desde ${DateFormat('dd/MM/yyyy').format(_desde!)}',
        ),
      ),
      OutlinedButton.icon(
        onPressed: () => _pickDate(isFrom: false),
        icon: const Icon(Icons.event),
        label: Text(
          _hasta == null
              ? 'Hasta'
              : 'Hasta ${DateFormat('dd/MM/yyyy').format(_hasta!)}',
        ),
      ),
      TextButton.icon(
        onPressed: _clearFilters,
        icon: const Icon(Icons.filter_alt_off),
        label: const Text('Limpiar'),
      ),
    ],
  );

  Widget _notificationCard(NotificacionItem item) {
    final date = DateFormat('dd/MM/yyyy HH:mm');
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: SurfaceCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    item.alumno,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                Text(item.estado),
              ],
            ),
            Text(
              '${item.tipo} · ${item.telefono}'
              '${item.sucursal == null ? '' : ' · ${item.sucursal}'}',
            ),
            Text(
              'Creada ${date.format(item.fechaCreacion.toLocal())} · Intentos ${item.intentos}',
            ),
            if (item.fechaEnvio != null)
              Text(
                'Aceptada por Twilio ${date.format(item.fechaEnvio!.toLocal())}',
              ),
            if (item.fechaVencimiento != null)
              Text(
                'Vencimiento ${DateFormat('dd/MM/yyyy').format(item.fechaVencimiento!)}',
              ),
            if (item.providerMessageId != null)
              SelectableText('Message SID: ${item.providerMessageId}'),
            if (item.error != null)
              Text(
                item.error!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            if (item.puedeReintentar)
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: () => _retry(item),
                  icon: const Icon(Icons.replay),
                  label: const Text('Reintentar'),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _pagination() => Row(
    mainAxisAlignment: MainAxisAlignment.center,
    children: [
      IconButton(
        onPressed: _pagina > 1
            ? () {
                _pagina--;
                _load();
              }
            : null,
        icon: const Icon(Icons.chevron_left),
      ),
      Text('Página $_pagina de ${_data!.totalPaginas}'),
      IconButton(
        onPressed: _pagina < _data!.totalPaginas
            ? () {
                _pagina++;
                _load();
              }
            : null,
        icon: const Icon(Icons.chevron_right),
      ),
    ],
  );

  Widget _counter(String label, dynamic value) =>
      Chip(label: Text('$label: ${value ?? 0}'));
}
