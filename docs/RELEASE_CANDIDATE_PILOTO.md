# Release Candidate local — piloto Gym Manager

Fecha de validación: 7 de octubre de 2026.

## Veredicto

**GO condicional** para desplegar con sender falso y realizar smoke tests sin costo. **NO-GO para habilitar Twilio real** hasta completar el checklist humano y confirmar expresamente el criterio de mensajes vencidos.

La política vigente del producto, incorporada para ahorrar costos, genera únicamente `PorVencer`. La validación confirmó **cero** mensajes `Vencido` y **cero** `PagoConfirmado`. El punto de aceptación que pide “un Vencido por pago” contradice esa decisión anterior; no se reactivó porque modificaría comportamiento y costos.

## Resultados

| Prueba | Resultado | Evidencia |
|---|---|---|
| Bootstrap Admin | PASS | Admin idempotente creado en Mongo temporal y login HTTP exitoso. |
| Gestor opcional | PASS | Creado por endpoint Admin. |
| Login/JWT | PASS | Login real contra API temporal y uso posterior del JWT. |
| Persistencia, guardas y logout | PASS | Tests Flutter de secure storage, ruta directa anónima, rol Gestor y logout. |
| Exactamente cuatro sucursales | PASS | Cuatro POST y listado con longitud exacta 4. |
| Alumnos | PASS | Alta, edición, búsqueda, asignación de sede y desactivación vía HTTP. Los filtros de estado/sucursal permanecen del lado Flutter. |
| Categoría de pago | PASS | Categoría mensual creada vía HTTP. |
| Métodos de pago | PASS | Efectivo, Transferencia y Tarjeta registrados vía HTTP. |
| Estados de cuota | PASS | Tests de hoy, hoy+N, hoy+N+1, vencida y sin pagos. |
| Renovación atrasada | PASS | Pago histórico vencido insertado en la base temporal; la renovación HTTP comenzó en el día de negocio actual. |
| Deduplicación PorVencer | PASS | Dos solicitudes manuales devolvieron el mismo pendiente y quedó un solo documento. |
| Vencido | PASS respecto de política vigente / discrepancia con checklist | Cero generados deliberadamente. |
| PagoConfirmado | PASS | Cero generados. |
| Inactivo/sin consentimiento | PASS | Ambos recordatorios fueron rechazados; tests unitarios cubren además el job. |
| Sender falso | PASS | Pendiente procesado como `Simulado`, sin contacto con Twilio. |
| Historial y contadores | PASS | Consulta paginada y filtrada vía HTTP; suite cubre filtros, errores seguros y paginación. |
| Reintento manual | PASS unitario | Solo Fallida/RequiereRevision, con confirmación y sin reintento automático. |
| Aislamiento GymId | PASS | Documento de otro gimnasio insertado directamente y no visible por la API del piloto. |
| Base temporal | PASS | Se usó `GymRC_*` y se eliminó al finalizar. |

## Comandos ejecutados

- `dotnet build backend/GyMApi/GyMApi.csproj --no-restore`: 0 errores, 0 advertencias.
- `dotnet test backend/GyMApi.Tests/GyMApi.Tests.csproj --no-restore`: 76/76.
- RC Mongo opt-in: 1/1 sobre base temporal aislada.
- `flutter test`: 11/11.
- `flutter analyze --no-fatal-infos`: sin errores ni warnings; 21 avisos informativos preexistentes.
- `flutter build web --release`: correcto, salida en `frontend/build/web`.

## Defectos corregidos durante la RC

1. Se agregó un recorrido HTTP opt-in y autolimpiable para futuras RC, usando una base `GymRC_*` aislada.
2. El primer arnés usaba un nombre de base superior al límite de MongoDB y no cargaba un origen CORS de Testing; ambos problemas quedaron corregidos.
3. Se agregaron tests Flutter para guardas de rutas, restricción por rol, persistencia del token y logout.
4. `AuthNotifier` ahora permite inyectar estado/servicio en tests sin cambiar su comportamiento productivo.
5. `appsettings.Example.json` ya no habilita bootstrap con una contraseña conocida e inválida; queda deshabilitado y vacío por defecto.

No se encontraron defectos bloqueantes del producto en el recorrido ejecutado.

## Riesgos aceptados

