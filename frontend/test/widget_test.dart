import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'package:frontend/main.dart';
import 'package:frontend/models/rol_enum.dart';
import 'package:frontend/widgets/rol_dropdown.dart';

void main() {
  testWidgets('muestra la pantalla de acceso de Gym Manager', (tester) async {
    FlutterSecureStorage.setMockInitialValues({});
    await tester.pumpWidget(const ProviderScope(child: MyApp()));
    await tester.pumpAndSettle();

    expect(find.text('Bienvenido'), findsOneWidget);
    expect(find.text('Iniciar sesión'), findsOneWidget);
    expect(find.text('Email'), findsOneWidget);
    expect(find.text('Contraseña'), findsOneWidget);
  });

  testWidgets('muestra roles históricos sin romper el dropdown', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: RolDropdown(
            currentRole: Rol.profesor,
            onRoleSelected: (_) async => true,
          ),
        ),
      ),
    );

    expect(find.text('Profesor'), findsOneWidget);
  });
}
