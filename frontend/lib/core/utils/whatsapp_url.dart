Uri? buildWhatsAppUri(String phone, {String? initialText}) {
  final trimmed = phone.trim();
  if (!RegExp(r'^\+[1-9]\d{7,14}$').hasMatch(trimmed)) return null;
  return Uri.https('wa.me', '/${trimmed.substring(1)}',
      initialText == null || initialText.trim().isEmpty ? null : {'text': initialText.trim()});
}
