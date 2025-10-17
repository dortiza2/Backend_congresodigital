# 📋 Documentación Exhaustiva de Controladores y Rutas API

## 🎯 Resumen Ejecutivo

### Estado General de la API
- **Total de Controladores**: 21 controladores identificados
- **Servidor Activo**: `http://localhost:5213` con Neon DB configurado
- **Swagger UI**: Disponible en `/swagger/index.html`
- **Estado de Salud**: Health checks operativos

### Categorización de Controladores por Módulos

| Módulo | Controladores | Estado Swagger |
|--------|---------------|----------------|
| **Diagnóstico** | DiagnosticsController | ✅ Documentado |
| **Público** | PublicController, PublicActivitiesController, PublicPodiumsController | ⚠️ Parcial |
| **Autenticación** | AuthController | ✅ Documentado |
| **Usuarios** | UsersController | ⚠️ Parcial |
| **Perfiles** | ProfilesController | ✅ Documentado |
| **Actividades** | AdminActivitiesController, ActivitiesController (legacy) | ⚠️ Parcial |
| **Inscripciones** | EnrollmentsController | ⚠️ Parcial |
| **Asistencias** | AttendancesController | ⚠️ Parcial |
| **Certificados** | CertificatesController | ✅ Documentado |
| **Staff** | StaffController | ⚠️ Parcial |
| **Estudiantes** | StudentController | ⚠️ Parcial |
| **Administración** | Admin/ActivitiesController, Admin/SpeakersController, Admin/FaqItemsController, Admin/OrganizationsController, Admin/WinnersController, AdminPodiumsController, AdminOutboxController | ⚠️ Parcial |
| **Utilidades** | EmailTestController | ⚠️ Parcial |

---

## 🔍 Documentación Detallada por Controlador

### 1. DiagnosticsController
**Ruta Base**: `/api/diagnostics`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/healthz` | GET | Health check básico | Ninguno | `{status, timestamp, checks}` | AllowAnonymous | ✅ Sí |
| `/ready` | GET | Readiness check | Ninguno | `{status, timestamp, checks}` | AllowAnonymous | ✅ Sí |
| `/metrics` | GET | Métricas Prometheus | Ninguno | Texto plano formato Prometheus | AllowAnonymous | ✅ Sí |

**DTOs de Respuesta**:
```csharp
{
  "status": "Healthy|Degraded|Unhealthy",
  "timestamp": "2024-01-01T12:00:00.000Z",
  "checks": {
    "certificates": { "status": "healthy", "description": "..." },
    "cache": { "status": "healthy", "description": "..." },
    "queues": { "status": "healthy", "description": "..." }
  }
}
```

---

### 2. PublicController
**Ruta Base**: `/api`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/activities-view` | GET | Lista actividades públicas | `?from=date&to=date&type=string` | `Activity[]` | AllowAnonymous | ⚠️ No |
| `/faq` | GET | Preguntas frecuentes | Ninguno | `FaqItem[]` | AllowAnonymous | ⚠️ No |
| `/podium` | GET | Podio por año | `?year=int` | `PodiumDto[]` | AllowAnonymous | ⚠️ No |
| `/speakers` | GET | Lista de ponentes | Ninguno | `Speaker[]` | AllowAnonymous | ⚠️ No |

**DTOs de Respuesta**:
```csharp
// PodiumDto
{
  "id": 0,
  "year": 2024,
  "place": 1,
  "activityId": "guid",
  "activityTitle": "string",
  "userId": 0,
  "winnerName": "string",
  "awardDate": "2024-01-01T12:00:00Z",
  "teamId": 0,
  "prizeDescription": "string"
}
```

---

