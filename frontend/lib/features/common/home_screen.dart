import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/auth_provider.dart';
import '../../core/theme/app_theme.dart';
import '../../models/rol_enum.dart';
import '../../widgets/app_ui.dart';
import '../../widgets/dashboard_card.dart';
import '../../widgets/dashboard_item.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    if (authState.loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    final user = authState.user;
    if (user == null) {
      return const Scaffold(
        body: Center(child: Text('Usuario no autenticado')),
      );
    }
    final visibleItems = dashboardItems
        .where((item) => item.allowedRoles.contains(user.rol))
        .toList();

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Container(
              width: 34,
              height: 34,
              decoration: BoxDecoration(
                color: AppTheme.primaryGreen,
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Icon(
                Icons.fitness_center_rounded,
                color: AppTheme.primaryDark,
                size: 20,
              ),
            ),
            const SizedBox(width: 11),
            const Text('Gym Manager'),
          ],
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 14),
            child: Center(
              child: StatusBadge(
                label: rolToString(user.rol),
                color: AppTheme.primaryGreen,
                icon: Icons.verified_user_outlined,
              ),
            ),
          ),
          IconButton(
            tooltip: 'Cerrar sesión',
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await ref.read(authProvider.notifier).logout();
              if (!context.mounted) return;
              Navigator.pushNamedAndRemoveUntil(
                context,
                '/login',
                (_) => false,
              );
            },
          ),
        ],
      ),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final columns = constraints.maxWidth >= 1000
              ? 3
              : constraints.maxWidth >= 620
              ? 2
              : 1;
          return AppPage(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: double.infinity,
                  padding: EdgeInsets.all(constraints.maxWidth < 600 ? 22 : 30),
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [Color(0xFF174326), Color(0xFF102619)],
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                    ),
                    borderRadius: BorderRadius.circular(24),
                    border: Border.all(
                      color: AppTheme.primaryGreen.withValues(alpha: .25),
                    ),
                  ),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Hola, ${user.nombre.split(' ').first}',
                              style: Theme.of(context).textTheme.headlineLarge,
                            ),
                            const SizedBox(height: 8),
                            Text(
                              'Todo lo que necesitás para administrar tu gimnasio, en un solo lugar.',
                              style: Theme.of(context).textTheme.bodyLarge
                                  ?.copyWith(color: AppTheme.textSecondary),
                            ),
                            const SizedBox(height: 20),
                            const StatusBadge(
                              label: 'Sistema operativo',
                              color: AppTheme.primaryGreen,
                              icon: Icons.check_circle_outline,
                            ),
                          ],
                        ),
                      ),
                      if (constraints.maxWidth >= 700) ...[
                        const SizedBox(width: 30),
                        Icon(
                          Icons.monitor_heart_outlined,
                          color: AppTheme.primaryGreen.withValues(alpha: .8),
                          size: 82,
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: 30),
                const SectionHeader(
                  title: 'Panel de gestión',
                  subtitle: 'Elegí un módulo para comenzar',
                ),
                const SizedBox(height: 18),
                GridView.builder(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: visibleItems.length,
                  gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: columns,
                    crossAxisSpacing: 16,
                    mainAxisSpacing: 16,
                    childAspectRatio: columns == 1 ? 2.25 : 1.55,
                  ),
                  itemBuilder: (context, index) {
                    final item = visibleItems[index];
                    return DashboardCard(
                      icon: item.icon,
                      title: item.title,
                      onTap: () => Navigator.pushNamed(context, item.route),
                    );
                  },
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
