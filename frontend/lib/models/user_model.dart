import 'rol_enum.dart';

class UserModel {
  final String id;
  final String nombre;
  final String email;
  final Rol rol;

  UserModel({
    required this.id,
    required this.nombre,
    required this.email,
    required this.rol,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    // backend puede usar 'name' o 'nombre' y 'role' o 'rol'
    final id = json['_id']?.toString() ?? json['id']?.toString() ?? '';
    final nombre = json['nombre'] ?? json['name'] ?? '';
    final email = json['email'] ?? '';
    final rolRaw = json['rol'] ?? json['rol'] ?? json['rolName'] ?? '';
    return UserModel(
      id: id,
      nombre: nombre.toString(),
      email: email.toString(),
      rol: rolFromString(rolRaw?.toString()),
    );
  }
}
