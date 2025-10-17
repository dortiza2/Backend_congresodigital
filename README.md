# Backend Congreso Digital (.NET 8 API)

Este repositorio contiene la API de Congreso Digital lista para ejecución local y despliegue en producción (Render).

## Estructura esperada

- `Congreso.Api/` (proyecto ASP.NET Core .NET 8)
- `db/` (migraciones y seguridad de BD auxiliares)
- `scripts/` (utilidades de despliegue y validación)
- `Congreso.sln` (solución)
- `render.yaml` (configuración de Render)

## Requisitos

- .NET SDK 8.x
- PostgreSQL 14+ (o servicio compatible)
- Git

Opcional:
- `dotnet-ef` para ejecutar migraciones manualmente: `dotnet tool install --global dotnet-ef`

## Configuración local

1) Restaurar dependencias y compilar

```
# Desde la raíz del repo
 dotnet restore Congreso.Api/Congreso.Api.csproj
 dotnet build Congreso.Api/Congreso.Api.csproj -c Debug
```

2) Variables de entorno (local)

Configura un archivo `Congreso.Api/appsettings.Development.json` (ya incluido) y/o variables de entorno equivalentes:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ConnectionStrings__DefaultConnection` (cadena a PostgreSQL)
- Cualquier secreto de email/JWT usado por el proyecto (ver Program.cs/Configuration/Services)

3) Migraciones de base de datos (opcional, si se aplican con EF)

```
# Si usas EF Migrations del proyecto
 dotnet ef database update --project Congreso.Api/Congreso.Api.csproj
```

4) Ejecutar la API

```
# Opción 1 (desde raíz)
 dotnet run --project Congreso.Api/Congreso.Api.csproj

# Opción 2 (dentro de la carpeta del proyecto)
 cd Congreso.Api
 dotnet run
```

La API suele exponerse en `http://localhost:5000` o `http://localhost:5174`/`http://localhost:8080` dependiendo de `ASPNETCORE_URLS`/`launchSettings.json`.

## Despliegue en Render

Este repo incluye `render.yaml`. Pasos recomendados:

1) Subir el repo a GitHub y conectarlo a Render.
2) Crear un Web Service (Docker o Build Command) según lo definido en `render.yaml`.
3) Configurar variables de entorno en Render (Producción):
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionStrings__DefaultConnection` (cadena a PostgreSQL gestionado)
   - Cualquier secreto necesario (JWT/Email/Storage, etc.)
4) Deploy. Puedes usar los scripts de `scripts/` para validaciones:
   - `scripts/setup-secrets.sh` (plantillas de variables)
   - `scripts/validate-deployment.sh` (chequeos básicos)

Si Render usa contenedor, la publicación típica es:

```
dotnet publish Congreso.Api/Congreso.Api.csproj -c Release -o out
# Entrada del contenedor (ejemplo)
dotnet /app/Congreso.Api.dll
```

## Comandos útiles

- Ejecutar pruebas (si existen):
```
dotnet test Congreso.Api/Congreso.Api.csproj
```

- Aplicar migraciones (EF):
```
dotnet ef migrations add <Nombre> --project Congreso.Api/Congreso.Api.csproj
```

## Estructura final esperada en el root

```
Congreso.Api/
db/
scripts/
Congreso.sln
render.yaml
```

Si tu estructura difiere, reorganiza según la sección siguiente.

## Reorganización rápida (si es necesario)

Ejecuta estos comandos desde la raíz del repo para dejar sólo el backend listo:

```
# 1) Mover backend al raíz
mv "Archivo/Congreso.Api" ./Congreso.Api
mv "Archivo/db" ./db

# 2) Copiar scripts y archivos de solución/render
cp -a "Congreso_tecnologico_proyectoD/scripts" ./scripts
cp -f "Congreso_tecnologico_proyectoD/Congreso.sln" ./Congreso.sln
cp -f "Congreso_tecnologico_proyectoD/render.yaml" ./render.yaml

# 3) Aislar frontend/opcional en frontdelete y marcar contenedores para borrar luego
mkdir -p frontdelete
[ -d "Archivo/landing" ] && mv "Archivo/landing" frontdelete/
[ -d "Archivo/documents" ] && mv "Archivo/documents" frontdelete/
[ -d "Archivo/tools" ] && mv "Archivo/tools" frontdelete/
[ -f "Congreso_tecnologico_proyectoD/vercel.json" ] && mv "Congreso_tecnologico_proyectoD/vercel.json" frontdelete/
[ -f "Congreso_tecnologico_proyectoD/package-lock.json" ] && mv "Congreso_tecnologico_proyectoD/package-lock.json" frontdelete/

# 4) Renombrar carpetas monorepo para eliminación manual segura
[ -d Archivo ] && mv Archivo Archivo.trash || true
[ -d Congreso_tecnologico_proyectoD ] && mv Congreso_tecnologico_proyectoD Congreso_tecnologico_proyectoD.trash || true

# 5) Verificación rápida
for i in Congreso.Api db scripts Congreso.sln render.yaml; do
  if [ -e "$i" ]; then echo "OK $i"; else echo "FALTA $i"; fi
done
```

## Git y primer push

```
# Desde la raíz del repo
git init
git add .
git commit -m "chore: initial clean backend structure (API .NET 8)"
git branch -M main
# Cambia la URL si usas otro remoto
git remote add origin https://github.com/dortiza2/Backend_congresodigital.git
git push -u origin main
```

---

Si necesitas que automatice estos pasos aquí mismo, dímelo y lo ejecuto en bloques pequeños para evitar timeouts