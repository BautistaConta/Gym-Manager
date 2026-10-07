import 'dart:convert';

import 'package:http/http.dart' as http;

import '../constants/api_constants.dart';
import 'auth_service.dart';

class NotificacionItem {
  final String id;
  final String alumno;
  final String telefono;
  final String tipo;
  final String estado;
  final String? sucursal;
  final String? providerMessageId;
  final String correlationId;
  final String? error;
  final DateTime fechaCreacion;
  final DateTime? fechaVencimiento;
  final DateTime? fechaEnvio;
  final int intentos;
  final bool puedeReintentar;

  const NotificacionItem({
    required this.id,
    required this.alumno,
    required this.telefono,
    required this.tipo,
    required this.estado,
    required this.fechaCreacion,
    required this.intentos,
    required this.puedeReintentar,
    required this.correlationId,
    this.sucursal,
    this.providerMessageId,
    this.error,
    this.fechaVencimiento,
    this.fechaEnvio,
  });

  factory NotificacionItem.fromJson(Map<String, dynamic> json) =>
      NotificacionItem(
        id: json['id'].toString(),
        alumno: json['alumno'].toString(),
        telefono: json['telefono'].toString(),
        tipo: _tipo(json['tipo']),
        estado: json['estadoDescripcion'].toString(),
        sucursal: json['sucursal']?.toString(),
        providerMessageId: json['providerMessageId']?.toString(),
        correlationId: json['correlationId']?.toString() ?? '-',
        error: json['errorResumen']?.toString(),
        fechaCreacion: DateTime.parse(json['fechaCreacion'].toString()),
        fechaVencimiento: json['fechaVencimiento'] == null
            ? null
            : DateTime.parse(json['fechaVencimiento'].toString()),
        fechaEnvio: json['fechaEnvio'] == null
            ? null
            : DateTime.parse(json['fechaEnvio'].toString()),
        intentos: json['intentos'] as int? ?? 0,
        puedeReintentar: json['puedeReintentar'] == true,
      );

  static String _tipo(dynamic value) => switch (value) {
    0 => 'Por vencer',
    1 => 'Vencido (histórico)',
    3 => 'Bienvenida',
    4 => 'Promoción',
    5 => 'Aviso general',
    _ => 'Otro',
  };
}

class PilotEventCount {
  final DateTime diaUtc;
  final String tipo;
  final int cantidad;

  const PilotEventCount(this.diaUtc, this.tipo, this.cantidad);

  factory PilotEventCount.fromJson(Map<String, dynamic> json) =>
      PilotEventCount(
        DateTime.parse(json['diaUtc'].toString()),
        json['tipo'].toString(),
        json['cantidad'] as int,
      );
}

class PilotUsageSummary {
  final DateTime desdeUtc;
  final DateTime hastaUtc;
  final List<PilotEventCount> conteos;

  const PilotUsageSummary(this.desdeUtc, this.hastaUtc, this.conteos);

  int totalFor(Iterable<String> types) => conteos
      .where((item) => types.contains(item.tipo))
      .fold(0, (total, item) => total + item.cantidad);

  factory PilotUsageSummary.fromJson(Map<String, dynamic> json) =>
      PilotUsageSummary(
        DateTime.parse(json['desdeUtc'].toString()),
        DateTime.parse(json['hastaUtc'].toString()),
        (json['conteos'] as List)
            .map((item) => PilotEventCount.fromJson(item))
            .toList(),
      );
}

class NotificacionPage {
  final List<NotificacionItem> items;
  final int total;
  final int pagina;
  final int totalPaginas;
  final Map<String, dynamic> contadores;

  const NotificacionPage(
    this.items,
    this.total,
    this.pagina,
    this.totalPaginas,
    this.contadores,
  );

  factory NotificacionPage.fromJson(Map<String, dynamic> json) =>
      NotificacionPage(
        (json['items'] as List)
            .map((item) => NotificacionItem.fromJson(item))
            .toList(),
        json['total'] as int,
        json['pagina'] as int,
        json['totalPaginas'] as int,
        json['contadores'] as Map<String, dynamic>,
      );
}

class NotificacionesService {
  final AuthService _auth = AuthService();

  Future<Map<String, String>> _headers() async => {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ${await _auth.getToken()}',
  };

  Future<NotificacionPage> getPage({
    int pagina = 1,
    int? estado,
    int? tipo,
    String? alumnoId,
    DateTime? desde,
    DateTime? hasta,
  }) async {
    final query = <String, String>{'pagina': '$pagina', 'tamanoPagina': '20'};
    if (estado != null) query['estado'] = '$estado';
    if (tipo != null) query['tipo'] = '$tipo';
    if (alumnoId != null && alumnoId.isNotEmpty) query['alumnoId'] = alumnoId;
    if (desde != null) query['desdeUtc'] = desde.toUtc().toIso8601String();
    if (hasta != null) query['hastaUtc'] = hasta.toUtc().toIso8601String();

    final response = await http.get(
      Uri.parse(
        '${ApiConstants.baseUrl}/api/notificaciones',
      ).replace(queryParameters: query),
      headers: await _headers(),
    );
    if (response.statusCode != 200) throw Exception(_error(response));
    return NotificacionPage.fromJson(jsonDecode(response.body));
  }

  Future<void> retry(String id) async {
    final response = await http.post(
      Uri.parse('${ApiConstants.baseUrl}/api/notificaciones/reenviar/$id'),
      headers: await _headers(),
      body: jsonEncode({'confirmacionExplicita': true}),
    );
    if (response.statusCode != 200) throw Exception(_error(response));
  }

  Future<PilotUsageSummary?> getPilotSummary() async {
    final response = await http.get(
      Uri.parse('${ApiConstants.baseUrl}/api/admin/pilot-events/resumen'),
      headers: await _headers(),
    );
    if (response.statusCode == 403) return null;
    if (response.statusCode != 200) throw Exception(_error(response));
    return PilotUsageSummary.fromJson(jsonDecode(response.body));
  }

  String _error(http.Response response) {
    try {
      final json = jsonDecode(response.body);
      return json['message']?.toString() ?? 'Error consultando notificaciones';
    } catch (_) {
      return 'Error consultando notificaciones';
    }
  }
}
