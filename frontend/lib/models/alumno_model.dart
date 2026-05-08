class AlumnoModel {
  final String id;
  final String nombre;
  final String dni;
  final String telefono;
  final bool activo;

  AlumnoModel({
    required this.id,
    required this.nombre,
    required this.dni,
    required this.telefono,
    required this.activo,
  });

  factory AlumnoModel.fromJson(Map<String, dynamic> json) {
    return AlumnoModel(
      id: json['_id']?.toString() ?? json['id']?.toString() ?? '',
      nombre: json['nombre'] ?? '',
      dni: json['dni'] ?? '',
      telefono: json['telefono'] ?? '',
      activo: json['activo'] ?? true,
    );
  }
}