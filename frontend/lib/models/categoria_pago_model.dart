class CategoriaPagoModel {
  final String id;
  final String nombre;
  final double precio;
  final int mesesDuracion;
  final int tipoAbono;
  final bool activa;

  CategoriaPagoModel({
    required this.id,
    required this.nombre,
    required this.precio,
    required this.mesesDuracion,
    required this.tipoAbono,
    required this.activa,
  });

  factory CategoriaPagoModel.fromJson(Map<String, dynamic> json) {
    return CategoriaPagoModel(
      id: json['id'] ?? json['_id'] ?? '',
      nombre: json['nombre'] ?? '',
      precio: (json['precio'] ?? 0).toDouble(),
      mesesDuracion: json['mesesDuracion'] ?? 1,
      tipoAbono: json['tipoAbono'] ?? 0,
      activa: json['activa'] ?? true,
    );
  }
}