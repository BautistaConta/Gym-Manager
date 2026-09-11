# Gym Manager

Aplicación de gestión de gimnasios con autenticación, roles, alumnos, sucursales, pagos y notificaciones.

## Estructura

- `/frontend`: aplicación Flutter con Riverpod, compatible con Web.
- `/backend/GyMApi`: ASP.NET Core Web API sobre .NET 8.
- Persistencia: MongoDB.

## Preparación multi-tenant

Los documentos de usuarios, alumnos, sucursales, categorías de pago, pagos y notificaciones incluyen un `GymId` obligatorio. En endpoints autenticados se obtiene exclusivamente del claim `gym_id` del JWT; no se acepta desde body ni query.

Durante la etapa piloto, `MultiTenancy:PilotGymId` identifica al único gimnasio para login, registro y background jobs. Puede tomarse `backend/GyMApi/appsettings.Example.json` como referencia de configuración. Los jobs usan ese valor de forma deliberada; cuando exista multi-tenant real deberá reemplazarse por una iteración explícita de tenants.

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
