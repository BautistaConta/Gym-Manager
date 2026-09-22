# Operación segura de Twilio WhatsApp — piloto

## Estado inicial y límites

El código está preparado y probado con HTTP simulado. No se comprobó la cuenta externa, su saldo, el remitente ni la aprobación de plantillas, y no se hicieron envíos reales. No activar ningún switch real antes de completar esta lista y autorizar expresamente los cargos.

Por defecto se inyecta `FakeWhatsAppSender` y el worker de envío está apagado. Los registros pendientes no son enviados ni etiquetados como aceptación. El fake retorna `Simulado`, sin SID de proveedor. El único mensaje de cuota habilitado es `PorVencer`; no se generan ni envían mensajes `Vencido`. Se conserva el historial anterior y el código de bienvenida/campañas, pero esos envíos requieren habilitación adicional deliberada.

El smoke test permite como máximo una solicitud `PorVencer` por `GymId`. El destinatario viene de configuración, no del body. Repetir el mismo POST, incluso concurrentemente, devuelve el historial existente sin reenviar. Un estado `Procesando`/`RequiereRevision` no autoriza una repetición. No borrar registros ni cambiar claves de deduplicación para forzar otro cargo.

## Intervenciones humanas, en orden

1. Rotar el AuthToken Twilio previamente expuesto en archivos/historial Git. Rotar también MongoDB y JWT antes del despliegue público. Los JSON locales se sanearon para Twilio y el backend ignora cualquier AccountSid/AuthToken en JSON.
2. El titular debe activar manualmente la cuenta paga desde **Upgrade** en Twilio Console, completar sus datos y decidir saldo/método de pago. No lo realiza este proyecto. Consultar [cuenta y upgrade](https://www.twilio.com/docs/usage/tutorials/how-to-use-your-free-trial-account).
3. Registrar y verificar un remitente WhatsApp del gimnasio mediante [Self Sign-up](https://www.twilio.com/docs/whatsapp/self-sign-up). Confirmar en Console que está operativo. Configurar `WhatsAppFromNumber` como `+códigoPaísNúmero`, sin prefijo `whatsapp:`. No comprar números ni modificar cuentas sin autorización.
4. Crear una plantilla `PorVencer` con [Content Template Builder](https://www.twilio.com/docs/whatsapp/tutorial/send-whatsapp-notification-messages-templates), someterla a WhatsApp y esperar estado aprobado. Debe pertenecer al AccountSid configurado. Copiar su ContentSid `HX...`, no un SID legacy `HM...`. Ejemplo: «Hola {{1}}, tu cuota vence el {{2}}.». Las variables son `1 = nombre` y `2 = fecha dd/MM/yyyy`; no poner el mensaje completo en una única variable.
5. Obtener consentimiento del destinatario de prueba y configurar exclusivamente ese número en `AuthorizedTestNumber`. Los recordatorios operativos siguen validando consentimiento del alumno, actividad, teléfono y último pago.
6. Cargar secretos en el panel seguro del hosting (variables de entorno). Para desarrollo local se admite el almacenamiento .NET User Secrets del proyecto; no está cifrado y no es un vault de producción. Evitar valores reales en historial de consola, capturas o logs. Reiniciar el backend después de cargar/modificar configuración.

## Configuración

AccountSid y AuthToken, y los switches de envío real/worker/smoke/confirmaciones, se leen de una configuración separada compuesta únicamente por User Secrets y variables de entorno (el entorno prevalece). No se aceptan esos valores desde appsettings. `Testing` fuerza envío falso aunque exista un switch real en el equipo.

```text
Twilio__AccountSid=<AC... de la cuenta>
Twilio__AuthToken=<TOKEN ROTADO, secreto>
Twilio__WhatsAppFromNumber=<remitente E.164 aprobado>
Twilio__PorVencerContentSid=<HX... aprobado>
Twilio__AuthorizedTestNumber=<único destinatario E.164 con consentimiento>
Twilio__Enabled=true
Twilio__WorkerEnabled=false
Twilio__SmokeTestEnabled=true
Twilio__PaidAccountConfirmed=true
Twilio__TemplatesApprovedConfirmed=true
Twilio__AdditionalTypesEnabled=false
WhatsApp__CampaignsEnabled=false
```

Estos ejemplos no deben ejecutarse con placeholders. Las confirmaciones de cuenta paga/aprobación son declaraciones del operador, no una verificación automática de Twilio. Si faltan credenciales/SIDs el modo real falla al iniciar con un mensaje seguro. Si falta un paso posterior, `GET /api/admin/whatsapp/prueba` devuelve las intervenciones pendientes, sin mostrar secretos. No enviar mientras esa lista tenga elementos.

Variables previas conservadas (no activar en esta prueba):

```text
Twilio__WelcomeContentSid
Twilio__PromotionContentSid
Twilio__GeneralNoticeContentSid
WhatsApp__CampaignsEnabled
WhatsApp__MaxRecipientsPerCampaign
WhatsApp__CampaignBatchSize
WhatsApp__CampaignDelayMilliseconds
```

## Smoke test con aprobación expresa de cargos

Con sesión JWT Admin del gimnasio piloto, consultar primero `GET /api/admin/whatsapp/prueba`. Requiere worker apagado para no drenar la cola histórica durante la prueba. Pedir autorización al titular para **un mensaje potencialmente facturable** antes de ejecutar el siguiente POST. No se requiere ni se crea un pago ficticio: es un registro administrativo de prueba, con nombre/fecha de ejemplo, sin alterar alumnos.

```http
POST /api/admin/whatsapp/prueba
Authorization: Bearer <JWT ADMIN>
Content-Type: application/json

{"tipo":0,"confirmacionCargos":true}
```

No repetir con otro tipo: el endpoint rechaza `Vencido` y cualquier mensaje distinto de `PorVencer`. No agregar número, GymId ni texto al body: no forman parte del contrato. El endpoint deja historial antes de contactar al sender y reclama ese registro atómicamente.

Revisar el registro desde la pantalla **Notificaciones** o `GET /api/notificaciones`: debe quedar `estadoDescripcion=AceptadoPorTwilio`, `providerMessageId=SM...` (o MM...) y timestamps UTC. El valor numérico del estado aceptado sigue siendo 1, compatible con los documentos Mongo anteriores. `FechaEnvio` representa aceptación de la solicitud, no entrega. Cotejar el SID con Console, siguiendo [Messages resource](https://www.twilio.com/docs/messaging/api/message-resource). No se agregaron webhooks ni se afirma entrega/lectura.

Un rechazo 4xx queda `Fallido`. Timeout, interrupción, 5xx o aceptación sin SID válido quedan `RequiereRevision` (o `Procesando` hasta recuperar al reiniciar). Nunca reenviar automáticamente: verificar manualmente en Console. Los logs propios contienen solamente HTTP, categoría, código numérico del proveedor y correlation ID; no incluyen AuthToken, teléfono, nombres ni body completo.

Al terminar apagar `Twilio__SmokeTestEnabled` y `Twilio__Enabled`. Activar posteriormente `WorkerEnabled=true` sólo tras auditar pendientes, validar la plantilla y autorizar operación continua. Usar exactamente una réplica. Los registros antiguos sin variables estructuradas no se convierten automáticamente ni se envían con el body completo: requieren revisión manual.

## Validación sin gasto

Desde la raíz: `dotnet build backend/GyMApi` y `dotnet test backend/GyMApi`. Las pruebas usan senders/handlers falsos sin contactar Twilio. La validación local no confirma facturación, permisos del remitente ni aprobación externa: esos pasos siguen a cargo del titular.
