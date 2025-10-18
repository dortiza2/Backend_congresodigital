# Instrucciones de Despliegue (Render)

Este documento resume los cambios relevantes y especifica el proceso recomendado para desplegar la API en Render, incluyendo variables de entorno, comandos de verificación y soluciones a problemas comunes.

## 1) Resumen de cambios recientes
- Swagger: se ocultó/limitó la exposición del controlador de imágenes en Swagger (evitar operaciones sensibles en producción). En producción, Swagger puede estar deshabilitado salvo que se habilite explícitamente.
- Rutas duplicadas: se ajustaron rutas para eliminar conflictos y respuestas 404/ambigua por atributos duplicados.
- Configuración por entorno: se añadieron y documentaron variables de entorno para ejecución local y despliegue (`ASPNETCORE_*`, `ConnectionStrings`, `JWT`, `Cloudinary`, etc.). También se incluyó `SEED_MINIMAL=true` para un seed reducido en desarrollo.

## 2) Pasos para desplegar en Render

### A. Crear el servicio Web
- Tipo: Web Service
- Runtime: .NET (usar .NET 9 si está disponible; alternativamente usar Docker o Nixpacks).
- Repositorio: conectar el repositorio que contiene este proyecto.
- Directorio de trabajo: raíz del repo (este archivo está en la raíz). El proyecto de la API está en `Archivo/Congreso.Api`.

### B. Comandos de build y start
- Build Command:
  - `dotnet restore` \
    `dotnet build Archivo/Congreso.Api -c Release`
- Start Command:
  - `ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet Archivo/Congreso.Api/bin/Release/net9.0/Congreso.Api.dll`

Notas:
- Render expone el puerto en la variable `PORT`. Es obligatorio enlazar a `0.0.0.0:$PORT` para evitar 502/tiempos de espera.
- Si decides usar `dotnet run`, hazlo solo si la plataforma lo soporta; en Render suele ser más estable invocar el DLL de Release.

### C. Aplicar migraciones (recomendado)
Hay dos enfoques:
1) Pre Deploy Command (si tu plan de Render lo soporta):
   - `dotnet tool install --global dotnet-ef` \
     `export PATH="$PATH:$HOME/.dotnet/tools"` \
     `dotnet ef database update --project Archivo/Congreso.Api`
2) Auto-migración al arranque: si ya tienes lógica de `ApplyMigrations()` en `Program.cs`, verifica logs. Si aparecen advertencias como "PendingModelChangesWarning", genera una migración nueva y vuelve a aplicar.

## 3) Variables de entorno
Configura en Render (Environment > Variables) lo siguiente:

- Aplicación:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ASPNETCORE_URLS=http://0.0.0.0:$PORT` (Render sobrescribe `PORT`; mantenlo así)

- Base de datos (elige UNA estrategia):
  - Opción A (recomendada en Render): `DATABASE_URL=postgres://USER:PASSWORD@HOST:5432/DBNAME?sslmode=require`
  - Opción B: variables por `ConnectionStrings`:
    - `ConnectionStrings__Default=Host=HOST;Port=5432;Database=DBNAME;Username=USER;Password=PASSWORD;SSL Mode=Require;Trust Server Certificate=true`
    - `ConnectionStrings__DefaultConnection=` (igual a la anterior, si el código la lee explícitamente)

- JWT:
  - `JWT_ISSUER=https://tu-dominio`
  - `JWT_AUDIENCE=https://tu-dominio`
  - `JWT_SECRET=<cadena-secreta-larga>`
  - `JWT_EXPIRES_MINUTES=60` (o el valor deseado)

- Cloudinary (si aplica):
  - `CLOUDINARY_CLOUD_NAME=<cloud_name>`
  - `CLOUDINARY_API_KEY=<api_key>`
  - `CLOUDINARY_API_SECRET=<api_secret>`
  - Alternativa: `CLOUDINARY_URL=cloudinary://<api_key>:<api_secret>@<cloud_name>`

- Otras:
  - `SEED_MINIMAL=false` en producción (recomendado). En dev se puede usar `true`.

## 4) Comandos de verificación
Una vez desplegado, verifica:

- Salud:
  - `curl -i "$RENDER_EXTERNAL_URL/api/health"`
  - Esperas `200 OK` y un JSON con `{ ok: true, ... }`.

- Endpoint de negocio (ejemplo):
  - `curl -i "$RENDER_EXTERNAL_URL/api/podium?year=2024"`
  - Debe responder `200 OK`. Si retorna `[]`, no hay datos; verifica seed/migraciones.

- Logs en Render: usa el panel de Logs del servicio para confirmar:
  - "Now listening on: http://0.0.0.0:PORT"
  - "Application started."
  - Cualquier mensaje de migraciones/seed.

## 5) Solución de problemas comunes

