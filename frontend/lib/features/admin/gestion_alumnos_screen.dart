import 'package:flutter/material.dart';
import '../../core/services/alumnos_service.dart';
import '../../models/alumno_model.dart';
import '../../models/estado_alumno.dart';
import '../../widgets/modals/create_alumno_modal.dart';

class GestionAlumnosScreen extends StatefulWidget {
  const GestionAlumnosScreen({super.key});

  @override
  State<GestionAlumnosScreen> createState() =>
      _GestionAlumnosScreenState();
}

class _GestionAlumnosScreenState
    extends State<GestionAlumnosScreen> {
  final AlumnosService _service = AlumnosService();

  bool loading = true;
  String? error;
  List<AlumnoModel> alumnos = [];

  @override
  void initState() {
    super.initState();
    _load();
  }
  Map<String, EstadoAlumno> estados = {};

 Future<void> _load() async {
  setState(() {
    loading = true;
    error = null;
  });

  try {
    final data = await _service.fetchAll();

    // 🔥 Carga paralela de estados
    final futures = data.map((a) async {
      try {
        final estado = await _service.getEstado(a.id);
        return MapEntry(a.id, estado);
      } catch (_) {
        return null;
      }
    });

    final results = await Future.wait(futures);

    final estadosMap = {
      for (var e in results)
        if (e != null) e.key: e.value
    };

    setState(() {
      alumnos = data;
      estados = estadosMap;
    });
  } catch (e) {
    setState(() => error = e.toString());
  } finally {
    setState(() => loading = false);
  }
}

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Gestión de Alumnos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _load,
          ),
          IconButton(
            icon: const Icon(Icons.person_add),
            tooltip: 'Crear alumno',
            onPressed: () async {
              final created = await showDialog<bool>(
                context: context,
                builder: (_) => const CreateAlumnoModal(),
              );

              if (created == true) {
                _load();
              }
            },
          ),
        ],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
              ? Center(
                  child: Text(
                    error!,
                    style: const TextStyle(color: Colors.redAccent),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: alumnos.length,
                    itemBuilder: (context, i) {
                      final a = alumnos[i];
                      final estado = estados[a.id];

                      return Container(
                        margin: const EdgeInsets.only(bottom: 12),
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: const Color(0xFF111111),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(
                            color: Theme.of(context).colorScheme.primary,
                          ),
                        ),
                       child: Column(
  crossAxisAlignment: CrossAxisAlignment.start,
  children: [
    Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Expanded(
          child: Text(
            a.nombre,
            style: Theme.of(context)
                .textTheme
                .titleMedium
                ?.copyWith(
                  color: Theme.of(context).colorScheme.primary,
                ),
          ),
        ),
        if (estado != null)
          _buildEstadoBadge(estado.estado),
      ],
    ),

    const SizedBox(height: 6),

    Text('DNI: ${a.dni}'),
    Text('Tel: ${a.telefono}'),

    const SizedBox(height: 6),

    Text(
      a.activo ? 'Activo' : 'Inactivo',
      style: TextStyle(
        color: a.activo
            ? Theme.of(context).colorScheme.primary
            : Colors.redAccent,
      ),
    ),

    // 🔥 FECHA DE VENCIMIENTO
    if (estado?.fechaVencimiento != null)
      Padding(
        padding: const EdgeInsets.only(top: 6),
        child: Text(
          'Vence: ${estado!.fechaVencimiento!.toLocal().toString().split(' ')[0]}',
          style: const TextStyle(color: Colors.grey),
        ),
      ),
  ],
),
                      );
                    },
                  ),
                ),
    );
  }

  Widget _buildEstadoBadge(String estado) {
  final primary = Theme.of(context).colorScheme.primary;

  Color color;

  switch (estado) {
    case 'ACTIVO':
      color = primary;
      break;
    case 'VENCIDO':
      color = Colors.redAccent;
      break;
    default:
      color = Colors.grey;
  }

  return Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
    decoration: BoxDecoration(
      color: color.withOpacity(0.15),
      borderRadius: BorderRadius.circular(12),
      border: Border.all(color: color),
    ),
    child: Text(
      estado,
      style: TextStyle(
        color: color,
        fontWeight: FontWeight.bold,
        fontSize: 12,
      ),
    ),
  );
}
}