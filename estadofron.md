# Requisitos y estado funcional del Frontend

Objetivo: Documentar, de forma práctica y accionable, todo lo necesario para ejecutar y mantener el frontend del Congreso Digital, especificando datos enviados/recibidos, ubicación en la UI, validaciones, comportamientos esperados e interacciones entre componentes.


## 1) Requisitos previos y configuración
- Node.js 18+
- Instalar dependencias: `npm install`
- Arranque local: `npm run dev` en `http://localhost:3000`
- Variables de entorno mínimas (archivo `.env.local`):
  - `NEXT_PUBLIC_API_URL`: raíz del backend (ej. `https://congreso-api.onrender.com`)
  - `NEXTAUTH_URL`: URL pública del frontend (ej. `http://localhost:3000`)
  - `NEXTAUTH_SECRET`: secreto para firmar sesiones NextAuth
  - Opcional:
    - `NEXT_PUBLIC_ALLOWED_DOMAINS`: dominios permitidos para Google (coma-separado)
    - `NEXT_PUBLIC_USE_MOCK`: `true` para habilitar mocks en desarrollo (si se usa)

Fuentes:
- Configuración centralizada: `lib/appConfig.ts`
- Config API: `lib/apiConfig.ts`
- NextAuth: `pages/api/auth/[...nextauth].ts`


## 2) Arquitectura de autenticación y sesión
Componentes principales:
- Contexto: `contexts/AuthContext.tsx`
  - Expone `loginEmail`, `logout`, `refreshUser` y estado `user`/`loading`.
  - Revalida token periódicamente y limpia sesión si expira.
- Cliente de sesión: `lib/authClient.ts`
  - Claves de persistencia:
    - Cookie: `cd_jwt`
    - LocalStorage: `cd_token`, `cd_user`, `cd_user_roles`, `cd_user_id`, `cd_user_email`
  - Helpers clave: `getToken`, `getUser`, `isLoggedIn`, `clearSession`, `getRedirectPath`, `handleLoginSuccess` (routing post-login inteligente por rol y/o inscripciones).
- NextAuth (Google): `pages/api/auth/[...nextauth].ts` y `pages/api/auth/me.ts`
  - Sesión JWT con cookies `next-auth.*`.
  - Páginas configuradas: `signIn: '/inscripcion', error: '/inscripcion'`.
- Utilidades API: `lib/api.ts`, `lib/api-errors.ts`, `lib/errorHandler.tsx`
  - Clase `ApiError` y mapeo de errores de negocio a mensajes de UI.

Notas de compatibilidad:
- Existe código legacy que usa `localStorage.auth_token`. El flujo nuevo guarda cookie `cd_jwt` y usa `/api/*` relativos. Mantener ambos hasta completar la migración.


## 3) Contratos de datos: enviados y recibidos
Referencias: `services/auth.ts`, `services/activities.ts`, `services/enrollments.ts`, `types/ui.ts`, `lib/adapters.ts`.

3.1 Autenticación
- Login (POST `/api/auth/login`)
  - Request: `{ email: string; password: string }`
  - Respuesta DTO: `{ token?: string; expiresAt?: string; user: { id, email, fullName, isUmg, organizationName?, roleLevel, roles[] } }`
  - Adaptado a UI (`AuthUserUI`):
    - `id`, `email`, `fullName`, `isUmg`, `organizationName?`, `roleLevel` (0 student, 1 asistente, 2 admin, 3 adminDev), `roles: string[]`, opcionales `profileType`, `staffRole`, `image`, `createdAt`.
- Registro (POST `/api/auth/register`)
  - Request: `{ email, fullName, password, institution? }`
  - Respuesta DTO: `{ message, token?, expiresAt?, user: AuthUserDTO }`
  - Tras registro: se guardan token y usuario (login automático) y se redirige con `handleLoginSuccess`.
- Sesión (GET `/api/auth/session`)
  - Respuesta: `AuthUserDTO` o error 401.

3.2 Actividades (GET `/api/activities`)
- Query opcional: `from`, `to`, `type`
- UI Type (`ActivityUI`):
  - `id`, `title`, `type: 'CHARLA'|'TALLER'|'COMPETENCIA'`, `location`, `startsAt`, `endsAt`, `capacity`, `registeredCount`, `seatsLeft`, `isFull`, `isActive`
  - Derivados opcionales: `isSoon`, `isOngoing`, `isPast`, más `description`, `instructor`, `requirements`, `requiresEnrollment`.