### 3. PublicActivitiesController
**Ruta Base**: `/api/public/activities`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/` | GET | Lista actividades | `?kinds=string` | `ActivityDto[]` | AllowAnonymous | ⚠️ No |
| `/{id}` | GET | Actividad por ID | `id: string` | `ActivityDto` | AllowAnonymous | ⚠️ No |
| `/upcoming` | GET | Próximas actividades | Ninguno | `ActivityDto[]` | AllowAnonymous | ⚠️ No |

**Rutas Alias Adicionales**:
- `/api/activities`
- `/api/activities/public`
- `/api/activities/public/{id}`
- `/api/activities/upcoming`

**DTOs de Respuesta**:
```csharp
// ActivityDto
{
  "id": "string",
  "title": "string",
  "activityType": "talk|workshop|competition|activity",
  "location": "string|null",
  "startTime": "2024-01-01T12:00:00Z",
  "endTime": "2024-01-01T14:00:00Z",
  "capacity": 100,
  "published": true,
  "enrolledCount": 0,
  "availableSpots": 100,
  "speaker": {
    "id": "string",
    "name": "string",
    "bio": "string|null",
    "company": "string|null",
    "roleTitle": "string|null",
    "avatarUrl": "string|null",
    "links": "string|null"
  }
}
```

---

### 4. AuthController
**Ruta Base**: `/api/auth`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/login` | POST | Login tradicional | `{email: string, password: string}` | `{token, expiresAtUtc, user}` | AllowAnonymous | ✅ Sí |
| `/me` | GET | Información del usuario | Ninguno | `{userId, email, roles, roleLevel}` | Authorize | ✅ Sí |
| `/session` | GET | Estado de sesión | Ninguno | `{isAuthenticated, user, roleLevel}` | Authorize | ✅ Sí |
| `/google` | POST | Login con Google | `{email, name, picture}` | `{token, expiresAtUtc, user}` | AllowAnonymous | ✅ Sí |

**DTOs de Entrada/Salida**:
```csharp
// LoginDto (entrada)
{
  "email": "usuario@ejemplo.com",
  "password": "contraseña"
}

// Respuesta Login
{
  "message": "Login exitoso",
  "token": "eyJ...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2024-01-01T12:00:00Z",
  "user": {
    "id": "guid",
    "email": "usuario@ejemplo.com",
    "fullName": "Nombre Completo",
    "roles": ["student", "staff"],
    "roleLevel": 1
  }
}
```

**Validaciones de Seguridad**:
- Dominios permitidos para Google: `umg.edu.gt` (configurable via `GOOGLE_ALLOWED_DOMAINS`)
- Rate limiting implícito
- Manejo de errores controlado (401 para credenciales inválidas)

---

### 5. UsersController
**Ruta Base**: `/api/users`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/` | GET | Lista usuarios paginada | `?page=1&pageSize=50` | `ApiResponse<PagedResponseDto<UserDto>>` | RequireStaffOrHigher | ⚠️ No |
| `/{id:guid}` | GET | Usuario por ID | `id: Guid` | `ApiResponse<UserDto>` | Authorize | ⚠️ No |
| `/search` | GET | Buscar usuarios | `?q=string` | `ApiResponse<List<UserDto>>` | RequireStaffOrHigher | ⚠️ No |
| `/role/{role}` | GET | Usuarios por rol | `role: string` | `UserDto[]` | AdminOnly | ⚠️ No |
| `/` | POST | Crear usuario | `CreateUserDto` | `ApiResponse<UserDto>` | RequireAdmin | ⚠️ No |

**DTOs de Entrada/Salida**:
```csharp
// UserDto
{
  "id": "guid",
  "email": "usuario@ejemplo.com",
  "fullName": "Nombre Completo",
  "phoneNumber": "string|null",
  "createdAt": "2024-01-01T12:00:00Z",
  "roles": ["student"],
  "isActive": true
}

