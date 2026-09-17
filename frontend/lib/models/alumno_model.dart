class AlumnoModel {
  final String id;
  final String nombre;
  final String dni;
  final String telefono;
  final bool activo;
  final bool notificacionesHabilitadas;
  final DateTime? fechaConsentimientoWhatsApp;
  final DateTime? fechaRevocacionWhatsApp;
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
    this.fechaConsentimientoWhatsApp,
    this.fechaRevocacionWhatsApp,
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
      fechaConsentimientoWhatsApp:
          json['fechaConsentimientoWhatsApp'] == null
          ? null
          : DateTime.parse(
              json['fechaConsentimientoWhatsApp'].toString(),
            ),
      fechaRevocacionWhatsApp: json['fechaRevocacionWhatsApp'] == null
          ? null
          : DateTime.parse(json['fechaRevocacionWhatsApp'].toString()),
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
