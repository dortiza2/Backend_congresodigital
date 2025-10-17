# Congreso Neon – Base de Datos (PostgreSQL)  
**Archivo origen:** `CongresoNeon_v1.sql`  
**Motor:** PostgreSQL (Neon) – esquema `public`  
**Fecha del volcado:** 2025‑10‑17

> Este documento resume la **estructura**, **tipos**, **índices**, **vistas** y **datos de ejemplo** para que herramientas como **TRAE** puedan entender y consultar la BD sin necesidad de interpretar directamente el `.sql`.

---

## Tipos ENUM

- `activity_kind`: `talk`, `activity`
- `enrollment_status`: `active`, `canceled`, `attended`, `confirmed`
- `user_role`: `student`, `assistant`, `admin`, `devadmin`

---

## Tablas (por orden alfabético)

> ⚠️ Nota: en la mayoría de tablas el campo `id` existe y tiene `DEFAULT nextval(...)`, pero **no se declararon restricciones PRIMARY KEY** explícitas (excepto `__EFMigrationsHistory`). En consultas y relaciones se asume `id` como PK.

### `__EFMigrationsHistory`
- **Campos:** `MigrationId (varchar150)`, `ProductVersion (varchar32)`  
- **PK:** `MigrationId`  
- **Propósito:** historial de migraciones EF Core.

### `activities`
- **Campos:**  
  `id (bigint, seq)`, `kind (activity_kind)`, `place (text)`, `title (text)`,
  `note (text)`, `speaker_id (bigint, nullable)`,  
  `starts_at (timestamptz)`, `ends_at (timestamptz)`, `capacity (int)`,  
  `is_enabled (bool, default true)`, `created_at (timestamptz, default now())`,  
  `room_id (bigint, nullable)`
- **Índices:**  
  - `ix_activities_dates` sobre `(starts_at, ends_at)`  
  - `ix_activities_speaker` sobre `speaker_id`  
  - `ux_activities_title` **UNIQUE** sobre `title`
- **Semántica:** catálogo de charlas/talleres/eventos.

### `activity_speakers`
- **Campos:** `activity_id (bigint)`, `speaker_id (bigint)`  
- **Semántica:** relación N‑a‑N entre `activities` y `speakers`.

### `activity_tags`
- **Campos:** `activity_id (bigint)`, `tag_id (bigint)`  
- **Semántica:** relación N‑a‑N entre `activities` y `tags`.

### `audit_logs`
- **Campos:**  
  `id (bigint, seq)`, `user_id (bigint, null)`, `action (text)`,  
  `table_name (text, null)`, `record_id (bigint, null)`,  
  `payload (jsonb, null)`, `created_at (timestamptz, default now())`,  
  `old_data (jsonb, null)`, `new_data (jsonb, null)`
- **Semántica:** bitácora de cambios (auditoría).

### `certificates`
- **Campos:**  
  `id (bigint, seq)`, `enrollment_id (bigint)`, `issued_at (timestamptz, default now())`,  
  `code (text)`, `delivered_at (timestamptz, null)`, `file_url (text, null)`, `notes (text, null)`

### `enrollment_audit`
- **Campos:**  
  `id (bigint, seq)`, `enrollment_id (bigint)`, `changed_at (timestamptz, default now())`,  
  `old_status (enrollment_status, null)`, `new_status (enrollment_status, null)`,  
  `changed_by (bigint, null)`

### `enrollments`
- **Campos:**  
  `id (bigint, seq)`, `user_id (bigint)`, `activity_id (bigint)`,  
  `status (enrollment_status, default 'active')`,  
  `canceled_at (timestamptz, null)`, `cancel_reason (text, null)`,  
  `created_at (timestamptz, default now())`
- **Semántica:** inscripciones de usuarios a actividades.

### `faq_items`
- **Campos:** `id (int, seq)`, `question (text)`, `answer (text)`, `published (bool, default true)`, `position (int, default 0)`

### `invitations`
- **Campos:** `id (bigint, seq)`, `activity_id (bigint, null)`, `email (text)`, `token (text)`, `expires_at (timestamptz)`, `accepted_at (timestamptz, null)`

### `outbox_events`
- **Campos:**  
  `id (bigint, seq)`, `topic (text)`, `aggregate_type (text)`, `aggregate_id (bigint, null)`,  
  `payload (jsonb, null)`, `status (text, default 'pending')`,  
  `created_at (timestamptz, default now())`, `processed_at (timestamptz, null)`
- **Semántica:** patrón Outbox para integración/colero de eventos.

### `password_reset_tokens`
- **Campos:** `id (bigint, seq)`, `user_id (bigint)`, `token_hash (text)`, `created_at (timestamptz, default now())`, `expires_at (timestamptz)`, `attempts (int, default 0)`

### `podiums`
- **Campos:** `id (bigint, seq)`, `activity_id (bigint)`, `user_id (bigint)`, `position (smallint)`, `year (smallint, default extract(year from current_date))`, `created_at (timestamptz, default now())`
- **Semántica:** podios/ganadores por actividad y año.

### `roles`
- **Campos:** `id (int, seq)`, `code (text)`, `name (text, null)`, `label (text, null)`, `level (smallint, default 1)`
- **Semántica:** catálogo de roles “staff” auxiliares (diferente de `user_role`).

### `rooms`
- **Campos:** `id (bigint, seq)`, `name (text)`, `location (text, null)`, `capacity (int, null)`, `is_active (bool, default true)`

### `speakers`
- **Campos:** `id (bigint, seq)`, `full_name (text)`, `bio (text, null)`, `company (text, null)`, `role_title (text, null)`, `avatar_url (text, null)`, `links (jsonb, default '{}')`, `created_at (timestamptz, default now())`

### `tags`
- **Campos:** `id (bigint, seq)`, `name (text)`

