import 'package:flutter/material.dart';
import '../../core/services/auth_service.dart';
import '../../../widgets/dark_text_field.dart';


class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final nameController = TextEditingController();
  final emailController = TextEditingController();
  final passwordController = TextEditingController();

  final AuthService _authService = AuthService();
  bool loading = false;
  bool showPassword = false;

  void handleRegister() async {
    if (nameController.text.isEmpty ||
        emailController.text.isEmpty ||
        passwordController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Completa todos los campos")),
      );
      return;
    }

    setState(() => loading = true);

    final response = await _authService.register(
      nameController.text,
      emailController.text,
      passwordController.text,
    );

    setState(() => loading = false);

    if (response['message'] != null || response['token'] != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Registro exitoso')),
      );
      Navigator.pop(context);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Error al registrarse')),
      );
    }
  }

 @override
Widget build(BuildContext context) {
  return Scaffold(
    backgroundColor: Colors.black,
    body: Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'CREAR CUENTA',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: Colors.greenAccent,
                  fontSize: 26,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1.2,
                ),
              ),
              const SizedBox(height: 40),

              DarkTextField(
                controller: nameController,
                label: 'Nombre completo',
                icon: Icons.person_outline,
              ),
              const SizedBox(height: 16),

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
                    color: Colors.greenAccent,
                  ),
                  onPressed: () {
                    setState(() => showPassword = !showPassword);
                  },
                ),
              ),
              const SizedBox(height: 30),

              loading
                  ? const Center(
                      child: CircularProgressIndicator(
                        color: Colors.greenAccent,
                      ),
                    )
                  : ElevatedButton(
                      onPressed: handleRegister,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.greenAccent,
                        foregroundColor: Colors.black,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text(
                        'REGISTRARSE',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 1,
                        ),
                      ),
                    ),

              const SizedBox(height: 16),

              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text(
                  '¿Ya tenés cuenta? Iniciá sesión',
                  style: TextStyle(color: Colors.greenAccent),
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
}