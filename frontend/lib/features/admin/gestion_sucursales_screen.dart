import 'package:flutter/material.dart';
import '../../core/services/sucursales_service.dart';
import '../../models/sucursal_model.dart';
import '../../widgets/modals/create_sucursal_modal.dart';

class GestionSucursalesScreen extends StatefulWidget {
  const GestionSucursalesScreen({super.key});

  @override
  State<GestionSucursalesScreen> createState() =>
      _GestionSucursalesScreenState();
}

class _GestionSucursalesScreenState
    extends State<GestionSucursalesScreen> {
  final SucursalesService _service = SucursalesService();

  bool loading = true;
  String? error;
  List<SucursalModel> sucursales = [];

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
      setState(() => sucursales = data);
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      setState(() => loading = false);
    }
  }

  Future<void> _edit(SucursalModel s) async {
    final nombre = TextEditingController(text: s.nombre); final direccion = TextEditingController(text: s.direccion);
    final ok = await showDialog<bool>(context: context, builder: (_) => AlertDialog(title: const Text('Editar sucursal'), content: Column(mainAxisSize: MainAxisSize.min, children: [TextField(controller: nombre, decoration: const InputDecoration(labelText: 'Nombre')), TextField(controller: direccion, decoration: const InputDecoration(labelText: 'Dirección'))]), actions: [TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancelar')), FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Guardar'))]));
    if (ok != true) return; try { await _service.updateSucursal(s.id, nombre: nombre.text.trim(), direccion: direccion.text.trim()); await _load(); } catch (e) { if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString()))); }
  }

  Future<void> _delete(SucursalModel s) async {
    final ok = await showDialog<bool>(context: context, builder: (_) => AlertDialog(title: const Text('Eliminar sucursal'), content: Text('¿Eliminar ${s.nombre}?'), actions: [TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancelar')), FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Eliminar'))]));
    if (ok != true) return; try { await _service.deleteSucursal(s.id); await _load(); } catch (e) { if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString()))); }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Gestión de Sucursales'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _load,
          ),
          IconButton(
            icon: const Icon(Icons.add_business),
            tooltip: 'Crear sucursal',
            onPressed: () async {
              final created = await showDialog<bool>(
                context: context,
                barrierDismissible: false,
                builder: (_) => const CreateSucursalModal(),
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
                    itemCount: sucursales.length,
                    itemBuilder: (context, i) {
                      final s = sucursales[i];
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
                            Text(
                              s.nombre,
                              style: Theme.of(context)
                                  .textTheme
                                  .titleMedium
                                  ?.copyWith(
                                    color: Theme.of(context)
                                        .colorScheme
                                        .primary,
                                  ),
                            ),
                            const SizedBox(height: 6),
                            Text(s.direccion),
                            Align(alignment: Alignment.centerRight, child: Row(mainAxisSize: MainAxisSize.min, children: [IconButton(onPressed: () => _edit(s), icon: const Icon(Icons.edit_outlined)), IconButton(onPressed: () => _delete(s), icon: const Icon(Icons.delete_outline, color: Colors.redAccent))])),
                          ],
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
