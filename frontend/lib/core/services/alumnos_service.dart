import 'dart:convert';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import '../../models/alumno_model.dart';
import '../../models/estado_alumno.dart';
import 'auth_service.dart';

class AlumnosService {
  final AuthService _authService = AuthService();

  Future<List<AlumnoModel>> fetchAll() async {
    final token = await _authService.getToken();
    final url = Uri.parse('${ApiConstants.baseUrl}/api/alumnos');

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode != 200) {
      throw Exception('Error al obtener alumnos');
    }

    final List data = jsonDecode(response.body);
    return data.map((e) => AlumnoModel.fromJson(e)).toList();
  }

  Future<void> createAlumno({
    required String nombre,
    required String dni,
    required String telefono,
  }) async {
    final token = await _authService.getToken();
    final url = Uri.parse('${ApiConstants.baseUrl}/api/alumnos');

    final response = await http.post(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'nombre': nombre,
        'dni': dni,
        'telefono': telefono,
      }),
    );

    if (response.statusCode != 201 && response.statusCode != 200) {
      final body = jsonDecode(response.body);
      throw Exception(body ?? 'Error al crear alumno');
    }
  }
  Future<EstadoAlumno> getEstado(String alumnoId) async {
  final token = await _authService.getToken();

  final url = Uri.parse(
    '${ApiConstants.baseUrl}/api/alumnos/$alumnoId/estado',
  );

  final response = await http.get(
    url,
    headers: {
      'Authorization': 'Bearer $token',
    },
  );

  if (response.statusCode != 200) {
    throw Exception('Error obteniendo estado');
  }

  return EstadoAlumno.fromJson(jsonDecode(response.body));
}
}