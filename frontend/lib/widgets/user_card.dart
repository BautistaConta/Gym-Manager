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
      margin: const EdgeInsets.symmetric(vertical: 6, horizontal: 10),
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Row(
          children: [
            Expanded(
              child: ListTile(
                title: Text(user.nombre),
                subtitle: Text(user.email),
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text('Rol actual: ${rolToString(user.rol)}'),
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
