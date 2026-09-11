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
      body: jsonEncode({"email": email, "password": password}),
    );

    if (response.statusCode != 200) {
      throw Exception('Email o contraseña incorrectos.');
    }

    final Map<String, dynamic> data = jsonDecode(response.body);

    // Validación de token
    final token = data['token'];
    if (token == null || token is! String || token.isEmpty) {
      throw Exception("Token inválido o no recibido del backend.");
    }

    await saveToken(token);

    // Store user
    if (data['user'] != null) {
      await _storage.write(key: _userKey, value: jsonEncode(data['user']));
    }

    return data;
  }

  // -------------------------------
  // GET CURRENT USER (/me)
  // -------------------------------
  Future<UserModel?> fetchCurrentUser() async {
    final token = await getToken();
    if (token == null) {
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

    if (response.statusCode == 200) {
      try {
        final data = jsonDecode(response.body);
        return UserModel.fromJson(data);
      } catch (_) {
        return null;
      }
    }

    // Token expirado o inválido → limpiar storage
    if (response.statusCode == 401) {
      await deleteToken();
    }

    return null;
  }
}
