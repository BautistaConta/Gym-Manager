class SucursalModel {
  final String id;
  final String nombre;
  final String direccion;

  SucursalModel({
    required this.id,
    required this.nombre,
    required this.direccion,
  });

  factory SucursalModel.fromJson(Map<String, dynamic> json) {
    return SucursalModel(
      id: json['_id']?.toString() ?? json['id']?.toString() ?? '',
      nombre: json['nombre'] ?? '',
      direccion: json['direccion'] ?? '',
    );
  }
}