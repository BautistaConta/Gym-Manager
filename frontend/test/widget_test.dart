import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'package:frontend/main.dart';
import 'package:frontend/models/rol_enum.dart';
import 'package:frontend/widgets/rol_dropdown.dart';
import 'package:frontend/widgets/modals/create_alumno_modal.dart';
import 'package:frontend/widgets/modals/edit_consentimiento_modal.dart';
import 'package:frontend/widgets/modals/edit_alumno_modal.dart';
import 'package:frontend/models/alumno_model.dart';

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

  testWidgets('el alta no habilita avisos sin consentimiento explícito', (
    tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: CreateAlumnoModal(sucursales: [])),
      ),
    );

    expect(
      find.widgetWithText(
        SwitchListTile,
        'El alumno autoriza recibir avisos del gimnasio por WhatsApp',
      ),
      findsOneWidget,
    );
    expect(
      tester.widget<SwitchListTile>(find.byType(SwitchListTile)).value,
      isFalse,
    );
    expect(
      find.text('Confirmo que el alumno aceptó explícitamente'),
      findsNothing,
    );
    expect(
      find.byType(CheckboxListTile),
      findsNothing,
    );
    await tester.tap(find.byType(SwitchListTile));
    await tester.pump();
    expect(find.byType(CheckboxListTile), findsOneWidget);
    expect(
      tester.widget<CheckboxListTile>(find.byType(CheckboxListTile)).value,
      isFalse,
    );
  });

  testWidgets(
    'el diálogo de consentimiento cierra sin reutilizar controllers ni keys',
    (tester) async {
      ConsentimientoEdicion? resultado;
      final alumno = AlumnoModel(
        id: 'alumno-1',
        nombre: 'Ana',
        dni: '12345678',
        telefono: '+5493811234567',
        activo: true,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => FilledButton(
                onPressed: () async {
                  resultado = await showDialog<ConsentimientoEdicion>(
                    context: context,
                    builder: (_) => EditConsentimientoModal(alumno: alumno),
                  );
                },
                child: const Text('Abrir'),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();
      await tester.tap(find.byType(SwitchListTile));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextFormField), 'formulario firmado');
      await tester.tap(find.byType(CheckboxListTile));
      await tester.tap(find.text('Guardar'));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(resultado?.habilitadas, isTrue);
      expect(resultado?.medio, 'formulario firmado');

      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('el diálogo de edición libera controllers al salir del árbol', (
    tester,
  ) async {
    AlumnoEdicion? resultado;
    final alumno = AlumnoModel(
      id: 'alumno-2',
      nombre: 'Ana',
      dni: '12345678',
      telefono: '+5493811234567',
      activo: true,
    );
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => FilledButton(
              onPressed: () async {
                resultado = await showDialog<AlumnoEdicion>(
                  context: context,
                  builder: (_) =>
                      EditAlumnoModal(alumno: alumno, sucursales: const []),
                );
              },
              child: const Text('Editar'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Editar'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Guardar cambios'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(resultado?.nombre, 'Ana');
  });
}
