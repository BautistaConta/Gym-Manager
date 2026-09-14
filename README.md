# Gym Manager

Aplicación de gestión de gimnasios con autenticación, roles, alumnos, sucursales, pagos y notificaciones.

## Estructura

- `/frontend`: aplicación Flutter con Riverpod, compatible con Web.
- `/backend/GyMApi`: ASP.NET Core Web API sobre .NET 8.
- Persistencia: MongoDB.

## Preparación multi-tenant

Los documentos de usuarios, alumnos, sucursales, categorías de pago, pagos y notificaciones incluyen un `GymId` obligatorio. En endpoints autenticados se obtiene exclusivamente del claim `gym_id` del JWT; no se acepta desde body ni query.

Durante la etapa piloto, `MultiTenancy:PilotGymId` identifica al único gimnasio para login, bootstrap administrativo y background jobs. Puede tomarse `backend/GyMApi/appsettings.Example.json` como referencia de configuración. Los jobs usan ese valor de forma deliberada; cuando exista multi-tenant real deberá reemplazarse por una iteración explícita de tenants.

## Seguridad y despliegue

El registro público no está disponible. Los usuarios administrativos sólo pueden ser creados o modificados por otro usuario con rol `Admin`; `Gestor` conserva acceso operativo a alumnos, sucursales, categorías y pagos.

Para crear el primer administrador, configurar secretos o variables de entorno y habilitar el bootstrap explícitamente:

```powershell
$env:BootstrapAdmin__Enabled = "true"
$env:BootstrapAdmin__Nombre = "Administrador"
$env:BootstrapAdmin__Email = "admin@example.com"
$env:BootstrapAdmin__Password = "una-contraseña-larga-y-única"
dotnet run --project backend/GyMApi/GyMApi.csproj
```

El bootstrap es idempotente: si ya existe cualquier administrador no crea otro; si el email ya pertenece a un usuario, tampoco modifica su rol ni su contraseña. Después del primer inicio debe deshabilitarse eliminando `BootstrapAdmin__Enabled` o configurándolo en `false`.

