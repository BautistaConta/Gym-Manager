import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/services/auth_service.dart';
import '../../models/user_model.dart';

// Estado simple
class AuthState {
  final bool loading;
  final String? token;
  final UserModel? user;

  AuthState({this.loading = false, this.token, this.user});

  AuthState copyWith({bool? loading, String? token, UserModel? user}) {
    return AuthState(
      loading: loading ?? this.loading,
      token: token ?? this.token,
      user: user ?? this.user,
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  final AuthService _authService = AuthService();

  AuthNotifier(): super(AuthState(loading: true)) {
    _init();
  }

  Future<void> _init() async {
    final token = await _authService.getToken();
    if (token != null) {
      final user = await _authService.fetchCurrentUser();
      state = AuthState(loading: false, token: token, user: user);
    } else {
      state = AuthState(loading: false, token: null, user: null);
    }
  }

  Future<void> login(String email, String password) async {
    state = state.copyWith(loading: true);
    final resp = await _authService.login(email, password);
    final token = resp['token'] ?? resp['accessToken'] ?? resp['jwt'];
    if (token != null) {
      await _authService.saveToken(token.toString());
      final user = await _authService.fetchCurrentUser();
      state = AuthState(loading: false, token: token.toString(), user: user);
    } else {
      state = state.copyWith(loading: false);
      throw Exception('Login failed');
    }
  }

  Future<void> logout() async {
    await _authService.deleteToken();
    state = AuthState(loading: false, token: null, user: null);
  }

  Future<void> refreshUser() async {
    final user = await _authService.fetchCurrentUser();
    state = state.copyWith(user: user);
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier();
});