### `user_roles`
- **Campos:** `user_id (bigint)`, `role_id (int)`, `user_id_uuid (uuid, null)`
- **Semántica:** mapeo adicional a `roles` (no confundir con `users.role`).

### `users`
- **Campos:**  
  `id (bigint, seq)`, `email (text)`, `full_name (text)`, `password_hash (text, null)`,  
  `is_active (bool, default true)`, `created_at (timestamptz, default now())`,  
  `last_login_at (timestamptz, null)`, `role (user_role, default 'student')`,  
  `role_level (smallint, generado a partir de 'role': student 0, assistant 1, admin 2, devadmin 3)`,  
  `avatar_url (text, null)`, `status (varchar50, default 'active')`,  
  `is_umg (bool, default false)`, `org_id (uuid, null)`, `org_name (varchar255, null)`,  
  `updated_at (timestamptz, null)`, `id_guid (uuid, null)`

### `waitlists`
- **Campos:** `id (bigint, seq)`, `activity_id (bigint)`, `user_id (bigint)`, `created_at (timestamptz, default now())`, `promoted_at (timestamptz, null)`, `notes (text, null)`

---

## Relaciones (lógicas)

- `enrollments.activity_id → activities.id`  
- `enrollments.user_id → users.id`  
- `certificates.enrollment_id → enrollments.id`  
- `podiums.activity_id → activities.id`, `podiums.user_id → users.id`  
- `activity_speakers.activity_id → activities.id`, `activity_speakers.speaker_id → speakers.id`  
- `activity_tags.activity_id → activities.id`, `activity_tags.tag_id → tags.id`  
- `waitlists.activity_id → activities.id`, `waitlists.user_id → users.id`  
- `user_roles.user_id → users.id`, `user_roles.role_id → roles.id`  

> **No hay FKs declaradas en el SQL**; si es necesario forzar integridad referencial, se pueden crear `FOREIGN KEY` posteriores.

---

## Vistas principales

- `v_enrollments_extended`: `enrollments` + `activities` (título, tipo, horarios).  
- `v_user_activity_last_status`: última inscripción por usuario/actividad.  
- `v_enrollment_status_order`: orden lógico del enum de estados.  
- `v_podiums`: `podiums` + `activities` + `users`.  
- `v_outbox_pending`: eventos en estado `pending`.  
- `vw_activities`: selección básica de `activities`.  
- `vw_enrollments` y `vw_user_enrollments`: inscripciones con datos de actividad (y orden).  
- `v_certificates`: certificados con usuario y actividad.  
- `v_outbox_stats`: totales de `outbox_events` por `status`.  
- `v_audit_recent`: últimos registros de `audit_logs`.  
- `v_health`: chequeo rápido (`now()` y conteo de outbox pendientes).  
- `vw_public_speakers`: proyección pública de `speakers`.  
- `vw_podium_by_year`: podio por año con aliases “friendly”.  
- `vw_public_activities`: proyección pública de actividades con cupos calculados.

---

## Índices relevantes

- `activities`: fechas (`starts_at`, `ends_at`), por `speaker_id`, y **único por `title`**.
- No se declaran otros índices en el script.

---

## Secuencias
> Todas las tablas con `id` usan una secuencia propia (`*_id_seq`) y el script deja el `setval()` en los siguientes valores iniciales:  

`activities: 24` · `audit_logs: 99` · `certificates: 5` · `enrollment_audit: 0` ·  
`enrollments: 9` · `faq_items: 2` · `invitations: 0` · `outbox_events: 7` ·  
`password_reset_tokens: 0` · `podiums: 5` · `roles: 3` · `rooms: 5` ·  
`speakers: 5` · `tags: 5` · `users: 20` · `waitlists: 6`

---

## Datos de ejemplo (semillas destacadas)

- **activities:** 24 registros (charlas/talleres de 2023‑2025).  
- **users:** usuarios demo (alumnos y staff) con `role` y `role_level` calculado.  
- **enrollments / waitlists:** varias inscripciones y listas de espera.  
- **podiums:** posiciones de ganadores.  
- **faq_items, roles, rooms, tags:** catálogos.  
- **audit_logs / outbox_events:** actividad de ejemplo para pruebas.

---

## Consultas útiles (copiar/pegar)

```sql
-- Próximas actividades publicadas
SELECT id, title, kind, starts_at, ends_at, capacity
FROM vw_public_activities
WHERE starts_at > now()
ORDER BY starts_at;

-- Inscripciones por usuario con estado más reciente
SELECT * FROM v_user_activity_last_status
WHERE user_id = $1
ORDER BY created_at DESC;

-- Cupos disponibles por actividad (versión pública ya lo trae)
SELECT a.id, a.title,
       COALESCE(a.capacity,0) AS capacity,
       COUNT(e.id) FILTER (WHERE e.status IN ('active','confirmed')) AS enrolled,
       GREATEST(COALESCE(a.capacity,0) - COUNT(e.id) FILTER (WHERE e.status IN ('active','confirmed')), 0) AS available
FROM activities a
LEFT JOIN enrollments e ON e.activity_id = a.id
GROUP BY a.id, a.title, a.capacity
ORDER BY a.starts_at;

-- Outbox pendiente
SELECT * FROM v_outbox_pending ORDER BY created_at;

-- Auditoría reciente
SELECT * FROM v_audit_recent LIMIT 100;
```
---

## Notas / posibles mejoras
- Añadir **PRIMARY KEY** y **FOREIGN KEY** explícitos para reforzar integridad.  
- Considerar índices en `enrollments (user_id, activity_id, status)` y en `outbox_events (status, created_at)` si crece el volumen.  
- Si se exponen vistas públicas, validar permisos `GRANT`/`REVOKE` según rol de conexión.
