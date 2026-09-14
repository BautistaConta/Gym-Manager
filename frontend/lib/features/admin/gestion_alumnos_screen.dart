import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/services/alumnos_service.dart';
import '../../core/services/sucursales_service.dart';
import '../../core/theme/app_theme.dart';
import '../../models/alumno_model.dart';
import '../../models/sucursal_model.dart';
import '../../widgets/app_ui.dart';
import '../../widgets/modals/create_alumno_modal.dart';
import '../../widgets/modals/edit_alumno_modal.dart';
import '../../widgets/modals/edit_consentimiento_modal.dart';

class GestionAlumnosScreen extends StatefulWidget {
  const GestionAlumnosScreen({super.key});
  @override
  State<GestionAlumnosScreen> createState() => _GestionAlumnosScreenState();
}

class _GestionAlumnosScreenState extends State<GestionAlumnosScreen> {
  final AlumnosService _service = AlumnosService();
  final SucursalesService _sucursalesService = SucursalesService();
  final TextEditingController _searchController = TextEditingController();
  bool loading = true;
  String? error;
  List<AlumnoModel> alumnos = [];
  List<SucursalModel> sucursales = [];
  String? _estadoFiltro;
  String? _sucursalFiltro;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final data = await _service.fetchAll();
      final sedes = await _sucursalesService.fetchAll();
      if (!mounted) return;
      setState(() {
        alumnos = data;
        sucursales = sedes;
      });
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _desactivarAlumno(AlumnoModel alumno) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Desactivar alumno'),
        content: Text('¿Querés desactivar a ${alumno.nombre}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Desactivar'),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await _service.deactivateAlumno(alumno.id);
      await _load();
    } catch (e) {
      _showError(e);
    }
  }

  Future<void> _editAlumno(AlumnoModel alumno) async {
    final resultado = await showDialog<AlumnoEdicion>(
      context: context,
      builder: (_) => EditAlumnoModal(alumno: alumno, sucursales: sucursales),
    );
    if (resultado == null || !mounted) return;
    try {
      await _service.updateAlumno(
        alumno.id,
        nombre: resultado.nombre,
        telefono: resultado.telefono,
        activo: resultado.activo,
        sucursalPrincipalId: resultado.sucursalPrincipalId,
      );
      await _load();
    } catch (e) {
      _showError(e);
    }
  }

  Future<void> _editNotificaciones(AlumnoModel alumno) async {
    final resultado = await showDialog<ConsentimientoEdicion>(
      context: context,
      builder: (_) => EditConsentimientoModal(alumno: alumno),
    );
    if (resultado == null || !mounted) return;
    try {
      await _service.updateNotificaciones(
        alumno.id,
        habilitadas: resultado.habilitadas,
        consentimientoConfirmado: resultado.confirmado,
        medioConsentimiento: resultado.medio,
      );
      await _load();
    } catch (e) {
      _showError(e);
    }
  }

  void _showError(Object e) {
    if (mounted)
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(e.toString())));
  }

  @override
  Widget build(BuildContext context) {
    final query = _searchController.text.trim().toLowerCase();
    final filtered = alumnos
        .where(
          (a) =>
              (a.nombre.toLowerCase().contains(query) ||
                  a.dni.contains(query)) &&
              (_estadoFiltro == null || a.estado == _estadoFiltro) &&
              (_sucursalFiltro == null ||
                  (_sucursalFiltro == '__sin_asignar__'
                      ? a.sucursalPrincipalId == null
                      : a.sucursalPrincipalId == _sucursalFiltro)),
        )
        .toList();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Alumnos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Actualizar',
            onPressed: loading ? null : _load,
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          if (await showDialog<bool>(
                context: context,
                builder: (_) => CreateAlumnoModal(sucursales: sucursales),
              ) ==
              true)
            _load();
        },
        icon: const Icon(Icons.person_add_alt_1),
        label: const Text('Nuevo alumno'),
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? Center(
              child: Text(
                error!,
                style: const TextStyle(color: AppTheme.danger),
              ),
            )
          : AppPage(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SectionHeader(
                    icon: Icons.groups_2_outlined,
                    title: 'Gestión de alumnos',
                    subtitle: '${alumnos.length} alumnos registrados',
                  ),
                  const SizedBox(height: 20),
                  TextField(
                    controller: _searchController,
                    onChanged: (_) => setState(() {}),
                    decoration: InputDecoration(
                      labelText: 'Buscar alumno',
                      hintText: 'Nombre o DNI',
                      prefixIcon: const Icon(Icons.search),
                      suffixIcon: query.isEmpty
                          ? null
                          : IconButton(
                              onPressed: () =>
                                  setState(_searchController.clear),
                              icon: const Icon(Icons.close),
                            ),
                    ),
                  ),
                  const SizedBox(height: 18),
                  Wrap(
                    spacing: 12,
                    runSpacing: 8,
                    children: [
                      SizedBox(
                        width: 230,
                        child: DropdownButtonFormField<String>(
                          initialValue: _estadoFiltro ?? '',
                          decoration: const InputDecoration(
                            labelText: 'Estado de cuota',
                          ),
                          items: const [
                            DropdownMenuItem(value: '', child: Text('Todos')),
                            DropdownMenuItem(
                              value: 'SIN_PAGOS',
                              child: Text('Sin pagos'),
                            ),
                            DropdownMenuItem(
                              value: 'VENCIDA',
                              child: Text('Vencida'),
                            ),
                            DropdownMenuItem(
                              value: 'PROXIMO_A_VENCER',
                              child: Text('Próximo a vencer'),
                            ),
                            DropdownMenuItem(
                              value: 'AL_DIA',
                              child: Text('Al día'),
                            ),
                          ],
                          onChanged: (value) => setState(
                            () => _estadoFiltro = value == '' ? null : value,
                          ),
                        ),
                      ),
                      SizedBox(
                        width: 230,
                        child: DropdownButtonFormField<String>(
                          initialValue: _sucursalFiltro ?? '',
                          decoration: const InputDecoration(
                            labelText: 'Sucursal principal',
                          ),
                          items: [
                            const DropdownMenuItem(
                              value: '',
                              child: Text('Todas'),
                            ),
                            const DropdownMenuItem(
                              value: '__sin_asignar__',
                              child: Text('Sin asignar'),
                            ),
                            ...sucursales.map(
                              (s) => DropdownMenuItem(
                                value: s.id,
                                child: Text(s.nombre),
                              ),
                            ),
                          ],
                          onChanged: (value) => setState(
                            () => _sucursalFiltro = value == '' ? null : value,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 18),
                  if (filtered.isEmpty)
                    EmptyState(
                      icon: Icons.person_search_outlined,
                      title: query.isEmpty
                          ? 'No hay alumnos'
                          : 'Sin resultados',
                      message: query.isEmpty
                          ? 'Creá el primer alumno para comenzar.'
                          : 'Probá con otro nombre o DNI.',
                    )
                  else
                    ...filtered.map((a) {
                      final stateColor = a.estado == 'AL_DIA'
                          ? AppTheme.primaryGreen
                          : a.estado == 'VENCIDA'
                          ? AppTheme.danger
                          : a.estado == 'PROXIMO_A_VENCER'
                          ? AppTheme.warning
                          : AppTheme.textSecondary;
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: SurfaceCard(
                          child: LayoutBuilder(
                            builder: (context, constraints) {
                              final details = Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      CircleAvatar(
                                        backgroundColor: AppTheme.surfaceHigh,
                                        foregroundColor: AppTheme.primaryGreen,
                                        child: Text(
                                          a.nombre.isEmpty
                                              ? '?'
                                              : a.nombre[0].toUpperCase(),
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w800,
                                          ),
                                        ),
                                      ),
                                      const SizedBox(width: 12),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Text(
                                              a.nombre,
                                              style: Theme.of(
                                                context,
                                              ).textTheme.titleMedium,
                                            ),
                                            Text(
                                              'DNI ${a.dni}',
                                              style: Theme.of(
                                                context,
                                              ).textTheme.bodySmall,
                                            ),
                                          ],
                                        ),
                                      ),
                                      StatusBadge(
                                        label:
                                            a.estado ??
                                            (a.activo ? 'Activo' : 'Inactivo'),
                                        color: stateColor,
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 16),
                                  InfoRow(
                                    icon: Icons.phone_outlined,
                                    label: 'Teléfono',
                                    value: a.telefono.isEmpty
                                        ? 'Sin teléfono'
                                        : a.telefono,
                                  ),
                                  InfoRow(
                                    icon: Icons.chat_outlined,
                                    label: 'WhatsApp',
                                    value:
                                        a.notificacionesHabilitadas &&
                                            a.fechaConsentimientoNotificacionesUtc !=
                                                null
                                        ? 'Consentimiento registrado'
                                        : 'Sin consentimiento registrado',
                                  ),
                                  if (a.fechaVencimiento != null)
                                    InfoRow(
                                      icon: Icons.event_outlined,
                                      label: 'Vencimiento',
                                      value: DateFormat(
                                        'dd/MM/yyyy',
                                      ).format(a.fechaVencimiento!),
                                    ),
                                ],
                              );
                              final actions = Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  OutlinedButton.icon(
                                    onPressed: () => _editAlumno(a),
                                    icon: const Icon(Icons.edit_outlined),
                                    label: const Text('Editar'),
                                  ),
                                  const SizedBox(width: 8),
                                  TextButton.icon(
                                    onPressed: () => _editNotificaciones(a),
                                    icon: const Icon(Icons.chat_outlined),
                                    label: const Text('WhatsApp'),
                                  ),
                                  IconButton(
                                    tooltip: 'Desactivar alumno',
                                    onPressed: a.activo
                                        ? () => _desactivarAlumno(a)
                                        : null,
                                    icon: const Icon(
                                      Icons.person_off_outlined,
                                      color: AppTheme.danger,
                                    ),
                                  ),
                                ],
                              );
                              return constraints.maxWidth > 620
                                  ? Row(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.end,
                                      children: [
                                        Expanded(child: details),
                                        const SizedBox(width: 18),
                                        actions,
                                      ],
                                    )
                                  : Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        details,
                                        const Divider(height: 24),
                                        actions,
                                      ],
                                    );
                            },
                          ),
                        ),
                      );
                    }),
                  const SizedBox(height: 70),
                ],
              ),
            ),
    );
  }
}
