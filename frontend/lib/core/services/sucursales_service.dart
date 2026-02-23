import 'dart:convert';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import '../../models/sucursal_model.dart';
import '../services/auth_service.dart';

class SucursalesService {
  final AuthService _authService = AuthService();

  Future<List<SucursalModel>> fetchAll() async {
    final token = await _authService.getToken();
    final url = Uri.parse(ApiConstants.baseUrl + ApiConstants.sucursales);

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode != 200) {
      throw Exception('Error al obtener sucursales');
    }

    final List data = jsonDecode(response.body);
    return data.map((e) => SucursalModel.fromJson(e)).toList();
  }

  Future<void> createSucursal({
    required String nombre,
    required String direccion,
  }) async {
    final token = await _authService.getToken();
    final url = Uri.parse(ApiConstants.baseUrl + ApiConstants.sucursales);

    final response = await http.post(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'nombre': nombre,
        'direccion': direccion,
      }),
    );

    if (response.statusCode != 200) {
      throw Exception('Error al crear sucursal');
    }
  }
}