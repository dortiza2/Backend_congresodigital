-- D2P5: índice único compuesto para evitar doble inscripción
-- Idempotente: no falla si ya existe
CREATE UNIQUE INDEX IF NOT EXISTS uq_enrollments_user_activity 
ON enrollments(user_id, activity_id);