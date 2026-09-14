import 'package:flutter/material.dart';

import '../../models/alumno_model.dart';
import '../../models/sucursal_model.dart';

class AlumnoEdicion {
  const AlumnoEdicion({
    required this.nombre,
    required this.telefono,
    required this.activo,
    this.sucursalPrincipalId,
  });

  final String nombre;
  final String telefono;
  final bool activo;
  final String? sucursalPrincipalId;
}

class EditAlumnoModal extends StatefulWidget {
  const EditAlumnoModal({
    super.key,
    required this.alumno,
    required this.sucursales,
  });

  final AlumnoModel alumno;
  final List<SucursalModel> sucursales;

  @override
  State<EditAlumnoModal> createState() => _EditAlumnoModalState();
}

class _EditAlumnoModalState extends State<EditAlumnoModal> {
  late final TextEditingController _nombre;
  late final TextEditingController _telefono;
  late bool _activo;
  String? _sucursalPrincipalId;

  @override
  void initState() {
    super.initState();
    _nombre = TextEditingController(text: widget.alumno.nombre);
    _telefono = TextEditingController(text: widget.alumno.telefono);
    _activo = widget.alumno.activo;
    _sucursalPrincipalId =
        widget.sucursales.any((s) => s.id == widget.alumno.sucursalPrincipalId)
        ? widget.alumno.sucursalPrincipalId
        : null;
  }

  @override
  void dispose() {
    _nombre.dispose();
    _telefono.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Editar alumno'),
      content: SizedBox(
        width: 430,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: _nombre,
                decoration: const InputDecoration(
                  labelText: 'Nombre',
                  prefixIcon: Icon(Icons.person_outline),
                ),
              ),
              const SizedBox(height: 14),
              TextField(
                controller: _telefono,
                decoration: const InputDecoration(
                  labelText: 'Teléfono',
                  prefixIcon: Icon(Icons.phone_outlined),
                ),
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<String>(
                initialValue: _sucursalPrincipalId ?? '',
                decoration: const InputDecoration(
                  labelText: 'Sucursal principal',
                ),
                items: [
                  const DropdownMenuItem(value: '', child: Text('Sin asignar')),
                  ...widget.sucursales.map(
                    (s) => DropdownMenuItem(value: s.id, child: Text(s.nombre)),
                  ),
                ],
                onChanged: (value) =>
                    setState(() => _sucursalPrincipalId = value),
              ),
              const SizedBox(height: 8),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Alumno activo'),
                subtitle: const Text('Puede acceder y registrar pagos'),
                value: _activo,
                onChanged: (value) => setState(() => _activo = value),
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(
            AlumnoEdicion(
              nombre: _nombre.text.trim(),
              telefono: _telefono.text.trim(),
              activo: _activo,
              sucursalPrincipalId: _sucursalPrincipalId,
            ),
          ),
          child: const Text('Guardar cambios'),
        ),
      ],
    );
  }
}
