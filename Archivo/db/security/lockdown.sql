-- =====================================================
-- D1P3 Database Security Lockdown Script
-- Congreso Digital - PostgreSQL Security Hardening
-- =====================================================
-- 
-- OBJETIVO: Aplicar principio de menor privilegio y optimizar índices
-- USUARIO OBJETIVO: user_congreso (aplicación)
-- FECHA: 2025-10-02
-- 
-- IMPORTANTE: Este script es idempotente y puede ejecutarse múltiples veces
-- =====================================================

-- Configurar cliente para mostrar tiempo de ejecución
\timing on

-- Mostrar información de conexión actual
SELECT 
    current_database() as database_name,
    current_user as current_user,
    session_user as session_user,
    version() as postgresql_version;

-- =====================================================
-- FASE 1: REVOCACIÓN DE PRIVILEGIOS PÚBLICOS
-- =====================================================

-- Revocar todos los privilegios del esquema público para PUBLIC
REVOKE ALL ON SCHEMA public FROM PUBLIC;

-- Revocar todos los privilegios de tablas para PUBLIC
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM PUBLIC;

-- Revocar todos los privilegios de secuencias para PUBLIC
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM PUBLIC;

-- Revocar privilegios de funciones para PUBLIC
REVOKE ALL ON ALL FUNCTIONS IN SCHEMA public FROM PUBLIC;

-- =====================================================
-- FASE 2: CONFIGURACIÓN DE PRIVILEGIOS MÍNIMOS
-- =====================================================

-- Otorgar uso del esquema público al usuario de aplicación
GRANT USAGE ON SCHEMA public TO user_congreso;

-- Otorgar privilegios básicos de CRUD en todas las tablas
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO user_congreso;

-- Otorgar uso y selección en todas las secuencias (para SERIAL/IDENTITY)
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO user_congreso;

-- =====================================================
-- FASE 3: PRIVILEGIOS ESPECÍFICOS PARA VISTAS PÚBLICAS
-- =====================================================

-- Vistas de solo lectura para consultas públicas
GRANT SELECT ON vw_public_activities TO user_congreso;
GRANT SELECT ON vw_podium_by_year TO user_congreso;

-- Tabla FAQ (solo lectura para consultas públicas)
-- Nota: Ya incluida en el GRANT anterior, pero explícita para claridad
GRANT SELECT ON faq_items TO user_congreso;

-- =====================================================
-- FASE 4: CREACIÓN DE ÍNDICES FALTANTES
-- =====================================================

-- Índice para consultas de ganadores por actividad
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_winners_activity_id 
ON winners(activity_id);

-- Índice para consultas de tokens de check-in por usuario
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_check_in_tokens_user_id 
ON check_in_tokens(user_id);

-- Índice para consultas temporales de inscripciones
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_enrollments_created_at 
ON enrollments(created_at);

-- Índice para consultas de actividades por fecha de inicio
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_activities_start_time 
ON activities(start_time);

-- Índice para consultas de usuarios por fecha de último login
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_users_last_login 
ON users(last_login) WHERE last_login IS NOT NULL;

-- Índice para consultas de staff por rol
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_staff_accounts_staff_role 
ON staff_accounts(staff_role);

-- Índice compuesto para consultas de estudiantes por carrera y cohorte
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_student_accounts_career_cohort 
ON student_accounts(career, cohort_year);

-- Índice para consultas de invitaciones de staff pendientes
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_staff_invites_pending 
ON staff_invites(created_at) 
WHERE accepted_at IS NULL AND revoked_at IS NULL;

-- =====================================================
-- FASE 5: CONFIGURACIÓN DE PRIVILEGIOS POR DEFECTO
-- =====================================================

-- Configurar privilegios por defecto para objetos futuros
ALTER DEFAULT PRIVILEGES IN SCHEMA public 
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO user_congreso;

ALTER DEFAULT PRIVILEGES IN SCHEMA public 
GRANT USAGE, SELECT ON SEQUENCES TO user_congreso;

