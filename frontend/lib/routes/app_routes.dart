import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/providers/auth_provider.dart';
import '../features/auth/login_page.dart';
import '../features/common/home_screen.dart';
import '../features/Admin/gestion_usuarios_screen.dart';
import '../features/Admin/gestion_sucursales_screen.dart';
import '../features/Admin/gestion_alumnos_screen.dart';
import '../features/Admin/gestion_categorias_pago_screen.dart';
import '../features/Admin/cobrar_abono_screen.dart';
import '../features/Admin/gestion_pagos_screen.dart';
import '../features/Admin/campanias_whatsapp_screen.dart';
import '../features/Admin/configuracion_whatsapp_screen.dart';
import '../models/rol_enum.dart';

class AppRoutes {
  static const String login = '/login';
  static const String home = '/home';
  static const String gestionUsuarios = '/admin/gestion-usuarios';
  static const String gestionSucursales = '/admin/gestion-sucursales';
  static const String gestionAlumnos = '/admin/gestion-alumnos';
  static const String gestionCategoriasPago = '/admin/gestion-categorias-pago';
  static const String cobrarAbono = '/admin/cobrar-abono';
  static const String pagos = '/admin/pagos';
  static const String campaniasWhatsApp = '/admin/campanias-whatsapp';
  static const String configuracionWhatsApp = '/admin/configuracion-whatsapp';

  static final Map<String, WidgetBuilder> _routes = {
    login: (_) => const LoginPage(),
    home: (_) => const HomeScreen(),
    gestionUsuarios: (_) => const GestionUsuariosScreen(),
    gestionSucursales: (_) => const GestionSucursalesScreen(),
    gestionAlumnos: (_) => const GestionAlumnosScreen(),
    gestionCategoriasPago: (_) => const GestionCategoriasPagoScreen(),
    cobrarAbono: (_) => const CobrarAbonoScreen(),
    pagos: (_) => const GestionPagosScreen(),
    campaniasWhatsApp: (_) => const CampaniasWhatsAppScreen(),
    configuracionWhatsApp: (_) => const ConfiguracionWhatsAppScreen(),
  };

  static final Map<String, Set<Rol>> _allowedRoles = {
    home: {Rol.admin, Rol.gestor},
    gestionUsuarios: {Rol.admin},
    gestionSucursales: {Rol.admin, Rol.gestor},
    gestionAlumnos: {Rol.admin, Rol.gestor},
    gestionCategoriasPago: {Rol.admin, Rol.gestor},
    cobrarAbono: {Rol.admin, Rol.gestor},
    pagos: {Rol.admin, Rol.gestor},
    campaniasWhatsApp: {Rol.admin, Rol.gestor},
    configuracionWhatsApp: {Rol.admin},
  };

  static Route<dynamic> onGenerateRoute(RouteSettings settings) {
    final routeName = settings.name ?? login;
    final pageBuilder = _routes[routeName] ?? _routes[login]!;
    final allowedRoles = _allowedRoles[routeName];

    return MaterialPageRoute(
      settings: settings,
      builder: (context) => allowedRoles == null
          ? pageBuilder(context)
          : _SessionGuard(
              allowedRoles: allowedRoles,
              child: Builder(builder: pageBuilder),
            ),
    );
  }
}

class _SessionGuard extends ConsumerWidget {
  final Set<Rol> allowedRoles;
  final Widget child;

  const _SessionGuard({required this.allowedRoles, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider);
    if (auth.loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (!auth.isAuthenticated) return const LoginPage();
    if (!allowedRoles.contains(auth.user!.rol)) {
      return Scaffold(
        appBar: AppBar(title: const Text('Acceso restringido')),
        body: Center(
          child: FilledButton(
            onPressed: () => Navigator.pushNamedAndRemoveUntil(
              context,
              AppRoutes.home,
              (_) => false,
            ),
            child: const Text('Volver al inicio'),
          ),
        ),
      );
    }
    return child;
  }
}
