class ApiConstants {
  // Producción debe compilarse con:
  // --dart-define=API_BASE_URL=https://<backend>
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5211',
  );

  static const String login = "/api/auth/login";
  static const String me = "/api/users/me";
  static const String users = "/api/users";
  static String changeRol(String id) => "/api/users/$id/rol";
  static const String crearEmpleado = "/api/users/crear-empleado";
  static const String sucursales = "/api/sucursales";
}