-- =====================================================
-- FASE 6: VERIFICACIÓN DE SEGURIDAD
-- =====================================================

-- Verificar que PUBLIC no tiene privilegios en el esquema
SELECT 
    'schema_privileges' as check_type,
    nspname as schema_name,
    nspacl as privileges
FROM pg_namespace 
WHERE nspname = 'public';

-- Verificar privilegios del usuario de aplicación en tablas críticas
SELECT 
    'table_privileges' as check_type,
    schemaname,
    tablename,
    privilege_type,
    grantee
FROM information_schema.role_table_grants 
WHERE grantee = 'user_congreso' 
    AND schemaname = 'public'
    AND tablename IN ('users', 'activities', 'enrollments', 'user_roles')
ORDER BY tablename, privilege_type;

-- Verificar índices creados
SELECT 
    'new_indexes' as check_type,
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE schemaname = 'public' 
    AND indexname LIKE 'ix_%_activity_id'
    OR indexname LIKE 'ix_%_user_id'
    OR indexname LIKE 'ix_%_created_at'
    OR indexname LIKE 'ix_%_start_time'
    OR indexname LIKE 'ix_%_last_login'
    OR indexname LIKE 'ix_%_staff_role'
    OR indexname LIKE 'ix_%_career_cohort'
    OR indexname LIKE 'ix_%_pending'
ORDER BY tablename, indexname;

-- =====================================================
-- FASE 7: LIMPIEZA Y OPTIMIZACIÓN
-- =====================================================

-- Actualizar estadísticas de las tablas después de crear índices
ANALYZE;

-- Mostrar resumen de objetos y privilegios
SELECT 
    'summary' as info_type,
    COUNT(CASE WHEN obj_type = 'table' THEN 1 END) as tables_count,
    COUNT(CASE WHEN obj_type = 'view' THEN 1 END) as views_count,
    COUNT(CASE WHEN obj_type = 'sequence' THEN 1 END) as sequences_count
FROM (
    SELECT 'table' as obj_type FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
    UNION ALL
    SELECT 'view' as obj_type FROM information_schema.views WHERE table_schema = 'public'
    UNION ALL
    SELECT 'sequence' as obj_type FROM information_schema.sequences WHERE sequence_schema = 'public'
) objects;

-- =====================================================
-- NOTAS IMPORTANTES
-- =====================================================

/*
PRIVILEGIOS APLICADOS:
- user_congreso: SELECT, INSERT, UPDATE, DELETE en todas las tablas
- user_congreso: USAGE, SELECT en todas las secuencias
- user_congreso: USAGE en esquema public
- PUBLIC: Sin privilegios (revocados)

ÍNDICES AÑADIDOS:
- ix_winners_activity_id: Optimiza consultas de ganadores por actividad
- ix_check_in_tokens_user_id: Optimiza consultas de asistencia por usuario
- ix_enrollments_created_at: Optimiza consultas temporales de inscripciones
- ix_activities_start_time: Optimiza consultas de actividades por fecha
- ix_users_last_login: Optimiza consultas de último login (solo no nulos)
- ix_staff_accounts_staff_role: Optimiza consultas por rol de staff
- ix_student_accounts_career_cohort: Optimiza consultas por carrera y cohorte
- ix_staff_invites_pending: Optimiza consultas de invitaciones pendientes

SEGURIDAD MEJORADA:
- Principio de menor privilegio aplicado
- Esquema público protegido contra acceso no autorizado
- Privilegios por defecto configurados para objetos futuros
- Índices optimizados para consultas críticas

PRÓXIMOS PASOS RECOMENDADOS:
1. Implementar Row Level Security (RLS) para datos sensibles
2. Crear roles específicos por funcionalidad
3. Configurar auditoría de acceso
4. Monitorear performance de nuevos índices
*/

-- Mostrar tiempo total de ejecución
SELECT 'Script completed successfully at: ' || NOW()::timestamp as completion_status;