import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/core/providers/auth_provider.dart';
import 'package:frontend/core/services/auth_service.dart';
import 'package:frontend/models/rol_enum.dart';
import 'package:frontend/models/user_model.dart';
import 'package:frontend/routes/app_routes.dart';

void main() {
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));

  testWidgets('una ruta operativa directa exige una sesión válida', (
    tester,
  ) async {
    await tester.pumpWidget(
      _app(AppRoutes.gestionAlumnos, AuthState(loading: false)),
    );
    await tester.pumpAndSettle();

    expect(find.text('Bienvenido'), findsOneWidget);
    expect(find.text('Gestión de alumnos'), findsNothing);
  });

  testWidgets('Gestor no puede abrir una ruta exclusiva de Admin', (
    tester,
  ) async {
    await tester.pumpWidget(
      _app(AppRoutes.gestionUsuarios, _authenticated(Rol.gestor)),
    );
    await tester.pumpAndSettle();

    expect(find.text('Acceso restringido'), findsOneWidget);
  });

  testWidgets('logout visible elimina la sesión y vuelve al login', (
    tester,
  ) async {
    const storage = FlutterSecureStorage();
    await storage.write(key: 'jwt_token', value: 'token-rc');
    await tester.pumpWidget(_app(AppRoutes.home, _authenticated(Rol.admin)));
    await tester.pumpAndSettle();

    expect(find.byTooltip('Cerrar sesión'), findsOneWidget);
    await tester.tap(find.byTooltip('Cerrar sesión'));
    await tester.pumpAndSettle();

    expect(find.text('Bienvenido'), findsOneWidget);
    expect(await AuthService().getToken(), isNull);
  });

  test('el token persiste en secure storage hasta cerrar sesión', () async {
    final service = AuthService();
    await service.saveToken('token-persistido');
    expect(await AuthService().getToken(), 'token-persistido');
    await service.deleteToken();
    expect(await service.getToken(), isNull);
  });
}

Widget _app(String initialRoute, AuthState state) => ProviderScope(
  overrides: [
    authProvider.overrideWith(
      (ref) => AuthNotifier(initialState: state, initialize: false),
    ),
  ],
  child: MaterialApp(
    initialRoute: initialRoute,
    onGenerateRoute: AppRoutes.onGenerateRoute,
  ),
);

AuthState _authenticated(Rol role) => AuthState(
  loading: false,
  token: 'token-rc',
  user: UserModel(
    id: 'usuario-rc',
    nombre: 'Usuario RC',
    email: 'usuario@rc.test',
    rol: role,
  ),
);
