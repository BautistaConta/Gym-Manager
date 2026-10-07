# Despliegue económico del piloto

Verificado contra documentación oficial el 7 de octubre de 2026.

## Arquitectura elegida

| Componente | Servicio | Costo base estimado | Decisión |
|---|---|---:|---|
| Flutter Web | Cloudflare Pages Free | USD 0 | Sitio estático global, dominio `pages.dev` incluido. |
| API .NET 8 | Railway Hobby, una réplica | USD 5/mes como mínimo | No se usa Free ni un servicio que duerma. El consumo incluido es USD 5; el total puede superar ese importe. |
| MongoDB | Atlas M0 | USD 0 | Suficiente para el piloto mientras datos + índices no superen 0,5 GB. |
| WhatsApp | Twilio + Meta | Por mensaje | Sólo `PorVencer`; el envío real se habilita deliberadamente. |

Railway factura consumo real: RAM USD 10/GB/mes, CPU USD 20/vCPU/mes y egreso USD 0,05/GB. Para esta API pequeña se estima **USD 5–8/mes**, a confirmar con métricas de la primera semana. Configurar un límite de gasto en Railway; un límite puede detener el servicio y no reemplaza el monitoreo.

Referencias oficiales:

- [Railway: planes y precios](https://docs.railway.com/pricing/plans)
- [Railway: health checks](https://docs.railway.com/deployments/healthchecks)
- [Cloudflare Pages: límites](https://developers.cloudflare.com/pages/platform/limits/)
- [MongoDB Atlas M0: límites](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/)
- [Twilio WhatsApp: precios](https://www.twilio.com/en-us/whatsapp/pricing)

Render Free fue descartado porque suspende servicios sin tráfico. El plan Railway Hobby requiere una tarjeta pospaga. Atlas M0 no incluye backups nativos y Atlas puede desactivar clústeres Free inactivos conforme a sus términos.

## Secretos que deben rotarse antes de desplegar

MongoDB, JWT y Twilio aparecieron previamente en el historial Git. Cambiar los tres en sus proveedores; borrar el valor del commit actual no invalida un secreto ya expuesto. No colocar valores reales en `appsettings*.json`, GitHub Actions ni comandos guardados.

## 1. Atlas M0

1. Crear un proyecto y un clúster M0 en AWS `us-east-1`, cercano a Railway US East.
2. Crear un usuario exclusivo con contraseña aleatoria y permisos de lectura/escritura sobre la base productiva.
3. Railway no garantiza una IP de salida fija en Hobby. Habilitar acceso de red desde `0.0.0.0/0` sólo si es necesario, usando TLS, credenciales exclusivas y contraseña fuerte.
4. Guardar la URI únicamente como secreto `MongoDB__ConnectionString` en Railway.
5. Usar un nombre nuevo de base, por ejemplo `GymManagerPilot`, en `MongoDB__DatabaseName`.

Límites M0 relevantes: 0,5 GB incluyendo índices, 500 conexiones y 10 GB de entrada/salida en siete días. Revisar almacenamiento y conexiones semanalmente.

## 2. Railway Hobby — backend

Conectar `BautistaConta/Gym-Manager`, rama `main`. El repositorio aporta el `Dockerfile` multi-stage de raíz y `.railway/railway.ts` con la configuración IaC vigente. El contenedor escucha `PORT`; Railway comprueba `/health/ready` y fija una sola réplica en US East. En Settings confirmar `Restart Policy: Always`; no habilitar Serverless ni más regiones/réplicas.

Variables normales:

```text
ASPNETCORE_ENVIRONMENT=Production
MongoDB__DatabaseName=GymManagerPilot
MongoDB__UsersCollectionName=Usuarios
Jwt__Issuer=gymmanager.api
Jwt__Audience=gymmanager.app
MultiTenancy__PilotGymId=<id estable y no secreto>
Cuotas__TimeZoneId=America/Argentina/Buenos_Aires
Cuotas__DiasProximoAVencer=5
Cors__AllowedOrigins__0=https://<proyecto>.pages.dev
Notificaciones__VencimientosIntervalHours=24
Notificaciones__EnvioIntervalMinutes=5
Notificaciones__EnvioBatchSize=50
Twilio__Enabled=false
Twilio__WorkerEnabled=false
Twilio__SmokeTestEnabled=false
Twilio__AdditionalTypesEnabled=false
WhatsApp__CampaignsEnabled=false
WhatsApp__MaxRecipientsPerCampaign=200
WhatsApp__CampaignBatchSize=20
WhatsApp__CampaignDelayMilliseconds=250
BootstrapAdmin__Enabled=false
```

Secretos Railway:

```text
MongoDB__ConnectionString
Jwt__Key
Twilio__AccountSid
Twilio__AuthToken
Twilio__WhatsAppFromNumber
Twilio__PorVencerContentSid
Twilio__AuthorizedTestNumber
```

`Jwt__Key` debe ser aleatoria y tener al menos 32 caracteres. En Railway los valores de Twilio se cargan como variables selladas; nunca se imprimen. `PORT` lo aporta Railway y no se configura manualmente.

### Migración e índices, una sola vez

Antes del primer despliegue productivo, agregar temporalmente este **Pre-deploy Command** en Railway:

```text
dotnet GyMApi.dll --migrate-pilot-gym
```

Desplegar una vez, comprobar en logs sólo los conteos (nunca documentos), y eliminar el comando inmediatamente. La migración es idempotente; completa `GymId`, normaliza emails, renombra la fecha histórica de consentimiento y crea índices. Los arranques normales vuelven a validar/crear índices de forma idempotente.

### Bootstrap seguro

Para el primer arranque establecer temporalmente como secretos:

```text
BootstrapAdmin__Enabled=true
BootstrapAdmin__Nombre=<nombre>
BootstrapAdmin__Email=<email>
BootstrapAdmin__Password=<contraseña larga y única>
```

Tras verificar el login, establecer `BootstrapAdmin__Enabled=false`, borrar email/contraseña y redesplegar. El bootstrap no reemplaza usuarios ni contraseñas existentes. Si se desea un segundo Admin, el primero lo crea desde Administración y luego se verifica su rol; no se vuelve a habilitar el bootstrap.

## 3. Cloudflare Pages — frontend

Crear un proyecto Pages llamado, por ejemplo, `gym-manager-piloto`. El workflow `.github/workflows/deploy-frontend-cloudflare.yml` compila Flutter y publica `frontend/build/web`; `frontend/web/_redirects` aporta fallback SPA.

Configurar en el environment GitHub `production`:

```text
Variable API_BASE_URL=https://<backend>.up.railway.app
Variable CLOUDFLARE_PAGES_PROJECT=gym-manager-piloto
Secret CLOUDFLARE_ACCOUNT_ID=<account id>
Secret CLOUDFLARE_API_TOKEN=<token limitado a Pages:Edit>
```

El build falla si `API_BASE_URL` está vacío o no usa HTTPS. Localhost sólo es el valor de desarrollo y nunca se incluye en el artefacto productivo cuando se ejecuta el workflow.

Después de obtener la URL `https://<proyecto>.pages.dev`, actualizar `Cors__AllowedOrigins__0` en Railway y redesplegar. No usar comodines. Un dominio propio es opcional y requiere autorización separada antes de comprarlo.

## 4. Twilio controlado

El producto vigente genera únicamente `PorVencer`; `Vencido` y `PagoConfirmado` permanecen desactivados para ahorrar costos. Por lo tanto el smoke autorizado consiste en **un solo mensaje PorVencer**, no uno de cada tipo histórico.

1. Mantener `Twilio__WorkerEnabled=false`.
2. Confirmar remitente, plantilla aprobada, cuenta paga y número de prueba con consentimiento.
3. Establecer temporalmente `Twilio__Enabled=true`, `Twilio__SmokeTestEnabled=true`, `Twilio__PaidAccountConfirmed=true` y `Twilio__TemplatesApprovedConfirmed=true`.
4. Consultar `GET /api/admin/whatsapp/prueba`; detenerse si enumera una intervención pendiente.
5. Con autorización expresa de cargo, ejecutar el POST administrativo una vez.
6. Confirmar `AceptadoPorTwilio` y `ProviderMessageId`. No significa entregado.
7. Volver a `Twilio__SmokeTestEnabled=false`. Habilitar `WorkerEnabled=true` sólo después de auditar pendientes.

## 5. Smoke tests de despliegue

- `/health/live` devuelve `Healthy`.
- `/health/ready` devuelve MongoDB `Healthy`.
- Login Admin, recarga de página, guarda de ruta y logout.
- Crear/editar una sucursal y un alumno de prueba; desactivarlos o identificarlos para limpieza.
- Crear categoría, registrar un pago y verificar vigencia/estado calculado.
- Ejecutar el job con sender falso primero; verificar deduplicación `PorVencer`.
- Revisar historial, contadores y resumen de `PilotEvents`.
- Verificar aislamiento con los tests automatizados; no crear un segundo tenant productivo.
- Hacer el único envío Twilio sólo tras autorización expresa.

## Logs, rollback y respaldo

Los logs JSON salen a stdout. Buscar por `CorrelationId` en Railway y usar el mismo identificador en historial/eventos. No copiar JWT, AuthToken, URI Mongo ni cuerpos completos a tickets.

Rollback simple: en Railway seleccionar el despliegue anterior y **Rollback** (Hobby conserva imágenes eliminadas 72 horas); en Cloudflare Pages elegir el deployment anterior y promoverlo. Un rollback de código no revierte datos: migraciones destructivas están fuera de alcance y la migración actual es aditiva/idempotente.

Atlas M0 no tiene backup administrado. Antes de cada release y semanalmente ejecutar `mongodump` desde un equipo autorizado, leyendo la URI desde un secreto local y guardando el resultado cifrado fuera del repositorio:

```powershell
mongodump --uri $env:GYM_BACKUP_MONGODB_URI --db GymManagerPilot --archive=GymManagerPilot.archive --gzip
```

Probar restauración periódicamente en una base distinta con `mongorestore --archive ... --gzip --nsFrom "GymManagerPilot.*" --nsTo "GymManagerRestoreTest.*"`. Nunca restaurar encima de producción durante una prueba.

## URLs finales

Completar después de provisionar:

```text
Frontend: pendiente — https://<proyecto>.pages.dev
Backend: pendiente — https://<servicio>.up.railway.app
Health:  pendiente — https://<servicio>.up.railway.app/health/ready
```
