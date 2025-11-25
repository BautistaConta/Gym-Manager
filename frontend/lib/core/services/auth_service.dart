import 'dart:convert';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';

class AuthService {
  Future<Map<String, dynamic>> login(String email, String password) async {
    final response = await http.post(
      Uri.parse(ApiConstants.baseUrl + ApiConstants.login),
      headers: {"Content-Type": "application/json"},
      body: jsonEncode({
        "email": email,
        "password": password,
      }),
    );

    return jsonDecode(response.body);
  }
  Future<Map<String, dynamic>> register(
  String nombre,
  String email,
  String password,
) async {
  final response = await http.post(
    Uri.parse(ApiConstants.baseUrl + ApiConstants.register),
    headers: {"Content-Type": "application/json"},
    body: jsonEncode({
      "nombre": nombre,
      "email": email,
      "password": password,
    }),
  );

  return jsonDecode(response.body);
}
}


