# Observabilidad mínima del piloto

## Qué queda disponible

El backend escribe logs JSON estructurados a `stdout`. Cada petición recibe un `CorrelationId`, visible en el campo de scope del log y en el header de respuesta `X-Correlation-ID`. El cliente puede enviar ese header si necesita conservar un identificador propio; solo se aceptan valores alfanuméricos seguros de hasta 64 caracteres.

Los errores no controlados devuelven `application/problem+json` con `status`, `title`, un mensaje seguro y `correlationId`. Nunca se devuelve un stack trace ni el mensaje interno de una excepción inesperada. El stack queda exclusivamente en los logs del proveedor.

Endpoints públicos para el monitor del hosting:

- `GET /health/live`: confirma que el proceso HTTP está vivo. No consulta dependencias.
- `GET /health/ready`: ejecuta `ping` contra MongoDB. Responde saludable solo si la base está disponible.

La colección MongoDB `PilotEvents` guarda únicamente `GymId`, tipo, identificador de entidad, usuario administrativo, fecha UTC y correlation ID. No guarda contraseñas, JWT, teléfonos, nombres, importes ni contenido de WhatsApp. El registro es *best effort*: un fallo de telemetría genera un warning, pero no revierte una operación de negocio ya exitosa.

## Eventos y resumen

Se registran `LoginExitoso`, altas/cambios/bajas lógicas de alumnos, pagos, altas/cambios de sucursales y notificaciones aceptadas, fallidas o reenviadas. `Aceptada` significa aceptación de la solicitud por Twilio, no entrega al teléfono.

Solo Admin puede consultar:

```http
GET /api/admin/pilot-events/resumen?desdeUtc=2026-09-01T00:00:00Z&hastaUtc=2026-10-01T00:00:00Z
Authorization: Bearer <JWT>
```

`hastaUtc` es exclusivo y el rango máximo es 31 días. La respuesta agrupa por día UTC y tipo. La pantalla **Notificaciones** muestra totales de los últimos siete días para Admin; Gestor conserva el historial de notificaciones sin acceso al resumen administrativo.

Mongo crea automáticamente el índice `ix_pilot_events_gym_fecha_tipo` (`GymId + FechaUtc + Tipo`) al arrancar. No hay una migración de datos históricos: los conteos comienzan desde el despliegue de esta versión.

## Configuración humana en el proveedor

1. Configurar el comando/URL de health check del servicio como `/health/ready`. Usar `/health/live` solo para comprobar que el proceso responde, no para decidir si puede recibir tráfico.
2. Confirmar que el proveedor capture `stdout` y conserve al menos 7 a 14 días de logs. No habilitar captura de headers `Authorization`, cuerpos de login ni variables de entorno.
3. Crear una alerta gratuita, si el proveedor la incluye, por reinicios repetidos, respuestas 5xx o fallo de `/health/ready`. No es necesario contratar Sentry ni otro servicio.
4. Verificar tras desplegar que MongoDB contiene `PilotEvents` y el índice mencionado. No crear la colección manualmente.
5. Realizar un login, crear un alumno de prueba y registrar un pago controlado; revisar que el resumen aumente. No es necesario enviar WhatsApp para validar eventos de uso.

## Investigar un error o mensaje de Twilio

1. Copiar `X-Correlation-ID` de la respuesta que falló. En Notificaciones también se muestra el correlation ID de cada mensaje.
2. Buscar ese valor exacto en los logs del hosting. Los logs JSON permiten ver endpoint, estado y excepción del servidor sin exponerla al navegador.
3. Para WhatsApp, obtener el `ProviderMessageId`/Message SID del historial de Notificaciones asociado al mismo registro.
4. Buscar ese SID en **Twilio Console → Monitor → Logs → Messaging**. Twilio conoce el SID, pero no el correlation ID interno de Gym Manager; la relación se realiza mediante el registro de Notificaciones.
5. Si el estado local es `RequiereRevision`, verificar primero el SID en Twilio. No reintentar a ciegas porque puede duplicar el mensaje y el cargo.

Nunca pegar JWT, AuthToken, cadena de MongoDB o cuerpos completos de mensajes en tickets, capturas o búsquedas compartidas.
