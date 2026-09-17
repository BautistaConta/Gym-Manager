import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/core/utils/whatsapp_url.dart';

void main() {
  test('genera wa.me sin el signo más y conserva texto como query', () {
    final uri = buildWhatsAppUri('+5491123456789', initialText: 'Hola Juan');
    expect(uri.toString(), 'https://wa.me/5491123456789?text=Hola+Juan');
  });

  test('rechaza teléfonos que no están en E.164', () {
    expect(buildWhatsAppUri('11 2345-6789'), isNull);
    expect(buildWhatsAppUri('+0123'), isNull);
  });
}
