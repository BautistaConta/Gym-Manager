import 'package:flutter/material.dart';
import '../../core/services/auth_service.dart';
import '../../routes/app_routes.dart';
import '../../../widgets/dark_text_field.dart';


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
  bool showPassword = false;

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
    body: Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'GYM MANAGER',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.headlineLarge,
              ),
              const SizedBox(height: 40),

              DarkTextField(
                controller: emailController,
                label: 'Email',
                icon: Icons.email_outlined,
              ),
              const SizedBox(height: 16),

              DarkTextField(
                controller: passwordController,
                label: 'Contraseña',
                icon: Icons.lock_outline,
                obscureText: !showPassword,
                suffix: IconButton(
                  icon: Icon(
                    showPassword
                        ? Icons.visibility_off
                        : Icons.visibility,
                  ),
                  onPressed: () {
                    setState(() => showPassword = !showPassword);
                  },
                ),
              ),
              const SizedBox(height: 30),

              loading
                  ? const Center(
                      child: CircularProgressIndicator(),
                    )
                  : ElevatedButton(
                      onPressed: handleLogin,
                      child: const Text('INICIAR SESIÓN'),
                    ),

              const SizedBox(height: 16),

              TextButton(
                onPressed: () {
                  Navigator.pushNamed(context, AppRoutes.register);
                },
                child: const Text('¿No tenés cuenta? Registrate'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
}