import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/services/pagos_service.dart';
import '../../core/theme/app_theme.dart';
import '../../models/pago_model.dart';
import '../../widgets/app_ui.dart';

class GestionPagosScreen extends StatefulWidget {
  const GestionPagosScreen({super.key});
  @override
  State<GestionPagosScreen> createState() => _GestionPagosScreenState();
}

class _GestionPagosScreenState extends State<GestionPagosScreen> {
  final PagosService _service = PagosService();
  final TextEditingController _searchController = TextEditingController();
  List<PagoModel> _pagos = [];
  bool _loading = true;
  String? _error;
  String? _sendingId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final pagos = await _service.fetchAll();
      if (mounted) setState(() => _pagos = pagos);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _sendReminder(PagoModel pago) async {
    final confirmar = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        icon: const Icon(
          Icons.notifications_active_outlined,
          color: AppTheme.primaryGreen,
          size: 38,
        ),
        title: const Text('Enviar recordatorio'),
        content: Text(
          'Se enviará un WhatsApp a ${pago.alumnoNombre} por el vencimiento del ${DateFormat('dd/MM/yyyy').format(pago.periodoHasta)}.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancelar'),
          ),
          FilledButton.icon(
            onPressed: () => Navigator.pop(context, true),
            icon: const Icon(Icons.send_outlined),
            label: const Text('Enviar'),
          ),
        ],
      ),
    );
    if (confirmar != true) return;
    setState(() => _sendingId = pago.id);
    try {
      final error = await _service.enviarRecordatorio(pago.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            error == null
                ? 'Recordatorio enviado por WhatsApp.'
                : 'No se pudo enviar: $error',
          ),
        ),
      );
    } catch (e) {
      if (mounted)
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(e.toString())));
    } finally {
      if (mounted) setState(() => _sendingId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final query = _searchController.text.trim().toLowerCase();
    final pagos = _pagos
        .where(
          (p) =>
              p.alumnoNombre.toLowerCase().contains(query) ||
              p.alumnoDni.contains(query) ||
              p.categoriaPagoNombre.toLowerCase().contains(query),
        )
        .toList();
    final total = pagos.fold<double>(0, (value, p) => value + p.montoFinal);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Historial de pagos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Actualizar',
            onPressed: _loading ? null : _load,
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? _ErrorState(message: _error!, onRetry: _load)
          : AppPage(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SectionHeader(
                    icon: Icons.receipt_long_outlined,
                    title: 'Pagos registrados',
                    subtitle:
                        'Consultá cobros y enviá recordatorios de vencimiento',
                  ),
                  const SizedBox(height: 22),
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final cardWidth = constraints.maxWidth >= 600
                          ? (constraints.maxWidth - 14) / 2
                          : constraints.maxWidth;
                      return Wrap(
                        spacing: 14,
                        runSpacing: 12,
                        children: [
                          SizedBox(
                            width: cardWidth,
                            child: _MetricCard(
                              icon: Icons.receipt_outlined,
                              label: 'Operaciones visibles',
                              value: '${pagos.length}',
                            ),
                          ),
                          SizedBox(
                            width: cardWidth,
                            child: _MetricCard(
                              icon: Icons.account_balance_wallet_outlined,
                              label: 'Total visible',
                              value:
                                  '\$${NumberFormat('#,##0.00', 'es_AR').format(total)}',
                            ),
                          ),
                        ],
                      );
                    },
                  ),
                  const SizedBox(height: 18),
                  TextField(
                    controller: _searchController,
                    onChanged: (_) => setState(() {}),
                    decoration: InputDecoration(
                      labelText: 'Buscar pagos',
                      hintText: 'Alumno, DNI o categoría',
                      prefixIcon: const Icon(Icons.search),
                      suffixIcon: query.isEmpty
                          ? null
                          : IconButton(
                              onPressed: () =>
                                  setState(_searchController.clear),
                              icon: const Icon(Icons.close),
                            ),
                    ),
                  ),
                  const SizedBox(height: 18),
                  if (pagos.isEmpty)
                    EmptyState(
                      icon: Icons.search_off_rounded,
                      title: query.isEmpty
                          ? 'Todavía no hay pagos'
                          : 'No encontramos resultados',
                      message: query.isEmpty
                          ? 'Los cobros confirmados aparecerán en esta sección.'
                          : 'Probá con otro nombre, DNI o categoría.',
                    )
                  else
                    ...pagos.map(
                      (pago) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: _PagoCard(
                          pago: pago,
                          sending: _sendingId == pago.id,
                          onReminder: () => _sendReminder(pago),
                        ),
                      ),
                    ),
                ],
              ),
            ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.icon,
    required this.label,
    required this.value,
  });
  final IconData icon;
  final String label;
  final String value;
  @override
  Widget build(BuildContext context) => SurfaceCard(
    child: Row(
      children: [
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: AppTheme.primaryGreen.withValues(alpha: .1),
            borderRadius: BorderRadius.circular(13),
          ),
          child: Icon(icon, color: AppTheme.primaryGreen),
        ),
        const SizedBox(width: 13),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 2),
              Text(value, style: Theme.of(context).textTheme.titleLarge),
            ],
          ),
        ),
      ],
    ),
  );
}

