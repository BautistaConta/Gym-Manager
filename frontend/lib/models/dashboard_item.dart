import 'package:flutter/material.dart';
import '../../models/rol_enum.dart';

class DashboardItem {
  final String title;
  final IconData icon;
  final String route;
  final List<Rol> allowedRoles;

  const DashboardItem({
    required this.title,
    required this.icon,
    required this.route,
    required this.allowedRoles,
  });
}

