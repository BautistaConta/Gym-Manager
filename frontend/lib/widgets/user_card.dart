import 'package:flutter/material.dart';
import '../models/user_model.dart';
import '../models/rol_enum.dart';
import 'rol_dropdown.dart';

class UserCard extends StatelessWidget {
  final UserModel user;
  final Future<bool> Function(String newRol) onChangeRol;

  const UserCard({
    super.key,
    required this.user,
    required this.onChangeRol,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(
          color: Theme.of(context).colorScheme.primary,
          width: 1.2,
        ),
      ),
      color: const Color(0xFF1A1A1A),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Info usuario
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    user.nombre,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    user.email,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: Colors.grey,
                        ),
                  ),
                ],
              ),
            ),

            // Rol + acción
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  'Rol actual',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Colors.grey,
                      ),
                ),
                Text(
                  rolToString(user.rol),
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: Theme.of(context).colorScheme.primary,
                        fontWeight: FontWeight.bold,
                      ),
                ),
                const SizedBox(height: 8),
                RolDropdown(
                  currentRole: user.rol,
                  onRoleSelected: (newRol) => onChangeRol(newRol),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
