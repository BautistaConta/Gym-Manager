import 'package:flutter/material.dart';
import '../../routes/app_routes.dart';
import '../../widgets/dashboard_card.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Gym Manager'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 10),

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

            // GRID DASHBOARD
            Expanded(
              child: GridView.count(
                crossAxisCount: 2,
                crossAxisSpacing: 16,
                mainAxisSpacing: 16,
                childAspectRatio: 1,
                children: [
                  DashboardCard(
                    icon: Icons.people_outline,
                    title: 'Gestión de usuarios',
                    onTap: () {
                      Navigator.pushNamed(
                        context,
                        AppRoutes.gestionUsuarios,
                      );
                    },
                  ),

                  DashboardCard(
                    icon: Icons.fitness_center,
                    title: 'Clases',
                    onTap: () {
                      // futura pantalla
                    },
                  ),

                  DashboardCard(
                    icon: Icons.payment,
                    title: 'Pagos',
                    onTap: () {
                      // futura pantalla
                    },
                  ),

                  DashboardCard(
                    icon: Icons.settings,
                    title: 'Configuración',
                    onTap: () {
                      // futura pantalla
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
