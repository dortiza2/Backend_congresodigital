#!/usr/bin/env bash
set -euo pipefail

API_BASE=${API_BASE:-http://127.0.0.1:5213}

echo "[1/3] /health/db"
curl -fsS "$API_BASE/health/db" >/dev/null && echo "OK"

echo "[2/3] /api/_diag/db/summary"
curl -fsS "$API_BASE/api/_diag/db/summary" | jq .

echo "[3/3] /api/_diag/db/preview"
curl -fsS "$API_BASE/api/_diag/db/preview" | jq .

echo "Smoke OK"
# [4/5] /api/admin/outbox/pending
echo "[4/5] /api/admin/outbox/pending"
curl -fsS "$API_BASE/api/admin/outbox/pending" | jq .

# [5/5] /api/admin/outbox/stats
echo "[5/5] /api/admin/outbox/stats"
curl -fsS "$API_BASE/api/admin/outbox/stats" | jq .