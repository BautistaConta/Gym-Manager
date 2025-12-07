import 'package:flutter/material.dart';
import '../../core/services/auth_service.dart';
import '../../routes/app_routes.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final emailController = TextEditingController();
  final passwordController = TextEditingController();
  final AuthService _authService = AuthService();

  bool loading = false;

  void handleLogin() async {
    setState(() => loading = true);

    final response = await _authService.login(
      emailController.text,
      passwordController.text,
    );

    setState(() => loading = false);

    if (response['token'] != null) {
  final user = response['user'] ?? {};
  final rol = user['rol'] ?? 'Desconocido';

  ScaffoldMessenger.of(context).showSnackBar(
    const SnackBar(content: Text('Login exitoso ✅')),
  );

  // Independiente del rol → va al Home común
  Navigator.pushReplacementNamed(context, AppRoutes.home);
  
} else {
  ScaffoldMessenger.of(context).showSnackBar(
    const SnackBar(content: Text('Error al iniciar sesión')),
  );
}
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Login")),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            TextField(
              controller: emailController,
              decoration: const InputDecoration(labelText: "Email"),
            ),
            TextField(
              controller: passwordController,
              obscureText: true,
              decoration: const InputDecoration(labelText: "Contraseña"),
            ),
            const SizedBox(height: 20),
            loading
                ? const CircularProgressIndicator()
                : ElevatedButton(
                    onPressed: handleLogin,
                    child: const Text("Iniciar sesión"),
                  ),
            TextButton(
              onPressed: () {
                Navigator.pushNamed(context, AppRoutes.register);
              },
              child: const Text("¿No tenés cuenta? Registrate"),
            ),
          ],
        ),
      ),
    );
  }
}
