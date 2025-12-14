import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/providers/auth_provider.dart';
import '../../widgets/dashboard_card.dart';
import '../../widgets/dashboard_item.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
Widget build(BuildContext context, WidgetRef ref) {
  final authState = ref.watch(authProvider);

  if (authState.loading) {
    return const Scaffold(
      body: Center(child: CircularProgressIndicator()),
    );
  }

  final user = authState.user;
  if (user == null) {
    return const Scaffold(
      body: Center(child: Text('Usuario no autenticado')),
    );
  }

  final role = user.rol; 

  final visibleItems = dashboardItems
      .where((item) => item.allowedRoles.contains(role))
      .toList();


    return Scaffold(
      appBar: AppBar(
        title: const Text('Gym Manager'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Bienvenido',
              style: Theme.of(context).textTheme.headlineLarge,
            ),
            const SizedBox(height: 6),
            Text(
              'Panel principal del sistema',
              style: Theme.of(context)
                  .textTheme
                  .bodyMedium
                  ?.copyWith(color: Colors.grey),
            ),
            const SizedBox(height: 20),

            // Estado del sistema
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: const Color(0xFF1A1A1A),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: Theme.of(context).colorScheme.primary,
                  width: 1.5,
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Estado del sistema',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          color: Theme.of(context).colorScheme.primary,
                          fontWeight: FontWeight.bold,
                        ),
                  ),
                  const SizedBox(height: 8),
                  const Text('Todo funcionando correctamente'),
                ],
              ),
            ),

            const SizedBox(height: 30),

            Expanded(
              child: GridView.builder(
                itemCount: visibleItems.length,
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 16,
                  mainAxisSpacing: 16,
                ),
                itemBuilder: (context, index) {
                  final item = visibleItems[index];
                  return DashboardCard(
                    icon: item.icon,
                    title: item.title,
                    onTap: () {
                      Navigator.pushNamed(context, item.route);
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