// CreateUserDto (entrada)
{
  "email": "nuevo@ejemplo.com",
  "password": "contraseña",
  "fullName": "Nombre Completo",
  "phoneNumber": "string|null",
  "roles": ["student"]
}
```

**Permisos y Validaciones**:
- Usuarios solo pueden ver su propio perfil (a menos que sean admin)
- Validación de email único
- Staff puede ver todos los usuarios
- Solo admin puede crear usuarios

---

### 6. ProfilesController
**Ruta Base**: `/api/profiles`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/staff` | GET | Lista perfiles staff | Ninguno | `StaffAccountDto[]` | AdminOnly | ✅ Sí |
| `/staff/{userId}` | GET | Perfil staff por ID | `userId: Guid` | `StaffAccountDto` | Authorize | ✅ Sí |
| `/staff` | POST | Crear perfil staff | `CreateStaffAccountDto` | `StaffAccountDto` | SuperAdminOnly | ✅ Sí |
| `/staff/{userId}` | PUT | Actualizar perfil staff | `userId: Guid, UpdateStaffAccountDto` | `StaffAccountDto` | Authorize | ✅ Sí |
| `/staff/{userId}` | DELETE | Eliminar perfil staff | `userId: Guid` | `204 No Content` | SuperAdminOnly | ✅ Sí |
| `/students` | GET | Lista perfiles estudiantes | Ninguno | `StudentAccountDto[]` | StaffOnly | ✅ Sí |
| `/students/{userId}` | GET | Perfil estudiante por ID | `userId: Guid` | `StudentAccountDto` | Authorize | ✅ Sí |
| `/students` | POST | Crear perfil estudiante | `CreateStudentAccountDto` | `StudentAccountDto` | StaffOnly | ✅ Sí |

**DTOs de Entrada/Salida**:
```csharp
// StaffAccountDto
{
  "userId": "guid",
  "email": "staff@ejemplo.com",
  "fullName": "Nombre Completo",
  "department": "string",
  "position": "string",
  "employeeCode": "string",
  "isActive": true,
  "permissions": ["manage_activities", "view_reports"],
  "createdAt": "2024-01-01T12:00:00Z"
}

// StudentAccountDto
{
  "userId": "guid",
  "email": "student@ejemplo.com",
  "fullName": "Nombre Completo",
  "studentCode": "string",
  "career": "string",
  "semester": 5,
  "phoneNumber": "string|null",
  "isActive": true,
  "createdAt": "2024-01-01T12:00:00Z"
}
```

**Políticas de Autorización**:
- `AdminOnly`: Solo administradores
- `SuperAdminOnly`: Solo super administradores  
- `StaffOnly`: Personal autorizado
- Control de acceso propio: Usuarios pueden ver/editar sus propios perfiles

---

### 7. EnrollmentsController
**Ruta Base**: `/api/enrollments`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/` | POST | Crear inscripción | `CreateEnrollmentEnvelope` | `{data: {id: guid}}` | Authorize | ⚠️ No |
| `/validate-conflict` | POST | Validar conflicto horario | `ValidateConflictRequest` | `{hasConflict, message, conflictingActivity}` | Authorize | ⚠️ No |
| `/validate-time-conflicts` | POST | Validar múltiples conflictos | `ValidateTimeConflictsRequest` | `{hasConflicts, conflicts, message}` | AllowAnonymous | ⚠️ No |
| `/validate-time-conflict` | POST | Validar conflicto con usuario | `ValidateTimeConflictRequest` | `{ok: boolean}` | Authorize | ⚠️ No |
| `/check-capacity` | POST | Verificar capacidad | `CheckCapacityRequest` | `CapacityStatusResult[]` | AllowAnonymous | ⚠️ No |

**DTOs de Entrada/Salida**:
```csharp
// CreateEnrollmentEnvelope
{
  "request": {
    "userId": "guid",
    "activityIds": ["guid1", "guid2"]
  }
}

// ValidateConflictRequest
{
  "userId": "guid",
  "activityId": "guid"
}

// ValidateTimeConflictsRequest
{
  "activityIds": ["guid1", "guid2", "guid3"]
}

