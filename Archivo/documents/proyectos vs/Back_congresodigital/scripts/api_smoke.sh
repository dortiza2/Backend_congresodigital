#!/usr/bin/env bash
set -euo pipefail

# Configuración
FRONTEND_URL=${FRONTEND_URL:-"http://localhost:3010"}
BACKEND_URL=${BACKEND_URL:-"http://localhost:5213"}
CONNECT_TIMEOUT=${CONNECT_TIMEOUT:-2}
TOTAL_TIMEOUT=${TOTAL_TIMEOUT:-4}

CURL_BASE=(curl -sS -o /dev/null --connect-timeout "$CONNECT_TIMEOUT" --max-time "$TOTAL_TIMEOUT" -w "%{http_code} %{time_total} %{url_effective}\n")

print_section() {
  printf "\n==== %s ====\n" "$1"
}

check() {
  local url=$1
  "${CURL_BASE[@]}" "$url" || echo "ERR curl $url"
}

print_section "Config"
echo "FRONTEND_URL=$FRONTEND_URL"
echo "BACKEND_URL=$BACKEND_URL"
echo "CONNECT_TIMEOUT=$CONNECT_TIMEOUT TOTAL_TIMEOUT=$TOTAL_TIMEOUT"

print_section "Frontend"
check "$FRONTEND_URL/"
check "$FRONTEND_URL/api/auth/me"
check "$FRONTEND_URL/api/auth/session"
check "$FRONTEND_URL/api/auth/login"   # debería ser proxy al backend por rewrites
check "$FRONTEND_URL/api/auth/register" # debería ser proxy al backend por rewrites

print_section "Backend Health"
check "$BACKEND_URL/api/health"
check "$BACKEND_URL/healthz"
check "$BACKEND_URL/ready"
check "$BACKEND_URL/api/Diagnostics/healthz"
check "$BACKEND_URL/api/PublicPodiums/health"

print_section "Backend Auth superficial"
"${CURL_BASE[@]}" -X GET "$BACKEND_URL/api/auth/login" || echo "ERR curl GET /api/auth/login"
"${CURL_BASE[@]}" -X GET "$BACKEND_URL/api/auth/register" || echo "ERR curl GET /api/auth/register"

print_section "Puertos activos"
# nc retorna 0 si puerto abierto, 1 si cerrado
for p in 3010 5001 5213 5432 8080; do
  if nc -z -w 1 localhost "$p" 2>/dev/null; then
    echo "OPEN :$p"
  else
    echo