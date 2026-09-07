class PagoModel {
  final String id;
  final String alumnoId;
  final String sucursalId;
  final String categoriaPagoId;
  final String alumnoNombre;
  final String alumnoDni;
  final String sucursalNombre;
  final String categoriaPagoNombre;
  final DateTime fechaPago;
  final DateTime periodoDesde;
  final DateTime periodoHasta;
  final double descuentoPorcentaje;
  final double precioCategoria;
  final double montoFinal;
  final String metodoPago;

  PagoModel({
    required this.id,
    required this.alumnoId,
    required this.sucursalId,
    required this.categoriaPagoId,
    required this.alumnoNombre,
    required this.alumnoDni,
    required this.sucursalNombre,
    required this.categoriaPagoNombre,
    required this.fechaPago,
    required this.periodoDesde,
    required this.periodoHasta,
    required this.descuentoPorcentaje,
    required this.precioCategoria,
    required this.montoFinal,
    required this.metodoPago,
  });

  factory PagoModel.fromJson(Map<String, dynamic> json) => PagoModel(
    id: json['id']?.toString() ?? '',
    alumnoId: json['alumnoId'].toString(),
    sucursalId: json['sucursalId'].toString(),
    categoriaPagoId: json['categoriaPagoId'].toString(),
    alumnoNombre: json['alumnoNombre']?.toString() ?? 'Alumno',
    alumnoDni: json['alumnoDni']?.toString() ?? '-',
    sucursalNombre: json['sucursalNombre']?.toString() ?? 'Sucursal',
    categoriaPagoNombre: json['categoriaPagoNombre']?.toString() ?? 'Categoría',
    fechaPago: json['fechaPago'] == null
        ? DateTime.now()
        : DateTime.parse(json['fechaPago']),
    periodoDesde: DateTime.parse(json['periodoDesde']),
    periodoHasta: DateTime.parse(json['periodoHasta']),
    descuentoPorcentaje: (json['descuentoPorcentaje'] ?? 0).toDouble(),
    precioCategoria: (json['precioCategoria'] ?? 0).toDouble(),
    montoFinal: (json['montoFinal'] ?? 0).toDouble(),
    metodoPago: json['metodoPago']?.toString() ?? '',
  );
}
