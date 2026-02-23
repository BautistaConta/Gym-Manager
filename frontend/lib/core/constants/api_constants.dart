class ApiConstants {
  static const String baseUrl = "http://localhost:5211"; // backend local

  static const String login = "/api/auth/login";
  static const String register = "/api/auth/register";
  static const String me = "/api/users/me";
  static const String users = "/api/users";
  static String changeRol(String id) => "/api/users/$id/rol";
  static const String crearEmpleado = "/api/users/crear-empleado";
  static const String sucursales = "/api/sucursales";
  
}
