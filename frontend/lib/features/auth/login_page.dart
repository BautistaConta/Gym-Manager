import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/auth_provider.dart';
import '../../core/theme/app_theme.dart';
import '../../routes/app_routes.dart';
import '../../widgets/dark_text_field.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});
  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final emailController = TextEditingController();
  final passwordController = TextEditingController();
  bool loading = false;
  bool showPassword = false;

  @override
  void dispose() {
    emailController.dispose();
    passwordController.dispose();
    super.dispose();
  }

  Future<void> handleLogin() async {
    if (emailController.text.trim().isEmpty ||
        passwordController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Completá el email y la contraseña.')),
      );
      return;
    }
    setState(() => loading = true);
    try {
      await ref
          .read(authProvider.notifier)
          .login(emailController.text.trim(), passwordController.text);
      if (!mounted) return;
      Navigator.pushNamedAndRemoveUntil(context, AppRoutes.home, (_) => false);
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Email o contraseña incorrectos.')),
      );
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: Stack(
      children: [
        Positioned(
          top: -120,
          right: -100,
          child: Container(
            width: 340,
            height: 340,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: AppTheme.primaryGreen.withValues(alpha: .07),
            ),
          ),
        ),
        Positioned(
          bottom: -170,
          left: -130,
          child: Container(
            width: 400,
            height: 400,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: AppTheme.primaryGreen.withValues(alpha: .04),
            ),
          ),
        ),
        Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 460),
              child: Container(
                padding: const EdgeInsets.all(30),
                decoration: BoxDecoration(
                  color: AppTheme.surface,
                  borderRadius: BorderRadius.circular(26),
                  border: Border.all(color: AppTheme.border),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: .25),
                      blurRadius: 40,
                      offset: const Offset(0, 18),
                    ),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Align(
                      child: Container(
                        width: 62,
                        height: 62,
                        decoration: BoxDecoration(
                          color: AppTheme.primaryGreen,
                          borderRadius: BorderRadius.circular(19),
                        ),
                        child: const Icon(
                          Icons.fitness_center_rounded,
                          color: AppTheme.primaryDark,
                          size: 30,
                        ),
                      ),
                    ),
                    const SizedBox(height: 22),
                    Text(
                      'Bienvenido',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.headlineLarge,
                    ),
                    const SizedBox(height: 7),
                    Text(
                      'Ingresá a tu panel de Gym Manager',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: AppTheme.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 30),
                    DarkTextField(
                      controller: emailController,
                      label: 'Email',
                      icon: Icons.email_outlined,
                      keyboardType: TextInputType.emailAddress,
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
                              ? Icons.visibility_off_outlined
                              : Icons.visibility_outlined,
                        ),
                        onPressed: () =>
                            setState(() => showPassword = !showPassword),
                      ),
                    ),
                    const SizedBox(height: 24),
                    SizedBox(
                      height: 54,
                      child: FilledButton(
                        onPressed: loading ? null : handleLogin,
                        child: loading
                            ? const SizedBox.square(
                                dimension: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Text('Iniciar sesión'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ],
    ),
  );
}