- No se realizó envío real a Twilio ni se verificó entrega/lectura; solo sender falso.
- La política de `Vencido` debe quedar confirmada antes del GO definitivo. El código actual no lo envía.
- Flutter Web compila a JavaScript. El dry-run de WebAssembly informa incompatibilidad de `flutter_secure_storage_web`; no bloquea el build Web actual.
- Persisten 21 lints informativos anteriores, principalmente llaves en `if` y tres usos de `BuildContext` después de `await`; no son errores de compilación.
- El piloto requiere una sola réplica del backend para el motor de notificaciones.
- La prueba RC usa la conexión Mongo configurada, pero siempre cambia a una base temporal y la elimina. Para repetirla se requiere acceso de red y permisos de crear/eliminar bases.

## Variables necesarias

Obligatorias en producción:

```text
ASPNETCORE_ENVIRONMENT=Production
MongoDB__ConnectionString=<secreto rotado>
MongoDB__DatabaseName=<base productiva>
MongoDB__UsersCollectionName=Usuarios
Jwt__Key=<secreto aleatorio de 32+ caracteres>
Jwt__Issuer=<issuer>
Jwt__Audience=<audience>
MultiTenancy__PilotGymId=<id estable del gimnasio piloto>
Cors__AllowedOrigins__0=https://<frontend>
Cuotas__TimeZoneId=America/Argentina/Buenos_Aires
Cuotas__DiasProximoAVencer=5
```

Bootstrap inicial, únicamente para el primer arranque:

```text
BootstrapAdmin__Enabled=true
BootstrapAdmin__Nombre=<nombre>
BootstrapAdmin__Email=<email>
BootstrapAdmin__Password=<secreto de 10+ caracteres>
```

Después de confirmar el Admin, cambiar `BootstrapAdmin__Enabled=false` y reiniciar. El mecanismo no reemplaza credenciales existentes.

Mantener Twilio apagado hasta el smoke test autorizado:

```text
Twilio__Enabled=false
Twilio__WorkerEnabled=false
Twilio__SmokeTestEnabled=false
Twilio__AdditionalTypesEnabled=false
WhatsApp__CampaignsEnabled=false
```

## Checklist manual de Twilio real

- [ ] Rotar y cargar `Twilio__AccountSid` y `Twilio__AuthToken` en secretos del hosting.
- [ ] Confirmar cuenta paga y saldo controlado.
- [ ] Confirmar remitente WhatsApp aprobado y cargar `Twilio__WhatsAppFromNumber`.
- [ ] Aprobar únicamente la plantilla `PorVencer` y cargar `Twilio__PorVencerContentSid`.
- [ ] Cargar un solo `Twilio__AuthorizedTestNumber` con consentimiento.
- [ ] Mantener `WorkerEnabled=false` durante el smoke test.
- [ ] Configurar `PaidAccountConfirmed=true`, `TemplatesApprovedConfirmed=true` y `SmokeTestEnabled=true`.
- [ ] Consultar `GET /api/admin/whatsapp/prueba`; no continuar si informa intervenciones pendientes.
- [ ] Autorizar explícitamente el cargo de un mensaje y enviar solamente `PorVencer`.
- [ ] Confirmar `AceptadoPorTwilio` y Message SID en Gym Manager y Twilio Console.
- [ ] Apagar `SmokeTestEnabled` al terminar.
- [ ] Auditar pendientes y recién entonces habilitar `WorkerEnabled=true`, manteniendo una sola réplica.

## Checklist Go/No-Go

- [x] Build backend limpio.
- [x] Tests backend y RC aislada en verde.
- [x] Tests Flutter en verde.
- [x] Build Web release generado.
- [x] Admin inicial y rol Gestor verificados.
- [x] CORS productivo con origen HTTPS concreto preparado.
- [x] Mongo, JWT y PilotGymId requeridos al iniciar.
- [x] Health checks y logs con correlation ID disponibles.
- [x] Sender falso por defecto; no hubo cargos.
- [ ] Confirmar por escrito que el piloto continúa **sin** mensajes `Vencido`.
- [ ] Rotar secretos MongoDB, JWT y Twilio antes del despliegue público.
- [ ] Configurar dominio HTTPS y health check `/health/ready` en el hosting.
- [ ] Completar smoke test Twilio controlado.

Con los pasos operativos pendientes y la confirmación de política, el deploy pasa a **GO**.
