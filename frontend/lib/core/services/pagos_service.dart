import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../models/pago_model.dart';
import '../constants/api_constants.dart';
import 'auth_service.dart';

class PagosService {
  final AuthService _authService = AuthService();
  Future<List<PagoModel>> fetchAll() async {
    final token = await _authService.getToken();
    final response = await http.get(
      Uri.parse('${ApiConstants.baseUrl}/api/pagos'),
      headers: {'Authorization': 'Bearer $token'},
    );
    if (response.statusCode != 200)
      throw Exception(
        _errorMessage(response, 'No se pudieron obtener los pagos.'),
      );
    return (jsonDecode(response.body) as List)
        .map((item) => PagoModel.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<String?> enviarRecordatorio(String pagoId) async {
    final token = await _authService.getToken();
    final response = await http.post(
      Uri.parse('${ApiConstants.baseUrl}/api/pagos/$pagoId/recordatorio'),
      headers: {'Authorization': 'Bearer $token'},
    );
    if (response.statusCode != 200)
      throw Exception(
        _errorMessage(response, 'No se pudo enviar el recordatorio.'),
      );
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    final estado = body['estado'];
    return estado == 1 || estado == 'Enviado'
        ? null
        : body['errorDetalle']?.toString() ??
              'Twilio no pudo enviar el recordatorio.';
  }

  Future<PagoModel> registrar({
    required String alumnoId,
    required String sucursalId,
    required String categoriaPagoId,
    required int metodoPago,
    required double descuentoPorcentaje,
    DateTime? periodoHastaManual,
  }) async {
    if (alumnoId.isEmpty || sucursalId.isEmpty || categoriaPagoId.isEmpty) {
      throw Exception(
        'El alumno, la sucursal y la categoría deben tener un identificador válido. Creá nuevamente cualquier registro antiguo que tenga ID vacío.',
      );
    }
    final token = await _authService.getToken();
    final response = await http.post(
      Uri.parse('${ApiConstants.baseUrl}/api/pagos'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'alumnoId': alumnoId,
        'sucursalId': sucursalId,
        'categoriaPagoId': categoriaPagoId,
        'metodoPago': metodoPago,
        'descuentoPorcentaje': descuentoPorcentaje,
        if (periodoHastaManual != null)
          'periodoHastaManual': periodoHastaManual.toUtc().toIso8601String(),
      }),
    );
    if (response.statusCode != 200) {
      final decoded = jsonDecode(response.body);
      if (decoded is Map && decoded['errors'] is Map) {
        final errors = (decoded['errors'] as Map).values
            .expand((value) => value is List ? value : [value])
            .join(' ');
        throw Exception(errors.isEmpty ? 'Datos de pago inválidos.' : errors);
      }
      throw Exception(
        decoded is Map
            ? decoded['message'] ??
                  decoded['title'] ??
                  'No se pudo registrar el pago.'
            : 'No se pudo registrar el pago.',
      );
    }
    return PagoModel.fromJson(jsonDecode(response.body));
  }

  String _errorMessage(http.Response response, String fallback) {
    try {
      final body = jsonDecode(response.body);
      return body is Map
          ? body['message']?.toString() ?? body['title']?.toString() ?? fallback
          : fallback;
    } catch (_) {
      return fallback;
    }
  }
}