3.3 Inscripciones
- Crear múltiples (POST `/api/enrollments` o wrapper equivalente)
  - Request: `{ activityIds: string[] }`
  - Response estándar (`EnrollmentResponse`): `{ success: boolean; data?: EnrollmentUI; error?: ErrorUI }`
  - En operación masiva, se usan estructuras con arrays: `enrollments: EnrollmentUI[]`, `errors?: string[]`.
- `EnrollmentUI` incluye: `id`, `activityId`, `activityTitle`, `activityType`, `location?`, `startTime`, `endTime`, `seatNumber?`, `qrToken`, `enrolledAt`, `attended?`, `instructor?`.

3.4 Errores de negocio (`lib/api-errors.ts`)
- Códigos mapeados a mensajes y severidades: `already_registered`, `no_seats_left`, `time_conflict`, `email_not_found`, `invalid_argument`, `duplicate_entry`, `service_unavailable`, `unauthorized`, `forbidden`, `not_found`, `validation_error`, `network_error`, etc.
- Interfaz `ErrorUI`: `{ code, message, severity: 'info'|'warning'|'error', correlationId?, action? }`.


## 4) Ubicación de datos en la interfaz
Pantalla principal de inscripción: `pages/inscripcion.tsx` (flujo por estados)
- Estados: `login` → `activities` → `summary` → `confirmation`.
- Secciones y datos:
  - Pestañas “Iniciar Sesión” y “Registrarse”.
    - Form login: `email`, `password`. Muestra errores bajo cada campo y un error general en el contenedor superior.
    - Form registro: `fullName`, `email`, `institution` (opcional), `password`, `confirmPassword`. Muestra errores por campo y general.
    - Mensajes de éxito se muestran arriba del botón (texto verde) cuando corresponde.
  - Listado de actividades (estado `activities`):
    - Cada tarjeta muestra: `title`, `type`, `location`, `startsAt/endsAt` (formateados), `seatsLeft` y bandera `isFull`.
    - Controles de filtro por tipo/fecha (si están visibles en la versión actual).
    - Selección múltiple: el usuario puede marcar/deseleccionar. Botón para continuar a `summary`.
  - Resumen (estado `summary`):
    - Lista compacta con las seleccionadas: título, horario, ubicación, total.
    - Botones para confirmar o volver.
  - Confirmación (estado `confirmation`):
    - Mensaje de éxito, links a “Mi Portal” (`/portal`) y botón “Imprimir confirmación”.

Portal del estudiante
- `pages/portal/inscripciones.tsx` (“Mis Inscripciones”):
  - Muestra tarjetas con: `activityTitle`, `activityType`, `location`, `start/end`, `seatNumber?` y QR.
  - Muestra total de inscripciones en el encabezado.
  - Estados: `loading` (spinner), `error` (caja roja) o lista vacía con mensaje amigable.

Rutas de acceso por rol (middleware y guardas)
- Estudiante (roleLevel 0): `/portal`, `/portal/inscripciones`, `/portal/qr`.
- Staff (≥1): `/staff`, `/dashboard`.
- Admin (≥2): `/admin`, `/admin/usuarios`, `/admin/actividades`.
- Middleware redirige según `roleLevel` y bloquea accesos cruzados.


## 5) Validaciones y formatos
Campos de autenticación
- Email: regex `APP_CONFIG.VALIDATION.EMAIL_REGEX`.
- Password (registro):
  - Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo (`services/auth.ts::AuthValidation`).
- Password (login):
  - Validación UI permite ≥6 caracteres (se recomienda unificar a política de 8+).
- Full name: 2–100 caracteres (`APP_CONFIG.VALIDATION.NAME_*`).
- Confirmar contraseña debe coincidir.

Actividades/Inscripciones
- No permitir seleccionar actividades `isFull`.
- Validación de conflictos de horario:
  - `validateTimeConflictWithUserEnrollments(ids)` y validaciones locales de choque entre seleccionadas.
  - Si hay conflicto: mostrar mensaje “Ya cuenta con una actividad o charla con el mismo horario.” y bloquear confirmación.
