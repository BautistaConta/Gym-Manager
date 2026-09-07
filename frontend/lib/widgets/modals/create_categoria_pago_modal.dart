import 'package:flutter/material.dart';

import '../../core/services/categorias_pago_service.dart';
import '../../core/theme/app_theme.dart';
import '../dark_text_field.dart';
import '../../models/tipo_abono_enum.dart';

class CreateCategoriaPagoModal extends StatefulWidget {
  const CreateCategoriaPagoModal({super.key});

  @override
  State<CreateCategoriaPagoModal> createState() =>
      _CreateCategoriaPagoModalState();
}

class _CreateCategoriaPagoModalState extends State<CreateCategoriaPagoModal> {
  final nombreController = TextEditingController();
  final precioController = TextEditingController();
  final mesesController = TextEditingController();

  TipoAbono tipoAbono = TipoAbono.adulto;

  bool loading = false;

  final CategoriasPagoService _service = CategoriasPagoService();

  Future<void> _submit() async {
    final precio = double.tryParse(precioController.text.replaceAll(',', '.'));
    final meses = int.tryParse(mesesController.text);
    if (nombreController.text.trim().isEmpty ||
        precio == null ||
        precio <= 0 ||
        meses == null ||
        meses <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Completá los datos correctamente.')),
      );
      return;
    }
    setState(() => loading = true);

    final success = await _service.create(
      nombre: nombreController.text.trim(),
      precio: precio,
      mesesDuracion: meses,
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
      backgroundColor: AppTheme.surface,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      title: Text(
        'Nueva Categoría',
        style: TextStyle(color: Theme.of(context).colorScheme.primary),
      ),
      content: SizedBox(
        width: 520,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Configurá el precio y la duración del nuevo abono.',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              const SizedBox(height: 20),
              DarkTextField(
                controller: nombreController,
                label: 'Nombre del abono',
                icon: Icons.card_membership_outlined,
              ),

              const SizedBox(height: 18),

              DarkTextField(
                controller: precioController,
                label: 'Precio',
                icon: Icons.attach_money,
                keyboardType: TextInputType.number,
              ),

              const SizedBox(height: 18),

              DarkTextField(
                controller: mesesController,
                label: 'Meses duración',
                icon: Icons.calendar_month,
                keyboardType: TextInputType.number,
              ),

              const SizedBox(height: 18),

              DropdownMenu<TipoAbono>(
                initialSelection: tipoAbono,
                expandedInsets: EdgeInsets.zero,
                label: const Text('Tipo de abono'),
                leadingIcon: const Icon(Icons.person_outline),
                dropdownMenuEntries: const [
                  DropdownMenuEntry(value: TipoAbono.adulto, label: 'Adulto'),
                  DropdownMenuEntry(value: TipoAbono.nino, label: 'Niño'),
                ],
                onSelected: (v) {
                  if (v != null) {
                    setState(() => tipoAbono = v);
                  }
                },
              ),
            ],
          ),
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
