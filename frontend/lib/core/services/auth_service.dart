import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import '../../core/constants/api_constants.dart';
import '../../models/user_model.dart';

class AuthService {
  final _storage = const FlutterSecureStorage();
  static const _tokenKey = 'jwt_token';

  // 🔹 Guardar token
  Future<void> saveToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
  }

  // 🔹 Obtener token
  Future<String?> getToken() async {
    return await _storage.read(key: _tokenKey);
  }

  // 🔹 Borrar token (logout)
  Future<void> deleteToken() async {
    await _storage.delete(key: _tokenKey);
  }

  // 🔥 LOGIN CORRECTO + GUARDA TOKEN !!!
  Future<Map<String, dynamic>> login(String email, String password) async {
    final response = await http.post(
      Uri.parse(ApiConstants.baseUrl + ApiConstants.login),
      headers: {"Content-Type": "application/json"},
      body: jsonEncode({
        "email": email,
        "password": password,
      }),
    );
     print("📥 Status login: ${response.statusCode}");
     print("📥 Body login: ${response.body}");
    final data = jsonDecode(response.body);
     

    // Si el backend responde con token → guardarlo
    if (response.statusCode == 200 && data['token'] != null) {
      await saveToken(data['token']);
    }
    if (data['token'] != null) {
    print("🔐 Token recibido: ${data['token']}");}
    print(data);
    return data;
  }

  // 🔥 REGISTER (sin cambios)
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

  // 🔥 ME (perfil actual)
  Future<UserModel?> fetchCurrentUser() async {
    final token = await getToken();
    if (token == null) return null;

    final response = await http.get(
      Uri.parse(ApiConstants.baseUrl + ApiConstants.me),
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer $token",
      },
    );

    if (response.statusCode == 200) {
      try {
        final body = jsonDecode(response.body);
        return UserModel.fromJson(body);
      } catch (_) {
        return null;
      }
    }

    return null;
  }
}
