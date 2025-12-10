import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../core/constants/api_constants.dart';
import '../../models/user_model.dart';
import 'auth_service.dart';

class UsersService {
  final AuthService _authService = AuthService();

  Future<List<UserModel>> fetchAllUsers() async {
    final token = await _authService.getToken();
    print("🔐 Usando token: $token");
    final response = await http.get(
      Uri.parse(ApiConstants.baseUrl + ApiConstants.users),
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer $token",
      },
    );
    print("Respuesta de getALlusers: ${response.statusCode} - ${response.body}");

    if (response.statusCode == 200) {
      final List<dynamic> body = jsonDecode(response.body);
      return body.map((e) => UserModel.fromJson(e)).toList();
    } else if (response.statusCode == 401 || response.statusCode == 403) {
      throw HttpException(response.statusCode, response.body);
    } else {
      throw Exception('Error fetching users: ${response.statusCode}');
    }
  }

  Future<bool> changeUserRol(String userId, String newRol) async {
    final token = await _authService.getToken();
    final response = await http.put(
      Uri.parse(ApiConstants.baseUrl + ApiConstants.changeRol(userId)),
      headers: {
        "Content-Type": "application/json",
        if (token != null) "Authorization": "Bearer $token",
      },
      body: jsonEncode({"rol": newRol}),
    );

    if (response.statusCode == 200) {
      return true;
    } else {
      return false;
    }
  }
}

class HttpException implements Exception {
  final int statusCode;
  final String body;
  HttpException(this.statusCode, this.body);
}
