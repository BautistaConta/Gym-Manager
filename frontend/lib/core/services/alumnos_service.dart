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

  Future<List<AlumnoModel>> search(String nombre) async {
    final token = await _authService.getToken();
    final response = await http.get(Uri.parse('${ApiConstants.baseUrl}/api/alumnos/search?nombre=${Uri.encodeQueryComponent(nombre)}'), headers: {'Authorization': 'Bearer $token'});
    if (response.statusCode != 200) throw Exception('Error buscando alumnos');
    return (jsonDecode(response.body) as List).map((e) => AlumnoModel.fromJson(e)).toList();
  }

  Future<AlumnoModel?> getByDni(String dni) async {
    final token = await _authService.getToken();
    final response = await http.get(Uri.parse('${ApiConstants.baseUrl}/api/alumnos/dni/${Uri.encodeComponent(dni)}'), headers: {'Authorization': 'Bearer $token'});
    if (response.statusCode == 404) return null;
    if (response.statusCode != 200) throw Exception('Error buscando alumno');
    return AlumnoModel.fromJson(jsonDecode(response.body));
  }

  Future<void> updateAlumno(String id, {required String nombre, required String telefono, required bool activo}) async {
    final token = await _authService.getToken();
    final response = await http.put(Uri.parse('${ApiConstants.baseUrl}/api/alumnos/$id'), headers: {'Content-Type': 'application/json', 'Authorization': 'Bearer $token'}, body: jsonEncode({'nombre': nombre, 'telefono': telefono, 'activo': activo}));
    if (response.statusCode != 200) throw Exception('No se pudo actualizar el alumno: ${response.body}');
  }

  Future<void> deactivateAlumno(String id) async {
    final token = await _authService.getToken();
    final response = await http.delete(Uri.parse('${ApiConstants.baseUrl}/api/alumnos/$id'), headers: {'Authorization': 'Bearer $token'});
    if (response.statusCode != 204) throw Exception('No se pudo desactivar el alumno: ${response.body}');
  }
}
