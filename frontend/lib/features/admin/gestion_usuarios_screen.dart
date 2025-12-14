import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/services/users_service.dart';
import '../../models/user_model.dart';
import '../../models/rol_enum.dart';
import '../../widgets/user_card.dart';
import '../../core/providers/auth_provider.dart';

class GestionUsuariosScreen extends ConsumerStatefulWidget {
  const GestionUsuariosScreen({super.key});

  @override
  ConsumerState<GestionUsuariosScreen> createState() => _GestionUsuariosScreenState();
}

class _GestionUsuariosScreenState extends ConsumerState<GestionUsuariosScreen> {
  final UsersService _usersService = UsersService();
  bool loading = true;
  String? error;
  List<UserModel> users = [];

  @override
  void initState() {
    super.initState();
    _loadUsers();
  }

  Future<void> _loadUsers() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final fetched = await _usersService.fetchAllUsers();
      setState(() {
        users = fetched;
      });
    } on Exception catch (e) {
      setState(() {
        error = e.toString();
      });
      // Si 401/403: podés hacer logout
      if (e is HttpException && (e.statusCode == 401 || e.statusCode == 403)) {
        // Forzar logout
        ref.read(authProvider.notifier).logout();
      }
    } finally {
      setState(() {
        loading = false;
      });
    }
  }

  Future<bool> _onChangeRol(String userId, String newRol) async {
    final success = await _usersService.changeUserRol(userId, newRol);
    if (success) {
      // actualizar lista local inmediatamente
      setState(() {
        users = users.map((u) {
          if (u.id == userId) {
            return UserModel(
                id: u.id, nombre: u.nombre, email: u.email, rol: rolFromString(newRol));
          }
          return u;
        }).toList();
      });
      // refrescar user actual en caso de que cambien rol a quien está logueado
      await ref.read(authProvider.notifier).refreshUser();
    }
    return success;
  }

  @override
Widget build(BuildContext context) {
  final authState = ref.watch(authProvider);

  if (authState.loading) {
    return const Scaffold(
      body: Center(child: CircularProgressIndicator()),
    );
  }

  // Protección extra: solo admin 
  return Scaffold(
    appBar: AppBar(
      title: const Text('Gestión de Usuarios'),
      actions: [
        IconButton(
          onPressed: _loadUsers,
          icon: const Icon(Icons.refresh),
          tooltip: 'Refrescar',
        ),
      ],
    ),
    body: loading
        ? const Center(
            child: CircularProgressIndicator(),
          )
        : error != null
            ? Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text(
                    'Error: $error',
                    textAlign: TextAlign.center,
                    style: Theme.of(context)
                        .textTheme
                        .bodyMedium
                        ?.copyWith(color: Colors.redAccent),
                  ),
                ),
              )
            : RefreshIndicator(
                onRefresh: _loadUsers,
                child: ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: users.length,
                  itemBuilder: (context, i) {
                    final u = users[i];
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: UserCard(
                        user: u,
                        onChangeRol: (newRole) =>
                            _onChangeRol(u.id, newRole),
                      ),
                    );
                  },
                ),
              ),
  );
}
}