import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import '../../core/constants/api_constants.dart';
import '../../models/user_model.dart';

class AuthService {
  final FlutterSecureStorage _storage = const FlutterSecureStorage();
  static const String _tokenKey = 'jwt_token';
  static const String _userKey = 'user_data';

  // -------------------------------
  // TOKEN STORAGE
  // -------------------------------
  Future<void> saveToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
  }

  Future<String?> getToken() async {
    return _storage.read(key: _tokenKey);
  }
Future<UserModel?> getStoredUser() async {
  final userData = await _storage.read(key: _userKey);
  if (userData == null) return null;
  return UserModel.fromJson(jsonDecode(userData));
}
  Future<void> deleteToken() async {
    await _storage.delete(key: _tokenKey);
    await _storage.delete(key: _userKey);
  }

  // -------------------------------
  // LOGIN
  // -------------------------------
  Future<Map<String, dynamic>> login(String email, String password) async {
    final url = Uri.parse(ApiConstants.baseUrl + ApiConstants.login);

    final response = await http.post(
      url,
      headers: {"Content-Type": "application/json"},
      body: jsonEncode({
        "email": email,
        "password": password,
      }),
    );

    print("📥 Status login: ${response.statusCode}");
    print("📥 Body login: ${response.body}");

    // Manejo de errores HTTP
    if (response.statusCode != 200) {
      throw Exception("Error en login: ${response.body}");
    }

    final Map<String, dynamic> data = jsonDecode(response.body);

    // Validación de token
    final token = data['token'];
    if (token == null || token is! String || token.isEmpty) {
      throw Exception("Token inválido o no recibido del backend.");
    }

    print("🔐 Token recibido: $token");
    await saveToken(token);

    // Store user
    if (data['user'] != null) {
      await _storage.write(
        key: _userKey,
        value: jsonEncode(data['user']),
      );
    }

    return data;
  }

  // -------------------------------
  // REGISTER
  // -------------------------------
  Future<Map<String, dynamic>> register(
      String nombre, String email, String password) async {
    final url = Uri.parse(ApiConstants.baseUrl + ApiConstants.register);

    final response = await http.post(
      url,
      headers: {"Content-Type": "application/json"},
      body: jsonEncode({
        "nombre": nombre,
        "email": email,
        "password": password,
      }),
    );

    return jsonDecode(response.body);
  }

  // -------------------------------
  // GET CURRENT USER (/me)
  // -------------------------------
  Future<UserModel?> fetchCurrentUser() async {
    final token = await getToken();
    if (token == null) {
      print("⚠️ No hay token almacenado, usuario no logueado.");
      return null;
    }

    final url = Uri.parse(ApiConstants.baseUrl + ApiConstants.me);

    final response = await http.get(
      url,
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer $token",
      },
    );

    print("📥 Status ME: ${response.statusCode}");
    print("📥 Body ME: ${response.body}");

    if (response.statusCode == 200) {
      try {
        final data = jsonDecode(response.body);
        return UserModel.fromJson(data);
      } catch (e) {
        print("❌ Error parseando usuario: $e");
        return null;
      }
    }

    // Token expirado o inválido → limpiar storage
    if (response.statusCode == 401) {
      print("⚠️ Token inválido o expirado, limpiando sesión.");
      await deleteToken();
    }

    return null;
  }
}
