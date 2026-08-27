import 'package:flutter/material.dart';

import '../../core/services/categorias_pago_service.dart';
import '../dark_text_field.dart';
import '../../models/tipo_abono_enum.dart';

class CreateCategoriaPagoModal extends StatefulWidget {
  const CreateCategoriaPagoModal({super.key});

  @override
  State<CreateCategoriaPagoModal> createState() =>
      _CreateCategoriaPagoModalState();
}

class _CreateCategoriaPagoModalState
    extends State<CreateCategoriaPagoModal> {
  final nombreController = TextEditingController();
  final precioController = TextEditingController();
  final mesesController = TextEditingController();

  TipoAbono tipoAbono = TipoAbono.adulto;

  bool loading = false;

  final CategoriasPagoService _service =
      CategoriasPagoService();

  Future<void> _submit() async {
    setState(() => loading = true);

    final success = await _service.create(
      nombre: nombreController.text,
      precio: double.parse(precioController.text),
      mesesDuracion: int.parse(mesesController.text),
      tipoAbono: tipoAbono.index,
    );

    setState(() => loading = false);

    if (success && mounted) {
      Navigator.pop(context, true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      backgroundColor: const Color(0xFF111111),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
      ),
      title: Text(
        'Nueva Categoría',
        style: TextStyle(
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            DarkTextField(
              controller: nombreController,
              label: 'Nombre',
              icon: Icons.category,
            ),

            const SizedBox(height: 16),

            DarkTextField(
              controller: precioController,
              label: 'Precio',
              icon: Icons.attach_money,
              keyboardType: TextInputType.number,
            ),

            const SizedBox(height: 16),

            DarkTextField(
              controller: mesesController,
              label: 'Meses duración',
              icon: Icons.calendar_month,
              keyboardType: TextInputType.number,
            ),

            const SizedBox(height: 16),

            DropdownButtonFormField<TipoAbono>(
              initialValue: tipoAbono,
              dropdownColor: const Color(0xFF1A1A1A),
              decoration: const InputDecoration(
                labelText: 'Tipo de abono',
              ),
              items: const [
                DropdownMenuItem(
                  value: TipoAbono.adulto,
                  child: Text('Adulto'),
                ),
                DropdownMenuItem(
                  value: TipoAbono.nino,
                  child: Text('Niño'),
                ),
              ],
              onChanged: (v) {
                if (v != null) {
                  setState(() => tipoAbono = v);
                }
              },
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancelar'),
        ),
        ElevatedButton(
          onPressed: loading ? null : _submit,
          child: loading
              ? const CircularProgressIndicator()
              : const Text('Crear'),
        ),
      ],
    );
  }
}