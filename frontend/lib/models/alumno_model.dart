class AlumnoModel {
  final String id;
  final String nombre;
  final String dni;
  final String telefono;
  final bool activo;
  final bool notificacionesHabilitadas;
  final DateTime? fechaConsentimientoNotificacionesUtc;
  final String? medioConsentimientoNotificaciones;
  final String? sucursalPrincipalId;
  final String? estado;
  final DateTime? fechaVencimiento;

  AlumnoModel({
    required this.id,
    required this.nombre,
    required this.dni,
    required this.telefono,
    required this.activo,
    this.notificacionesHabilitadas = false,
    this.fechaConsentimientoNotificacionesUtc,
    this.medioConsentimientoNotificaciones,
    this.sucursalPrincipalId,
    this.estado,
    this.fechaVencimiento,
  });

  factory AlumnoModel.fromJson(Map<String, dynamic> json) {
    return AlumnoModel(
      id: json['_id']?.toString() ?? json['id']?.toString() ?? '',
      nombre: json['nombre'] ?? '',
      dni: json['dni'] ?? '',
      telefono: json['telefono'] ?? '',
      activo: json['activo'] ?? true,
      notificacionesHabilitadas: json['notificacionesHabilitadas'] == true,
      fechaConsentimientoNotificacionesUtc:
          json['fechaConsentimientoNotificacionesUtc'] == null
          ? null
          : DateTime.parse(
              json['fechaConsentimientoNotificacionesUtc'].toString(),
            ),
      medioConsentimientoNotificaciones:
          json['medioConsentimientoNotificaciones']?.toString(),
      sucursalPrincipalId: json['sucursalPrincipalId']?.toString(),
      estado: json['estado']?.toString(),
      fechaVencimiento: json['fechaVencimiento'] == null
          ? null
          : DateTime.parse(json['fechaVencimiento'].toString()),
    );
  }
}
