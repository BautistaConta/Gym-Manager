import 'package:flutter/material.dart';
import '/../models/dashboard_item.dart'; 
import '/../models/rol_enum.dart'; 
import '../../routes/app_routes.dart';

final dashboardItems = [
   DashboardItem(
    title: 'Gestión de usuarios',
    icon: Icons.people_outline,
    route: AppRoutes.gestionUsuarios,
    allowedRoles: [Rol.admin, Rol.gestor],
  ),
  DashboardItem(
  title: 'Sucursales',
  icon: Icons.store,
  route: AppRoutes.gestionSucursales,
  allowedRoles: [Rol.admin, Rol.gestor],
),
DashboardItem(
  title: 'Alumnos',
  icon: Icons.school,
  route: AppRoutes.gestionAlumnos,
  allowedRoles: [Rol.admin, Rol.gestor],
),
DashboardItem(
  title: 'Categorías de pago',
  icon: Icons.payment,
  route: AppRoutes.gestionCategoriasPago,
  allowedRoles: [Rol.admin, Rol.gestor],
),
DashboardItem(
  title: 'Cobrar abono',
  icon: Icons.point_of_sale,
  route: AppRoutes.cobrarAbono,
  allowedRoles: [Rol.admin, Rol.gestor],
),
  // DashboardItem(
  //   title: 'Clases',
  //   icon: Icons.fitness_center,
  //   route: AppRoutes.clases,
  //   allowedRoles: [Rol.Profesor],
  // ),
  // DashboardItem(
  //   title: 'Pagos',
  //   icon: Icons.payment,
  //   route: AppRoutes.pagos,
  //   allowedRoles: [Rol.Admin],
  // ),
  // DashboardItem(
  //   title: 'Mi perfil',
  //   icon: Icons.person_outline,
  //   route: AppRoutes.perfil,
  //   allowedRoles: [Rol.Alumno],
  // ),
];
