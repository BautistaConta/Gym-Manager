enum Rol {
  admin,
  gestor,
  profesor,
  alumno,
  unknown,
}

Rol rolFromString(String? s) {
  if (s == null) return Rol.unknown;
  final lowered = s.toLowerCase();
  if (lowered.contains('admin')) return Rol.admin;
  if (lowered.contains('gestor')) return Rol.gestor;
  if (lowered.contains('profesor') || lowered.contains('professor')) return Rol.profesor;
  if (lowered.contains('alumno') || lowered.contains('student')) return Rol.alumno;
  return Rol.unknown;
}

String rolToString(Rol r) {
  switch (r) {
    case Rol.admin:
      return 'Admin';
    case Rol.gestor:
      return 'Gestor';
    case Rol.profesor:
      return 'Profesor';
    case Rol.alumno:
      return 'Alumno';
    default:
      return 'Unknown';
  }
}