- Errores devueltos por el backend se mapean con `ApiError` → `getUserFriendlyErrorMessage` → Toast/alerta en UI.

Formatos de fecha/hora
- Utilidades: `lib/datetime.ts` (`formatGT`, `formatGTTime`) para presentar `startsAt/endsAt`.


## 6) Comportamientos esperados por componente/flujo
Inscripción (`pages/inscripcion.tsx`)
- Login con email:
  - Valida campos → llama `services/auth.login` → guarda sesión → llama `AuthContext.loginEmail` → redirige con `handleLoginSuccess(router, next)`.
  - Errores 401 muestran “Correo o contraseña incorrectos”. Otros: “Error de autenticación”.
- Login con Google:
  - Usa `signIn('google', { redirect: false })`. Si `NEXT_PUBLIC_ALLOWED_DOMAINS` está configurado, solo permite institucionales.
  - Tras éxito, redirige con `getRedirectPath(next)`.
- Registro:
  - Valida campos (incluida fortaleza de password) → `services/auth.register` → login automático → `handleLoginSuccess` con pequeño delay para mostrar “Registro exitoso…”.
- Selección de actividades:
  - Impide seleccionar si `isFull` o hay choque de horario.
  - Al confirmar, intenta inscripción masiva; muestra éxito parcial si algunas fallan.

Portal “Mis Inscripciones”
- GET `/api/users/me/enrollments` (proxy local que a su vez consulta `/api/student/enrollments`).
- Si el backend responde 4xx/5xx, la UI cae a `[]` para no romper y muestra estado vacío.

RouteGuard/Middleware
- Si el usuario no está autenticado y la ruta requiere auth, redirige a `/inscripcion?next=<ruta>`.
- Si un rol no cumple (ej. estudiante intentando `/admin`), redirige a su dashboard por defecto.

Logout (`AuthContext.logout`)
- Llama `/api/auth/logout` (no bloqueante), limpia sesión local y redirige a `/`.


## 7) Interacciones entre componentes y servicios
- `AuthContext` ←→ `lib/authClient`:
  - Persistencia (cookie/localStorage), revalidación periódica y helpers de ruta por rol.
- `pages/inscripcion.tsx` ←→ `services/auth.ts`/`lib/api.ts`:
  - Validación de formularios, envío de credenciales y manejo de errores.
- `services/*` ←→ `lib/adapters.ts`:
  - Normalización de DTOs del backend hacia `*UI` firmes para la UI.
- `RouteGuard`/`middleware.ts` ←→ `APP_CONFIG.ROUTES`:
  - Control de acceso y redirecciones por `roleLevel`.
- `errorHandler`/`api-errors`:
  - Mensajes consistentes de error (toast/alerta) y telemetría básica.


## 8) Ejemplos de payloads
- Login request
```json
{ "email": "user@dominio.com", "password": "Clave.123" }
```
- Login response (simplificado)
```json
{ "token": "<jwt>", "expiresAt": "2025-01-01T00:00:00Z", "user": { "id": "...", "email": "user@dominio.com", "fullName": "Nombre Apellido", "isUmg": true, "roleLevel": 0, "roles": ["Student"] } }
```
- Registro request
```json
{ "email": "nuevo@umg.edu.gt", "fullName": "Nombre Apellido", "password": "Clave.123!", "institution": "UMG" }
```
- Inscripción masiva request
```json
{ "activityIds": ["<uuid-1>", "<uuid-2>"] }
```


## 9) Comprobación manual rápida
1. Ingresar a `/inscripcion`.
2. Probar registro con contraseña fuerte (8+ y compleja). Ver mensaje y redirección.
3. Cerrar sesión y probar login con credenciales incorrectas → ver error.
4. Con usuario válido, seleccionar actividades sin conflicto y confirmar.
5. Abrir `/portal/inscripciones` → ver tarjetas, descargar/mostrar QR.


## 10) Observaciones y notas
- Si aparece “A database error occurred” en registro, proviene de la respuesta backend: se muestra como error general arriba del formulario. Revisar logs del servidor y la validez de payload.
- Unificar validación de password de login a la misma política del registro es recomendable.
- Preferir `apiClient` con rutas relativas `/api/*` y mantener `lib/adapters.ts` como única capa de adaptación DTO→UI.