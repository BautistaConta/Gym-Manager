import 'package:flutter/material.dart';
import '../../core/services/alumnos_service.dart';

class CreateAlumnoModal extends StatefulWidget {
  const CreateAlumnoModal({super.key});

  @override
  State<CreateAlumnoModal> createState() =>
      _CreateAlumnoModalState();
}

class _CreateAlumnoModalState
    extends State<CreateAlumnoModal> {
  final _formKey = GlobalKey<FormState>();
  final _nombreCtrl = TextEditingController();
  final _dniCtrl = TextEditingController();
  final _telefonoCtrl = TextEditingController();

  final AlumnosService _service = AlumnosService();

  bool loading = false;

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => loading = true);

    try {
      await _service.createAlumno(
        nombre: _nombreCtrl.text.trim(),
        dni: _dniCtrl.text.trim(),
        telefono: _telefonoCtrl.text.trim(),
      );

      Navigator.pop(context, true);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Alumno creado')),
      );
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.toString())),
      );
    } finally {
      setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Crear alumno'),
      content: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextFormField(
              controller: _nombreCtrl,
              decoration: const InputDecoration(labelText: 'Nombre'),
              validator: (v) =>
                  v == null || v.isEmpty ? 'Campo requerido' : null,
            ),
            const SizedBox(height: 10),
            TextFormField(
              controller: _dniCtrl,
              decoration: const InputDecoration(labelText: 'DNI'),
              validator: (v) =>
                  v == null || v.isEmpty ? 'Campo requerido' : null,
            ),
            const SizedBox(height: 10),
            TextFormField(
              controller: _telefonoCtrl,
              decoration: const InputDecoration(labelText: 'Teléfono'),
              validator: (v) =>
                  v == null || v.isEmpty ? 'Campo requerido' : null,
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context, false),
          child: const Text('Cancelar'),
        ),
        ElevatedButton(
          onPressed: loading ? null : _submit,
          child: loading
              ? const CircularProgressIndicator(strokeWidth: 2)
              : const Text('Crear'),
        ),
      ],
    );
  }
}