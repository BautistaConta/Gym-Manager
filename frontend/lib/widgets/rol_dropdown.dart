import 'package:flutter/material.dart';
import '../models/rol_enum.dart';

typedef OnRoleSelected = Future<bool> Function(String newRole);

class RolDropdown extends StatefulWidget {
  final Rol currentRole;
  final OnRoleSelected onRoleSelected;

  const RolDropdown({
    super.key,
    required this.currentRole,
    required this.onRoleSelected,
  });

  @override
  State<RolDropdown> createState() => _RolDropdownState();
}

class _RolDropdownState extends State<RolDropdown> {
  late String selected;

  final options = ['Admin', 'Gestor', 'Profesor', 'Alumno'];

  @override
  void initState() {
    super.initState();
    selected = rolToString(widget.currentRole);
  }

  @override
  Widget build(BuildContext context) {
    return DropdownButton<String>(
      value: selected,
      items: options.map((o) => DropdownMenuItem(value: o, child: Text(o))).toList(),
      onChanged: (v) async {
        if (v == null) return;
        final previous = selected;
        setState(() => selected = v);
        final success = await widget.onRoleSelected(v);
        if (!success) {
          // revertir y mostrar error
          setState(() => selected = previous);
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Error al actualizar rol')),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Rol actualizado correctamente')),
          );
        }
      },
    );
  }
}
