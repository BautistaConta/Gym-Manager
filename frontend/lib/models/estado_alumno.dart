class EstadoAlumno {
  final String alumnoId;
  final String estado;
  final DateTime? fechaVencimiento;

  EstadoAlumno({
    required this.alumnoId,
    required this.estado,
    this.fechaVencimiento,
  });

  factory EstadoAlumno.fromJson(Map<String, dynamic> json) {
    return EstadoAlumno(
      alumnoId: json['alumnoId'],
      estado: json['estado'],
      fechaVencimiento: json['fechaVencimiento'] != null
          ? DateTime.parse(json['fechaVencimiento'])
          : null,
    );
  }
}