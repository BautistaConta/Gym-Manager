import 'package:flutter/material.dart';
import '../features/auth/login_page.dart';
import '../features/auth/register_page.dart';
import '../features/common/home_screen.dart';
import '../features/Admin/gestion_usuarios_screen.dart';
import '../features/Admin/gestion_sucursales_screen.dart';

class AppRoutes {
  static const String login = '/login';
  static const String register = '/register';
  static const String home = '/home';
  static const String gestionUsuarios = '/admin/gestion-usuarios';
  static const String gestionSucursales = '/admin/gestion-sucursales';

  static Map<String, WidgetBuilder> routes = {
    login: (_) => const LoginPage(),
    register: (_) => const RegisterPage(),
    home: (_) => const HomeScreen(),
    gestionUsuarios: (_) => const GestionUsuariosScreen(),
    gestionSucursales: (_) => const GestionSucursalesScreen(),
  };
}
