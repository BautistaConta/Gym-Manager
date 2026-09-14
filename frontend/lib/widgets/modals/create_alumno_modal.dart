import 'package:flutter/material.dart';
import '../../core/services/alumnos_service.dart';
import '../../models/sucursal_model.dart';

class CreateAlumnoModal extends StatefulWidget {
  final List<SucursalModel> sucursales;
  const CreateAlumnoModal({super.key, required this.sucursales});

  @override
  State<CreateAlumnoModal> createState() => _CreateAlumnoModalState();
}

class _CreateAlumnoModalState extends State<CreateAlumnoModal> {
  final _formKey = GlobalKey<FormState>();
  final _nombreCtrl = TextEditingController();
  final _dniCtrl = TextEditingController();
  final _telefonoCtrl = TextEditingController();
  final _medioConsentimientoCtrl = TextEditingController();

  final AlumnosService _service = AlumnosService();

  bool loading = false;
  bool notificacionesHabilitadas = true;
  bool consentimientoConfirmado = false;
  bool mostrarErrorConsentimiento = false;
  String? sucursalPrincipalId;

  @override
  void dispose() {
    _nombreCtrl.dispose();
    _dniCtrl.dispose();
    _telefonoCtrl.dispose();
    _medioConsentimientoCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (notificacionesHabilitadas && !consentimientoConfirmado) {
      setState(() => mostrarErrorConsentimiento = true);
      return;
    }

    final messenger = ScaffoldMessenger.of(context);
    setState(() => loading = true);

    try {
      await _service.createAlumno(
        nombre: _nombreCtrl.text.trim(),
        dni: _dniCtrl.text.trim(),
        telefono: _telefonoCtrl.text.trim(),
        sucursalPrincipalId: sucursalPrincipalId,
        notificacionesHabilitadas: notificacionesHabilitadas,
        consentimientoConfirmado: consentimientoConfirmado,
        medioConsentimiento: notificacionesHabilitadas
            ? _medioConsentimientoCtrl.text.trim()
            : null,
      );

      if (!mounted) return;
      messenger.showSnackBar(const SnackBar(content: Text('Alumno creado')));
      Navigator.pop(context, true);
    } catch (e) {
      if (mounted)
        messenger.showSnackBar(SnackBar(content: Text(e.toString())));
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Crear alumno'),
      content: SizedBox(
        width: 430,
        child: SingleChildScrollView(
          child: Form(
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
                const SizedBox(height: 10),
                DropdownButtonFormField<String>(
                  initialValue: sucursalPrincipalId ?? '',
                  decoration: const InputDecoration(
                    labelText: 'Sucursal principal',
                  ),
                  items: [
                    const DropdownMenuItem(
                      value: '',
                      child: Text('Sin asignar'),
                    ),
                    ...widget.sucursales.map(
                      (s) =>
                          DropdownMenuItem(value: s.id, child: Text(s.nombre)),
                    ),
                  ],
                  onChanged: (value) => setState(
                    () => sucursalPrincipalId = value == '' ? null : value,
                  ),
                ),
                const SizedBox(height: 12),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Avisos por WhatsApp'),
                  subtitle: const Text(
                    'Solo si el alumno aceptó recibir avisos de cuota.',
                  ),
                  value: notificacionesHabilitadas,
                  onChanged: (value) => setState(() {
                    notificacionesHabilitadas = value;
                    consentimientoConfirmado = false;
                    mostrarErrorConsentimiento = false;
                  }),
                ),
                if (notificacionesHabilitadas) ...[
                  TextFormField(
                    controller: _medioConsentimientoCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Cómo dio su consentimiento',
                      hintText: 'Ej. formulario firmado o verbal en recepción',
                    ),
                    maxLength: 200,
                    validator: (value) =>
                        notificacionesHabilitadas &&
                            (value == null || value.trim().isEmpty)
                        ? 'Indicá el medio de consentimiento'
                        : null,
                  ),
                  CheckboxListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text(
                      'Confirmo que el alumno aceptó explícitamente',
                    ),
                    value: consentimientoConfirmado,
                    onChanged: (value) => setState(() {
                      consentimientoConfirmado = value ?? false;
                      mostrarErrorConsentimiento = false;
                    }),
                  ),
                  if (mostrarErrorConsentimiento)
                    Text(
                      'Confirmá el consentimiento antes de habilitar los avisos.',
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                ],
              ],
            ),
          ),
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
