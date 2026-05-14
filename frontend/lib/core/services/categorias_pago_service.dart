import 'dart:convert';
import 'package:http/http.dart' as http;

import '../../models/categoria_pago_model.dart';
import '../constants/api_constants.dart';
import 'auth_service.dart';

class CategoriasPagoService {
  final AuthService _authService = AuthService();

  Future<List<CategoriaPagoModel>> fetchAll() async {
    final token = await _authService.getToken();

    final response = await http.get(
      Uri.parse('${ApiConstants.baseUrl}/api/categorias-pago'),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode != 200) {
      throw Exception('Error obteniendo categorías');
    }

    final List data = jsonDecode(response.body);

    return data
        .map((e) => CategoriaPagoModel.fromJson(e))
        .toList();
  }

  Future<bool> create({
    required String nombre,
    required double precio,
    required int mesesDuracion,
    required int tipoAbono,
  }) async {
    final token = await _authService.getToken();

    final response = await http.post(
      Uri.parse('${ApiConstants.baseUrl}/api/categorias-pago'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'nombre': nombre,
        'precio': precio,
        'mesesDuracion': mesesDuracion,
        'tipoAbono': tipoAbono,
      }),
    );

    return response.statusCode == 200;
  }
}