import 'package:flutter/material.dart';

import '../../core/services/categorias_pago_service.dart';
import '../../core/theme/app_theme.dart';
import '../../models/categoria_pago_model.dart';
import '../../models/tipo_abono_enum.dart';
import '../../widgets/modals/create_categoria_pago_modal.dart';

class GestionCategoriasPagoScreen extends StatefulWidget {
  const GestionCategoriasPagoScreen({super.key});

  @override
  State<GestionCategoriasPagoScreen> createState() =>
      _GestionCategoriasPagoScreenState();
}

class _GestionCategoriasPagoScreenState
    extends State<GestionCategoriasPagoScreen> {
  final CategoriasPagoService _service = CategoriasPagoService();

  bool loading = true;
  String? error;

  List<CategoriaPagoModel> categorias = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });

    try {
      final data = await _service.fetchAll();

      setState(() {
        categorias = data;
      });
    } catch (e) {
      setState(() {
        error = e.toString();
      });
    } finally {
      setState(() {
        loading = false;
      });
    }
  }

  Future<void> _edit(CategoriaPagoModel categoria) async {
    final nombreController = TextEditingController(text: categoria.nombre);
    final precioController = TextEditingController(
      text: categoria.precio.toString(),
    );
    final mesesController = TextEditingController(
      text: categoria.mesesDuracion.toString(),
    );
    var tipoAbono = TipoAbono.values.elementAt(
      categoria.tipoAbono.clamp(0, TipoAbono.values.length - 1),
    );
    var activa = categoria.activa;

    final updated = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Editar categoría'),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Modificá la información comercial del abono.',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                  const SizedBox(height: 20),
                  TextField(
                    controller: nombreController,
                    decoration: const InputDecoration(
                      labelText: 'Nombre del abono',
                      prefixIcon: Icon(Icons.card_membership_outlined),
                    ),
                  ),
                  const SizedBox(height: 18),
                  TextField(
                    controller: precioController,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Precio',
                      prefixIcon: Icon(Icons.attach_money),
                    ),
                  ),
                  const SizedBox(height: 18),
                  TextField(
                    controller: mesesController,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Meses de duración',
                      prefixIcon: Icon(Icons.calendar_month_outlined),
                    ),
                  ),
                  const SizedBox(height: 18),
                  DropdownMenu<TipoAbono>(
                    initialSelection: tipoAbono,
                    expandedInsets: EdgeInsets.zero,
                    label: const Text('Tipo de abono'),
                    leadingIcon: const Icon(Icons.person_outline),
                    dropdownMenuEntries: const [
                      DropdownMenuEntry(
                        value: TipoAbono.adulto,
                        label: 'Adulto',
                      ),
                      DropdownMenuEntry(value: TipoAbono.nino, label: 'Niño'),
                    ],
                    onSelected: (value) {
                      if (value != null) {
                        setDialogState(() => tipoAbono = value);
                      }
                    },
                  ),
                  const SizedBox(height: 10),
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Categoría activa'),
                    subtitle: const Text('Disponible para nuevos cobros'),
                    value: activa,
                    onChanged: (value) => setDialogState(() => activa = value),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialogContext, true),
              child: const Text('Guardar'),
            ),
          ],
        ),
      ),
    );

    final nombre = nombreController.text.trim();
    final precio = double.tryParse(precioController.text.trim());
    final meses = int.tryParse(mesesController.text.trim());
    nombreController.dispose();
    precioController.dispose();
    mesesController.dispose();

    if (updated != true) return;
    if (nombre.isEmpty || precio == null || meses == null || meses <= 0) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Completa los datos correctamente')),
        );
      }
      return;
    }

    try {
      await _service.update(
        categoria.id,
        nombre: nombre,
        precio: precio,
        mesesDuracion: meses,
        tipoAbono: tipoAbono.index,
        activa: activa,
      );
      await _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  Future<void> _deactivate(CategoriaPagoModel categoria) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Desactivar categoría'),
        content: Text('¿Desactivar "${categoria.nombre}"?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: const Text('Desactivar'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    try {
      await _service.deactivate(categoria.id);
      await _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Categorías de Pago'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Crear categoría',
            onPressed: () async {
              final created = await showDialog<bool>(
                context: context,
                builder: (_) => const CreateCategoriaPagoModal(),
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
          : ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: categorias.length,
              itemBuilder: (context, i) {
                final c = categorias[i];

                return Container(
                  margin: const EdgeInsets.only(bottom: 12),
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppTheme.surface,
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: AppTheme.border),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        c.nombre,
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(
                              color: Theme.of(context).colorScheme.primary,
                            ),
                      ),

                      const SizedBox(height: 8),

                      Text('Precio: \$${c.precio}'),

                      Text('Duración: ${c.mesesDuracion} meses'),

                      Text('Tipo: ${tipoAbonoLabel(c.tipoAbono)}'),

                      const SizedBox(height: 8),

                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: c.activa
                              ? Theme.of(
                                  context,
                                ).colorScheme.primary.withValues(alpha: 0.15)
                              : Colors.redAccent.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          c.activa ? 'ACTIVA' : 'INACTIVA',
                          style: TextStyle(
                            color: c.activa
                                ? Theme.of(context).colorScheme.primary
                                : Colors.redAccent,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      Align(
                        alignment: Alignment.centerRight,
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              tooltip: 'Editar categoría',
                              onPressed: () => _edit(c),
                              icon: const Icon(Icons.edit_outlined),
                            ),
                            IconButton(
                              tooltip: 'Desactivar categoría',
                              onPressed: c.activa ? () => _deactivate(c) : null,
                              icon: const Icon(
                                Icons.delete_outline,
                                color: Colors.redAccent,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
    );
  }
}