class _PagoCard extends StatelessWidget {
  const _PagoCard({
    required this.pago,
    required this.sending,
    required this.onReminder,
  });
  final PagoModel pago;
  final bool sending;
  final VoidCallback onReminder;

  @override
  Widget build(BuildContext context) => SurfaceCard(
    child: LayoutBuilder(
      builder: (context, constraints) {
        final wide = constraints.maxWidth >= 660;
        final info = Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  backgroundColor: AppTheme.surfaceHigh,
                  foregroundColor: AppTheme.primaryGreen,
                  child: Text(
                    pago.alumnoNombre.isEmpty
                        ? '?'
                        : pago.alumnoNombre[0].toUpperCase(),
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        pago.alumnoNombre,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      Text(
                        'DNI ${pago.alumnoDni} · ${pago.categoriaPagoNombre}',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            InfoRow(
              icon: Icons.calendar_today_outlined,
              label: 'Cobrado',
              value: DateFormat('dd/MM/yyyy').format(pago.fechaPago.toLocal()),
            ),
            InfoRow(
              icon: Icons.event_available_outlined,
              label: 'Vigencia',
              // Son fechas de calendario, no instantes: convertirlas a la zona
              // local mostraría el día anterior en Argentina (UTC-3).
              value:
                  '${DateFormat('dd/MM/yyyy').format(pago.periodoDesde)} al ${DateFormat('dd/MM/yyyy').format(pago.periodoHasta)}',
            ),
            InfoRow(
              icon: Icons.store_outlined,
              label: 'Sucursal',
              value: pago.sucursalNombre,
            ),
            InfoRow(
              icon: Icons.payments_outlined,
              label: 'Método',
              value: pago.metodoPago,
            ),
          ],
        );
        final amount = Column(
          crossAxisAlignment: wide
              ? CrossAxisAlignment.end
              : CrossAxisAlignment.start,
          children: [
            Text(
              '\$${NumberFormat('#,##0.00', 'es_AR').format(pago.montoFinal)}',
              style: Theme.of(
                context,
              ).textTheme.headlineSmall?.copyWith(color: AppTheme.primaryGreen),
            ),
            if (pago.descuentoPorcentaje > 0) ...[
              const SizedBox(height: 5),
              StatusBadge(
                label:
                    '${pago.descuentoPorcentaje.toStringAsFixed(0)}% descuento',
                color: AppTheme.warning,
                icon: Icons.percent,
              ),
            ],
            const SizedBox(height: 15),
            OutlinedButton.icon(
              onPressed: sending ? null : onReminder,
              icon: sending
                  ? const SizedBox.square(
                      dimension: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.notifications_none_rounded),
              label: const Text('Recordar'),
            ),
          ],
        );
        return wide
            ? Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(child: info),
                  const SizedBox(width: 24),
                  amount,
                ],
              )
            : Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [info, const Divider(height: 28), amount],
              );
      },
    ),
  );
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_outlined,
            color: AppTheme.danger,
            size: 48,
          ),
          const SizedBox(height: 14),
          Text(
            'No pudimos cargar los pagos',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 6),
          Text(
            message,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodySmall,
          ),
          const SizedBox(height: 18),
          FilledButton.icon(
            onPressed: onRetry,
            icon: const Icon(Icons.refresh),
            label: const Text('Reintentar'),
          ),
        ],
      ),
    ),
  );
}