// Respuesta de Conflicto
{
  "hasConflict": true,
  "message": "Conflicto de horario detectado",
  "conflictingActivity": {
    "id": "guid",
    "title": "Actividad en conflicto",
    "startTime": "2024-01-01T12:00:00Z",
    "endTime": "2024-01-01T14:00:00Z"
  }
}
```

**Códigos de Estado Específicos**:
- `422`: Conflicto de horario (`CONFLICTO_HORARIO`)
- `409`: Cupo agotado (`CUPO_AGOTADO/CUPO_LLENO`)
- `409`: Ya inscrito (duplicado)
- `500`: Error interno del servidor

---

### 8. CertificatesController
**Ruta Base**: `/api/certificates`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/generate` | POST | Generar certificado | `GenerateCertificateRequest` | `CertificateResponse` | Authorize | ✅ Sí |
| `/user/{userId}` | GET | Certificados de usuario | `userId: int, ?page=1&pageSize=10` | `PaginatedResult<CertificateResponse>` | Authorize | ✅ Sí |
| `/validate/{hash}` | GET | Validar certificado | `hash: string` | `CertificateValidationResult` | AllowAnonymous | ✅ Sí |

**DTOs de Entrada/Salida**:
```csharp
// GenerateCertificateRequest
{
  "userId": 123,
  "type": "participation|attendance|winner|merit",
  "activityId": "guid|null",
  "metadata": {
    "activityTitle": "Título de la actividad",
    "hours": 2,
    "issueDate": "2024-01-01T12:00:00Z"
  }
}

// CertificateResponse
{
  "id": 1,
  "userId": 123,
  "type": "participation",
  "hash": "abc123...",
  "issuedAt": "2024-01-01T12:00:00Z",
  "validUntil": "2025-01-01T12:00:00Z",
  "metadata": { ... },
  "downloadUrl": "https://...",
  "qrCode": "data:image/png;base64,..."
}

// CertificateValidationResult
{
  "isValid": true,
  "message": "Certificado válido",
  "certificateId": 1,
  "certificate": { ... },
  "validatedAt": "2024-01-01T12:00:00Z"
}
```

**Permisos**:
- Usuarios pueden generar sus propios certificados
- Admin puede generar certificados para otros usuarios
- Validación pública (sin autenticación)

---

### 9. AdminActivitiesController
**Ruta Base**: `/api/admin/activities`

| Endpoint | Método | Descripción | Parámetros | Respuesta | Auth | Swagger |
|----------|---------|-------------|------------|-----------|------|-----------|
| `/` | GET | Lista actividades admin | Ninguno | `ActivityAdminDto[]` | Authorize | ⚠️ No |
| `/{id}` | GET | Actividad admin por ID | `id: Guid` | `ActivityAdminDto` | Authorize | ⚠️ No |
| `/` | POST | Crear actividad | `CreateActivityAdminDto` | `ActivityAdminDto` | Authorize | ⚠️ No |
| `/{id}` | PUT | Actualizar actividad | `id: Guid, UpdateActivityAdminDto` | `ActivityAdminDto` | Authorize | ⚠️ No |
| `/{id}` | DELETE | Eliminar actividad | `id: Guid` | `204 No Content` | Authorize | ⚠️ No |

**DTOs de Entrada/Salida**:
```csharp
// ActivityAdminDto
{
  "id": "guid",
  "title": "Título de la actividad",
  "description": "Descripción completa",
  "location": "Ubicación",
  "startTime": "2024-01-01T12:00:00Z",
  "endTime": "2024-01-01T14:00:00Z",
  "capacity": 100,
  "published": true,
  "activityType": "talk|workshop|competition",
  "speakers": [{ ... }],
  "enrolledCount": 45,
  "availableSpots": 55,
  "createdAt": "2024-01-01T12:00:00Z",
  "updatedAt": "2024-01-01T12:00:00Z"
}

// CreateActivityAdminDto / UpdateActivityAdminDto
{
  "title": "string",
  "description": "string",
  "location": "string|null",
  "startTime": "2024-01-01T12:00:00Z",
  "endTime": "2024-01-01T14:00:00Z",
  "capacity": 100,
  "published": true,
  "activityType": "talk|workshop|competition",
  "speakerIds": ["guid1", "guid2"]
}
```

**Permisos**: Requiere autenticación y permisos de administrador

---

### 10. Controladores Administrativos Adicionales

#### Admin/SpeakersController
**Ruta**: `/api/admin/speakers`
- CRUD completo de ponentes
- Requiere autenticación admin
- Estado Swagger: ⚠️ Parcial

