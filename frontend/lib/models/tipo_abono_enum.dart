enum TipoAbono {
  adulto,
  nino,
}
String tipoAbonoLabel(int tipo) {
  switch (tipo) {
    case 0:
      return 'Adulto';

    case 1:
      return 'Niño';

    default:
      return 'Desconocido';
  }
}