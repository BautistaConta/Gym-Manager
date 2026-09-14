import 'package:flutter/material.dart';

import '../../models/alumno_model.dart';

class ConsentimientoEdicion {
  const ConsentimientoEdicion({
    required this.habilitadas,
    required this.confirmado,
    this.medio,
  });

  final bool habilitadas;
  final bool confirmado;
  final String? medio;
}

class EditConsentimientoModal extends StatefulWidget {
  const EditConsentimientoModal({super.key, required this.alumno});

  final AlumnoModel alumno;

  @override
  State<EditConsentimientoModal> createState() =>
      _EditConsentimientoModalState();
}

class _EditConsentimientoModalState extends State<EditConsentimientoModal> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _medio;
  late bool _habilitadas;
  bool _confirmado = false;
  bool _mostrarErrorConfirmacion = false;

  @override
  void initState() {
    super.initState();
    _habilitadas =
        widget.alumno.notificacionesHabilitadas &&
        widget.alumno.fechaConsentimientoNotificacionesUtc != null;
    _medio = TextEditingController(
      text: widget.alumno.medioConsentimientoNotificaciones ?? '',
    );
  }

  @override
  void dispose() {
    _medio.dispose();
    super.dispose();
  }

  void _guardar() {
    if (_habilitadas && !_formKey.currentState!.validate()) return;
    if (_habilitadas && !_confirmado) {
      setState(() => _mostrarErrorConfirmacion = true);
      return;
    }
    Navigator.of(context).pop(
      ConsentimientoEdicion(
        habilitadas: _habilitadas,
        confirmado: _confirmado,
        medio: _habilitadas ? _medio.text.trim() : null,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('WhatsApp · ${widget.alumno.nombre}'),
      content: SizedBox(
        width: 430,
        child: SingleChildScrollView(
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Avisos de cuota por WhatsApp'),
                  subtitle: const Text(
                    'El alumno puede aceptar o retirar su consentimiento.',
                  ),
                  value: _habilitadas,
                  onChanged: (value) => setState(() {
                    _habilitadas = value;
                    _confirmado = false;
                    _mostrarErrorConfirmacion = false;
                  }),
                ),
                if (_habilitadas) ...[
                  TextFormField(
                    controller: _medio,
                    decoration: const InputDecoration(
                      labelText: 'Cómo dio su consentimiento',
                    ),
                    maxLength: 200,
                    validator: (value) => value == null || value.trim().isEmpty
                        ? 'Indicá el medio de consentimiento'
                        : null,
                  ),
                  CheckboxListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text(
                      'Confirmo que el alumno aceptó explícitamente',
                    ),
                    value: _confirmado,
                    onChanged: (value) => setState(() {
                      _confirmado = value ?? false;
                      _mostrarErrorConfirmacion = false;
                    }),
                  ),
                  if (_mostrarErrorConfirmacion)
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
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        FilledButton(onPressed: _guardar, child: const Text('Guardar')),
      ],
    );
  }
}