#### Admin/FaqItemsController  
**Ruta**: `/api/admin/faq`
- Gestión de preguntas frecuentes
- CRUD completo
- Estado Swagger: ⚠️ Parcial

#### Admin/OrganizationsController
**Ruta**: `/api/admin/organizations`
- Gestión de organizaciones
- CRUD completo
- Estado Swagger: ⚠️ Parcial

#### Admin/WinnersController
**Ruta**: `/api/admin/winners`
- Gestión de ganadores
- CRUD completo
- Estado Swagger: ⚠️ Parcial

---

## 🚨 Análisis de Endpoints Críticos

### Endpoints de Autenticación (CRÍTICOS)
1. **`POST /api/auth/login`**: Login principal
   - Validación de credenciales BCrypt
   - Generación de JWT token
   - Manejo de errores controlado

2. **`POST /api/auth/google`**: Login con Google
   - Validación de dominio UMG
   - Creación automática de usuarios
   - Rate limiting implícito

### Endpoints Administrativos (ALTA SEGURIDAD)
1. **`/api/admin/**`: Todas las rutas admin
   - Requieren autenticación JWT válida
   - Verificación de permisos de administrador
   - Logging de auditoría

### Endpoints Públicos (SIN AUTENTICACIÓN)
1. **`GET /api/activities/**`: Actividades públicas
2. **`GET /api/faq`**: Preguntas frecuentes  
3. **`GET /api/podium`**: Podio de ganadores
4. **`GET /api/speakers`**: Lista de ponentes
5. **`GET /certificates/validate/{hash}`**: Validación de certificados

### Endpoints de Inscripciones (CRÍTICOS DE NEGOCIO)
1. **`POST /api/enrollments`**: Crear inscripciones
   - Validación de conflictos horarios
   - Verificación de cupo disponible
   - Prevención de duplicados

---

## 📊 Mapeo de DTOs y Modelos

### DTOs de Respuesta Estándar
```csharp
// ApiResponse<T>
{
  "success": true,
  "data": T,
  "message": "string",
  "errors": ["string"]
}

// PagedResponseDto<T>
{
  "items": T[],
  "totalCount": 100,
  "page": 1,
  "pageSize": 50,
  "totalPages": 2
}

// OperationResponseDto
{
  "success": true,
  "message": "string"
}
```

### Modelos de Entrada Comunes
```csharp
// CreateUserDto
{
  "email": "string",
  "password": "string", 
  "fullName": "string",
  "phoneNumber": "string|null",
  "roles": ["string"]
}

// LoginDto
{
  "email": "string",
  "password": "string"
}
```

---

## 📋 Estado de Swagger Documentation

### ✅ Controladores Completamente Documentados
1. **DiagnosticsController**: Todos los endpoints con XML comments
2. **AuthController**: Login, me, session, google documentados
3. **ProfilesController**: Staff y Students completos con SwaggerOperation
4. **CertificatesController**: Generate, GetUserCertificates, Validate completos

### ⚠️ Controladores con Documentación Parcial
1. **PublicController**: Sin atributos SwaggerOperation
2. **PublicActivitiesController**: Sin documentación Swagger
3. **UsersController**: Sin atributos SwaggerOperation
4. **EnrollmentsController**: Sin documentación Swagger
5. **AdminActivitiesController**: Sin atributos SwaggerOperation

### ❌ Controladores Sin Documentación Swagger
1. **StaffController**: Sin documentación
2. **StudentController**: Sin documentación  
3. **AttendancesController**: Sin documentación
4. **Admin/SpeakersController**: Sin documentación
5. **Admin/FaqItemsController**: Sin documentación
6. **Admin/OrganizationsController**: Sin documentación
7. **Admin/WinnersController**: Sin documentación
8. **AdminPodiumsController**: Sin documentación
9. **AdminOutboxController**: Sin documentación
10. **EmailTestController**: Sin documentación

---

## 🔒 Validaciones y Seguridad