En producción, `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc. deben contener los orígenes HTTPS concretos del frontend. La API falla al iniciar si faltan MongoDB, `PilotGymId`, la clave/emisor/audiencia JWT, la configuración requerida de Twilio cuando está habilitado, las credenciales del bootstrap cuando está habilitado o los orígenes CORS de producción.

> **Rotación obligatoria:** las credenciales de MongoDB, la clave JWT y las credenciales/tokens de Twilio deben rotarse antes de un despliegue público porque estuvieron anteriormente presentes en el historial Git. Eliminarlas del archivo actual no invalida los secretos ya publicados en commits anteriores.

### Migración de datos existentes

Antes de desplegar esta versión sobre una base con datos históricos, configurar `MultiTenancy:PilotGymId` y ejecutar una vez:

```powershell
dotnet run --project backend/GyMApi/GyMApi.csproj -- --migrate-pilot-gym
```

El comando es idempotente: completa `GymId` sólo cuando falta y genera `emailNormalizado` cuando corresponde. Al finalizar crea los índices tenant, incluidos los únicos por gimnasio para DNI y email normalizado. Si existen DNIs o emails duplicados dentro del gimnasio piloto, la creación del índice único fallará y esos duplicados deberán resolverse manualmente.

## Verificación

```powershell
dotnet build backend/GyMApi/GyMApi.sln
dotnet test backend/GyMApi.Tests/GyMApi.Tests.csproj
Set-Location frontend
flutter analyze
```

## Reglas de cuota del piloto

`Cuotas:TimeZoneId` define la zona horaria de negocio (por defecto `America/Argentina/Buenos_Aires`) y `Cuotas:DiasProximoAVencer` la ventana de aviso (por defecto 5). `FechaPago` y demás timestamps técnicos permanecen en UTC. `PeriodoDesde` y `PeriodoHasta` son fechas de calendario de negocio, almacenadas como UTC a las 00:00 para transportarlas sin ambigüedad; **ambos extremos son inclusivos**. El alumno está cubierto hasta finalizar el día `PeriodoHasta` en la zona de negocio.

Sin pagos se devuelve `SIN_PAGOS`; si el último vencimiento es anterior a hoy, `VENCIDA`; entre hoy y hoy + N días inclusive, `PROXIMO_A_VENCER`; después de esa ventana, `AL_DIA`. El estado se calcula en cada consulta y no se persiste.

Al renovar una cuota aún vigente, el nuevo `PeriodoDesde` es el día posterior al último `PeriodoHasta`, evitando superposición. Si la cuota anterior ya venció, el nuevo período comienza hoy. El `PeriodoHasta` automático es el mismo día de calendario tras `MesesDuracion` meses: por ejemplo, un mes iniciado el 14/09 vence el 14/10 inclusive. Con una ventana de 5 días, ese pago está `AL_DIA` hasta el 08/10, `PROXIMO_A_VENCER` del 09/10 al 14/10 y `VENCIDA` desde el 15/10. Una fecha manual no puede ser anterior al nuevo inicio. `SucursalId` del pago sigue identificando la sede que recibe el ingreso, independientemente de `SucursalPrincipalId` del alumno.

## Notificaciones WhatsApp del piloto

Producción debe ejecutar **exactamente una réplica** del backend, siempre encendida. Los dos jobs internos evalúan vencimientos y envían pendientes; no hay cola externa. Solo se generan `PorVencer` al entrar en la ventana `Cuotas:DiasProximoAVencer` y `Vencido` desde el día posterior al vencimiento. `PagoConfirmado` ya no se genera ni se envía. Cada notificación nueva guarda `GymId`, `PagoId`, `SucursalId`, vencimiento, clave de deduplicación, intentos, SID de Twilio y fechas UTC. Un índice único en MongoDB impide duplicar el mismo `GymId + PagoId + Tipo`, incluso si coinciden dos ejecuciones o un clic manual con el job. El botón manual solo admite el pago más reciente y en estado próximo a vencer o vencido; ahora **encola**, no envía durante la petición HTTP.

Antes de contactar a Twilio, el sender reclama una notificación `Pendiente` con una actualización atómica a `Procesando`, incrementa `Intentos` y vuelve a comprobar alumno activo, consentimiento, teléfono E.164 y que el pago todavía sea el último. Si cambió algo, queda `Descartado`. Una aceptación de Twilio con SID queda `Enviado`; un rechazo definitivo queda `Fallido` y solo puede reintentarse manualmente. Un timeout, HTTP 5xx, respuesta sin SID o interrupción entre envío y confirmación queda `RequiereRevision`; **no se reintenta automáticamente**. Al arrancar, cualquier `Procesando` remanente pasa a revisión. El operador debe cotejar el registro con Twilio antes de decidir qué hacer. Esto prioriza evitar mensajes/cargos duplicados: puede perderse un envío o requerir intervención, y no existe garantía de entrega efectiva al teléfono aunque Twilio acepte la solicitud. Los registros anteriores sin `PagoId` no son procesados por el nuevo sender.

Los alumnos no necesitan una cuenta. En el alta Flutter, "Avisos por WhatsApp" aparece activado por defecto, pero el personal solo puede guardar esa opción si confirma que el alumno aceptó explícitamente e indica cómo lo hizo (por ejemplo, formulario firmado). Si el alumno no acepta, se apaga el interruptor. El backend también exige `consentimientoConfirmado: true` y `medioConsentimiento` al crear o habilitar notificaciones por API; omitir esos datos no constituye consentimiento. Desde el módulo Alumnos, el personal puede registrar una aceptación posterior o revocarla. Un Admin o Gestor puede usar `PUT /api/alumnos/{id}/notificaciones`, body `{"notificacionesHabilitadas":true,"consentimientoConfirmado":true,"medioConsentimiento":"formulario firmado"}`; para revocar, `{"notificacionesHabilitadas":false}`. El backend guarda el medio declarado y la fecha UTC; el gimnasio conserva la prueba original. Sin confirmación, los alumnos nuevos se crean con la opción apagada o el alta se rechaza, y no se les envía WhatsApp. Los alumnos antiguos con flag `true` pero sin fecha registrada **no recibirán mensajes** hasta reconfirmar. El flag y la fecha son un registro operativo, no sustituyen la evidencia del consentimiento real.