- 502 o tiempo de espera:
  - Asegúrate de que el Start Command enlace `http://0.0.0.0:$PORT`.
  - Confirma que la app no se detiene por excepciones durante el arranque.

- "DefaultConnection not configured":
  - Define `DATABASE_URL` o `ConnectionStrings__Default`/`__DefaultConnection` en Render.
  - Revisa `Program.cs` para ver la prioridad de resolución de cadena (normalmente `DATABASE_URL`).

- Error SSL con Postgres:
  - En Render suele requerirse `sslmode=require`. Asegúrate de usar `?sslmode=require` en `DATABASE_URL` o `SSL Mode=Require` en `ConnectionStrings`.

- `PendingModelChangesWarning` y fallos al guardar durante el seed:
  - Genera y aplica migraciones:
    - Local: `cd Archivo/Congreso.Api && dotnet ef migrations add SyncModel && dotnet ef database update`
    - En Render: usa un Pre Deploy Command (ver sección 2C) o ejecuta un Job temporal.

- 404 o rutas ambiguas:
  - Revisa atributos de ruta en controladores y asegúrate de no tener duplicados.
  - Limpia rutas legacy que solapen con nuevas.

- Swagger en producción:
  - Por seguridad suele estar deshabilitado en `Production`. Si necesitas habilitar, añade una bandera de entorno controlada (p. ej. `ENABLE_SWAGGER=true`) y ajústalo en `Program.cs` solo para entornos confiables.

## Notas adicionales
- Mantén secretos fuera del repo. Usa variables de entorno de Render.
- Si tu base de datos Render es "Managed PostgreSQL", usa las credenciales y `DATABASE_URL` que te provee Render.
- Evita seeds agresivos en producción. Usa `SEED_MINIMAL=false` y/o seeds idempotentes.
- Si empleas CDN/Cloudinary, valida que las variables estén bien definidas antes de habilitar endpoints relacionados.

---

## 6) Despliegue de Congreso.Api (Render)

Este proyecto minimal expone health checks, endpoints públicos de actividades y un emisor de JWT para pruebas. Si deseas desplegarlo en Render como un servicio separado, usa esta guía.

### A. Comandos de build y start
- Build Command:
  - `dotnet restore` \
    `dotnet build Archivo/Congreso.Api -c Release`
- Start Command:
  - `ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet Archivo/Congreso.Api/bin/Release/net9.0/Congreso.Api.dll`

### B. Variables de entorno (obligatorias)
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://0.0.0.0:$PORT`
- `DATABASE_URL` (elige UNA forma):
  - URI: `postgres://USER:PASSWORD@HOST:5432/DB?sslmode=require` (evita saltos de línea en Render)
  - Key-value (recomendado si tienes problemas con URI):
    `Host=HOST;Port=5432;Database=DB;Username=USER;Password=PASS;SSL Mode=Require;Trust Server Certificate=true`
- JWT:
  - `JWT_ISSUER=https://tu-dominio-o-url-del-servicio`
  - `JWT_AUDIENCE=https://tu-dominio-o-url-del-servicio`
  - `JWT_SECRET_KEY` (mínimo 32 bytes/256 bits). Si ves `IDX10720`, usa una clave más larga.
- CORS (opcional):
  - `CORS_ORIGINS=https://tus-frontends.com,https://otro-dominio.com`

### C. Endpoints de verificación (smoke tests)
- Salud general: `curl -i "$RENDER_EXTERNAL_URL/api/health"` (200)
- Salud BD: `curl -i "$RENDER_EXTERNAL_URL/health/db"` (200)
- Token de prueba:
  - `curl -i -X POST "$RENDER_EXTERNAL_URL/api/auth/token" -H 'Content-Type: application/json' -d '{"Email":"admin@congreso.com","Name":"Admin Seed","UserId":"550e8400-e29b-41d4-a716-446655440014","Role":"admin"}'`
- Perfil (protegido):
  - `curl -s "$RENDER_EXTERNAL_URL/api/auth/token" ... | jq -r .access_token` → exporta `TOKEN`
  - `curl -i "$RENDER_EXTERNAL_URL/api/profile/me" -H "Authorization: Bearer $TOKEN"`
- Actividades públicas: `curl -i "$RENDER_EXTERNAL_URL/api/activities/upcoming"`

### D. Problemas comunes
- `503 Unhealthy` en `/health/db`:
  - Verifica que `DATABASE_URL` no tenga saltos de línea y que incluya `sslmode=require` (o usa el formato key=value anterior).
- `500` al emitir token: suele ser `JWT_SECRET_KEY` demasiado corta (HS256 requiere ≥ 256 bits).
- CORS bloqueando llamadas desde el frontend: define `CORS_ORIGINS` con los dominios finales (sin comodines en producción).
