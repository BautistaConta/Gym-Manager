class PagoModel {
  final String id;
  final String alumnoId;
  final String sucursalId;
  final String categoriaPagoId;
  final DateTime periodoDesde;
  final DateTime periodoHasta;
  final double descuentoPorcentaje;
  final double precioCategoria;
  final double montoFinal;
  final String metodoPago;

  PagoModel({required this.id, required this.alumnoId, required this.sucursalId, required this.categoriaPagoId, required this.periodoDesde, required this.periodoHasta, required this.descuentoPorcentaje, required this.precioCategoria, required this.montoFinal, required this.metodoPago});

  factory PagoModel.fromJson(Map<String, dynamic> json) => PagoModel(
    id: json['id']?.toString() ?? '', alumnoId: json['alumnoId'].toString(), sucursalId: json['sucursalId'].toString(), categoriaPagoId: json['categoriaPagoId'].toString(),
    periodoDesde: DateTime.parse(json['periodoDesde']), periodoHasta: DateTime.parse(json['periodoHasta']), descuentoPorcentaje: (json['descuentoPorcentaje'] ?? 0).toDouble(),
    precioCategoria: (json['precioCategoria'] ?? 0).toDouble(), montoFinal: (json['montoFinal'] ?? 0).toDouble(), metodoPago: json['metodoPago']?.toString() ?? '',
  );
}