### Políticas de Autorización Implementadas
```csharp
// Políticas definidas en Program.cs
- "AdminOnly": Requiere rol Admin
- "SuperAdminOnly": Requiere rol SuperAdmin  
- "StaffOnly": Requiere rol Staff o superior
- "RequireStaffOrHigher": Requiere Staff, Admin o SuperAdmin
- "RequireAdminOrHigher": Requiere Admin o SuperAdmin
```

### Validaciones de Entrada
1. **ModelState.IsValid**: Verificación en todos los endpoints
2. **Data Annotations**: Atributos de validación en DTOs
3. **Validaciones de Negocio**: 
   - Conflictos horarios en inscripciones
   - Cupo disponible
   - Emails únicos
   - Fechas futuras para actividades

### Manejo de Errores
1. **Códigos HTTP Estándar**:
   - `200 OK`: Éxito
   - `201 Created`: Recurso creado
   - `204 No Content`: Éxito sin contenido
   - `400 Bad Request`: Solicitud inválida
   - `401 Unauthorized`: No autenticado
   - `403 Forbidden`: Sin permisos
   - `404 Not Found`: Recurso no encontrado
   - `409 Conflict`: Conflicto (duplicado, cupo lleno)
   - `422 Unprocessable Entity`: Conflicto horario
   - `500 Internal Server Error`: Error del servidor

2. **Formato de Error Estándar**:
```csharp
{
  "message": "Descripción del error",
  "type": "tipo_de_error",
  "error_code": "CODIGO_ERROR"
}
```

---

## 🎯 Recomendaciones para Completar Documentación

### Prioridad Alta (Críticos para Frontend)
1. **PublicActivitiesController**: Documentar todos los endpoints públicos
2. **EnrollmentsController**: Documentar sistema de inscripciones
3. **UsersController**: Documentar gestión de usuarios
4. **AdminActivitiesController**: Documentar CRUD de actividades

### Prioridad Media (Administración)
1. **StaffController**: Documentar gestión de personal
2. **StudentController**: Documentar gestión de estudiantes
3. **AttendancesController**: Documentar control de asistencias
4. **Admin/SpeakersController**: Documentar gestión de ponentes

### Prioridad Baja (Utilidades)
1. **Admin/FaqItemsController**: Documentar gestión FAQ
2. **Admin/OrganizationsController**: Documentar organizaciones
3. **Admin/WinnersController**: Documentar ganadores
4. **EmailTestController**: Documentar pruebas de email

### Tareas de Documentación Pendientes
1. Agregar atributos `[SwaggerOperation]` a todos los endpoints
2. Documentar todos los DTOs con ejemplos
3. Agregar descripciones detalladas de parámetros
4. Documentar códigos de respuesta HTTP
5. Agregar ejemplos de request/response
6. Documentar validaciones y constraints

---

## 📈 Estado Final de la API

### ✅ Funcionalidad Verificada
- **Conexión Neon DB**: ✅ Operativa con SSL y pooling optimizado
- **Migraciones**: ✅ Aplicadas correctamente
- **Health Checks**: ✅ Funcionando en `/api/diagnostics/healthz`
- **Swagger UI**: ✅ Disponible y accesible
- **Autenticación JWT**: ✅ Implementada y funcional
- **Autorización por Roles**: ✅ Políticas implementadas

### 🔧 Configuración Actual
- **Base de Datos**: Neon PostgreSQL con SSL
- **Connection Pooling**: Máximo 20 conexiones (optimizado para 0.5GB)
- **Timeouts**: 30 segundos comando, 5 segundos conexión
- **SSL Mode**: Require (encriptación obligatoria)
- **Retry Policy**: 3 intentos con backoff exponencial

### 🚀 Próximos Pasos Recomendados
1. Completar documentación Swagger de endpoints pendientes
2. Implementar rate limiting en endpoints críticos
3. Agregar más validaciones de negocio
4. Optimizar consultas SQL para mejor performance
5. Implementar caché para endpoints de lectura frecuente
6. Agregar más pruebas unitarias y de integración

---

*Documento generado el: $(date)*
*Versión de API: 1.0.0*
*Estado: Produ