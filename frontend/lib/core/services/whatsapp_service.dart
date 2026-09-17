import 'dart:convert';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import 'auth_service.dart';

class ConfiguracionWhatsApp {
  final String nombreComercial;
  final String? whatsappGroupInviteUrl;
  final String? textoInicialChat;
  const ConfiguracionWhatsApp({required this.nombreComercial, this.whatsappGroupInviteUrl, this.textoInicialChat});
  factory ConfiguracionWhatsApp.fromJson(Map<String, dynamic> json) => ConfiguracionWhatsApp(
    nombreComercial: json['nombreComercial']?.toString() ?? '',
    whatsappGroupInviteUrl: json['whatsAppGroupInviteUrl']?.toString(),
    textoInicialChat: json['textoInicialChat']?.toString(),
  );
}

class CampaniaWhatsAppModel {
  final String id, nombre, creador;
  final int estado, tipoPlantilla, tipoAudiencia, total, pendientes, aceptadas, fallidas, omitidas;
  final DateTime fechaCreacion;
  const CampaniaWhatsAppModel({required this.id, required this.nombre, required this.creador, required this.estado,
    required this.tipoPlantilla, required this.tipoAudiencia, required this.total, required this.pendientes,
    required this.aceptadas, required this.fallidas, required this.omitidas, required this.fechaCreacion});
  factory CampaniaWhatsAppModel.fromJson(Map<String, dynamic> j) => CampaniaWhatsAppModel(
    id: j['id']?.toString() ?? '', nombre: j['nombreInterno']?.toString() ?? '',
    creador: j['creadorNombre']?.toString() ?? 'Usuario', estado: j['estado'] as int? ?? 0,
    tipoPlantilla: j['tipoPlantilla'] as int? ?? 0, tipoAudiencia: j['tipoAudiencia'] as int? ?? 0,
    total: j['cantidadObjetivo'] as int? ?? 0, pendientes: j['pendientes'] as int? ?? 0,
    aceptadas: j['aceptadasPorTwilio'] as int? ?? 0, fallidas: j['fallidas'] as int? ?? 0,
    omitidas: j['omitidas'] as int? ?? 0, fechaCreacion: DateTime.parse(j['fechaCreacion'].toString()));
}

class PreviewCampania {
  final String token, contenido, audiencia, plantilla;
  final int destinatarios, omitidos;
  final List<dynamic> motivos;
  const PreviewCampania({required this.token, required this.contenido, required this.audiencia,
    required this.plantilla, required this.destinatarios, required this.omitidos, required this.motivos});
  factory PreviewCampania.fromJson(Map<String, dynamic> j) => PreviewCampania(
    token: j['previewToken'].toString(), contenido: j['contenidoEjemplo'].toString(),
    audiencia: j['audiencia'].toString(), plantilla: j['plantilla'].toString(),
    destinatarios: j['cantidadDestinatarios'] as int, omitidos: j['cantidadOmitidos'] as int,
    motivos: j['motivosOmision'] as List? ?? const []);
}

class WhatsAppService {
  final _auth = AuthService();
  Future<Map<String, String>> _headers() async => {'Content-Type': 'application/json', 'Authorization': 'Bearer ${await _auth.getToken()}'};
  Future<dynamic> _decode(http.Response r) async {
    final value = r.body.isEmpty ? null : jsonDecode(r.body);
    if (r.statusCode < 200 || r.statusCode >= 300) throw Exception(value is Map ? value['message'] ?? 'Error de WhatsApp' : 'Error de WhatsApp');
    return value;
  }
  Future<ConfiguracionWhatsApp> getConfiguracion() async => ConfiguracionWhatsApp.fromJson(
    await _decode(await http.get(Uri.parse('${ApiConstants.baseUrl}/api/configuracion/whatsapp'), headers: await _headers())));
  Future<void> guardarConfiguracion(ConfiguracionWhatsApp c) async => _decode(await http.put(
    Uri.parse('${ApiConstants.baseUrl}/api/configuracion/whatsapp'), headers: await _headers(),
    body: jsonEncode({'nombreComercial': c.nombreComercial, 'whatsAppGroupInviteUrl': c.whatsappGroupInviteUrl, 'textoInicialChat': c.textoInicialChat})));
  Future<List<CampaniaWhatsAppModel>> getCampanias() async => ((await _decode(await http.get(
    Uri.parse('${ApiConstants.baseUrl}/api/campanias-whatsapp'), headers: await _headers()))) as List)
    .map((e) => CampaniaWhatsAppModel.fromJson(e)).toList();
  Future<String> crearCampania({required String nombre, required int plantilla, required Map<String,String> variables,
    required int audiencia, String? sucursalId, List<String> alumnoIds = const []}) async {
    final j = await _decode(await http.post(Uri.parse('${ApiConstants.baseUrl}/api/campanias-whatsapp'), headers: await _headers(),
      body: jsonEncode({'nombreInterno': nombre, 'tipoPlantilla': plantilla, 'variables': variables,
        'tipoAudiencia': audiencia, 'sucursalPrincipalId': sucursalId, 'alumnoIds': alumnoIds})));
    return j['id'].toString();
  }
  Future<PreviewCampania> preview(String id) async => PreviewCampania.fromJson(await _decode(await http.post(
    Uri.parse('${ApiConstants.baseUrl}/api/campanias-whatsapp/$id/preview'), headers: await _headers())));
  Future<void> confirmar(String id, PreviewCampania p) async => _decode(await http.post(
    Uri.parse('${ApiConstants.baseUrl}/api/campanias-whatsapp/$id/confirmar'), headers: await _headers(),
    body: jsonEncode({'previewToken': p.token, 'cantidadConfirmada': p.destinatarios, 'confirmacionExplicita': true})));
  Future<void> cancelar(String id) async => _decode(await http.post(
    Uri.parse('${ApiConstants.baseUrl}/api/campanias-whatsapp/$id/cancelar'), headers: await _headers()));
}
