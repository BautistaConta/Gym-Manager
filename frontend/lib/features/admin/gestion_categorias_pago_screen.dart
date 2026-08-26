import 'package:flutter/material.dart';

import '../../../core/services/categorias_pago_service.dart';
import '../../../models/categoria_pago_model.dart';
import '../../../models/tipo_abono_enum.dart';
import '../../../widgets/modals/create_categoria_pago_modal.dart';

class GestionCategoriasPagoScreen extends StatefulWidget {
  const GestionCategoriasPagoScreen({super.key});

  @override
  State<GestionCategoriasPagoScreen> createState() =>
      _GestionCategoriasPagoScreenState();
}

class _GestionCategoriasPagoScreenState
    extends State<GestionCategoriasPagoScreen> {
  final CategoriasPagoService _service =
      CategoriasPagoService();

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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Categorías de Pago'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _load,
          ),
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Crear categoría',
            onPressed: () async {
              final created = await showDialog<bool>(
                context: context,
                builder: (_) =>
                    const CreateCategoriaPagoModal(),
              );

              if (created == true) {
                _load();
              }
            },
          ),
        ],
      ),
      body: loading
          ? const Center(
              child: CircularProgressIndicator(),
            )
          : error != null
              ? Center(
                  child: Text(
                    error!,
                    style: const TextStyle(
                      color: Colors.redAccent,
                    ),
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
                        color: const Color(0xFF111111),
                        borderRadius:
                            BorderRadius.circular(16),
                        border: Border.all(
                          color: Theme.of(context)
                              .colorScheme
                              .primary,
                        ),
                      ),
                      child: Column(
                        crossAxisAlignment:
                            CrossAxisAlignment.start,
                        children: [
                          Text(
                            c.nombre,
                            style: Theme.of(context)
                                .textTheme
                                .titleMedium
                                ?.copyWith(
                                  color: Theme.of(context)
                                      .colorScheme
                                      .primary,
                                ),
                          ),

                          const SizedBox(height: 8),

                          Text(
                            'Precio: \$${c.precio}',
                          ),

                          Text(
                            'Duración: ${c.mesesDuracion} meses',
                          ),

                          Text(
                            'Tipo: ${tipoAbonoLabel(c.tipoAbono)}',
                          ),

                          const SizedBox(height: 8),

                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 10,
                              vertical: 4,
                            ),
                            decoration: BoxDecoration(
                              color: c.activa
                                  ? Theme.of(context)
                                      .colorScheme
                                      .primary
                                      .withValues(alpha: 0.15)
                                  : Colors.redAccent
                                      .withValues(alpha: 0.15),
                              borderRadius:
                                  BorderRadius.circular(12),
                            ),
                            child: Text(
                              c.activa
                                  ? 'ACTIVA'
                                  : 'INACTIVA',
                              style: TextStyle(
                                color: c.activa
                                    ? Theme.of(context)
                                        .colorScheme
                                        .primary
                                    : Colors.redAccent,
                                fontWeight:
                                    FontWeight.bold,
                              ),
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