import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/services/alumnos_service.dart';
import '../../core/theme/app_theme.dart';
import '../../models/alumno_model.dart';
import '../../models/estado_alumno.dart';
import '../../widgets/app_ui.dart';
import '../../widgets/modals/create_alumno_modal.dart';

class GestionAlumnosScreen extends StatefulWidget {
  const GestionAlumnosScreen({super.key});
  @override
  State<GestionAlumnosScreen> createState() => _GestionAlumnosScreenState();
}

class _GestionAlumnosScreenState extends State<GestionAlumnosScreen> {
  final AlumnosService _service = AlumnosService();
  final TextEditingController _searchController = TextEditingController();
  bool loading = true;
  String? error;
  List<AlumnoModel> alumnos = [];
  Map<String, EstadoAlumno> estados = {};

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
      final results = await Future.wait(
        data.map((a) async {
          try {
            return MapEntry(a.id, await _service.getEstado(a.id));
          } catch (_) {
            return null;
          }
        }),
      );
      if (!mounted) return;
      setState(() {
        alumnos = data;
        estados = {
          for (final e in results)
            if (e != null) e.key: e.value,
        };
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
    final nombre = TextEditingController(text: alumno.nombre);
    final telefono = TextEditingController(text: alumno.telefono);
    var activo = alumno.activo;
    final ok = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Editar alumno'),
          content: SizedBox(
            width: 430,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: nombre,
                  decoration: const InputDecoration(
                    labelText: 'Nombre',
                    prefixIcon: Icon(Icons.person_outline),
                  ),
                ),
                const SizedBox(height: 14),
                TextField(
                  controller: telefono,
                  decoration: const InputDecoration(
                    labelText: 'Teléfono',
                    prefixIcon: Icon(Icons.phone_outlined),
                  ),
                ),
                const SizedBox(height: 8),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Alumno activo'),
                  subtitle: const Text('Puede acceder y registrar pagos'),
                  value: activo,
                  onChanged: (value) => setDialogState(() => activo = value),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialogContext, true),
              child: const Text('Guardar cambios'),
            ),
          ],
        ),
      ),
    );
    final newName = nombre.text.trim();
    final newPhone = telefono.text.trim();
    nombre.dispose();
    telefono.dispose();
    if (ok != true) return;
    try {
      await _service.updateAlumno(
        alumno.id,
        nombre: newName,
        telefono: newPhone,
        activo: activo,
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
              a.nombre.toLowerCase().contains(query) || a.dni.contains(query),
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
                builder: (_) => const CreateAlumnoModal(),
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
                      final estado = estados[a.id];
                      final stateColor = estado?.estado == 'ACTIVO'
                          ? AppTheme.primaryGreen
                          : estado?.estado == 'VENCIDO'
                          ? AppTheme.danger
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
                                            estado?.estado ??
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
                                  if (estado?.fechaVencimiento != null)
                                    InfoRow(
                                      icon: Icons.event_outlined,
                                      label: 'Vencimiento',
                                      value: DateFormat(
                                        'dd/MM/yyyy',
                                      ).format(estado!.fechaVencimiento!),
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
