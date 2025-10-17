#!/bin/bash
# Configura variables de entorno para desarrollo local (macOS/Linux)

# Usar en la misma shell con: source ./setup-dev-env.sh

# --- API ---
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5010

# --- Base de datos Postgres ---
export DB_HOST=127.0.0.1
export DB_PORT=5432
export DB_NAME=congreso_dev
export DB_USER=user_congreso
export DB_PASSWORD=congreso123

# Alternativa: usa DATABASE_URL si prefieres una sola variable
# export DATABASE_URL="postgres://user_congreso:congreso123@127.0.0.1:5432/congreso_dev"

# --- ConnectionStrings para IConfiguration ---
export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=congreso_dev;Username=user_congreso;Password=congreso123;SearchPath=public"
export ConnectionStrings__Default="Host=127.0.0.1;Port=5432;Database=congreso_dev;Username=user_congreso;Password=congreso123;SearchPath=public"

# --- Cloudinary (reemplaza con los tuyos) ---
export CLOUDINARY_CLOUD_NAME=""
export CLOUDINARY_API_KEY=""
export CLOUDINARY_API_SECRET=""

# --- JWT ---
export JWT_SECRET_KEY="dev_local_secret_key_cambia_esto"
export JWT_ISSUER="http://localhost:5010"
export JWT_AUDIENCE="http://localhost:5010"

# --- Semilla mínima de datos ---
export SEED_MINIMAL=true

printf "\nVariables preparadas para esta sesión de shell.\n"
printf "Para aplicarlas, ejecuta: source ./setup-dev-env.sh\n"
printf "Luego inicia la API:\n  cd Archivo/Congreso.Api\n  dotnet run --no-launch-profile --urls http://localhost:5010\n\n"