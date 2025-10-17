using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Models.Email;
using Microsoft.EntityFrameworkCore;

namespace Congreso.Api.Services
{
    public class EnrollmentService
    {
        private readonly CongresoDbContext _context;
        private readonly ICheckInTokenService _checkInTokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(
            CongresoDbContext context, 
            ICheckInTokenService checkInTokenService,
            IEmailService emailService,
            ILogger<EnrollmentService> logger)
        {
            _context = context;
            _checkInTokenService = checkInTokenService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<EnrollmentResult>> EnrollManyAsync(Guid userId, List<Guid> activityIds)
        {
            if (!activityIds.Any())
                throw new ArgumentException("La lista de actividades no pueden estar vacía.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var results = new List<EnrollmentResult>();
                
                // Obtener actividades seleccionadas
                var selectedActivities = await _context.Activities
                    .Where(a => activityIds.Contains(a.Id))
                    .ToListAsync();

                if (selectedActivities.Count != activityIds.Count)
                    throw new ArgumentException("Una o más actividades no existen.");

                // Bloquear filas de actividades para prevenir carreras de capacidad
                // (SELECT ... FOR UPDATE por cada actividad)
                foreach (var actId in activityIds)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "SELECT id FROM activities WHERE id = {0} FOR UPDATE",
                        actId);
                }

                // Obtener todas las inscripciones existentes del usuario con sus actividades
                var userExistingEnrollments = await _context.Enrollments
                    .Include(e => e.Activity)
                    .Where(e => e.UserId == userId)
                    .ToListAsync();

                // Verificar inscripciones duplicadas
                var duplicateEnrollments = userExistingEnrollments
                    .Where(e => activityIds.Contains(e.ActivityId))
                    .ToList();

                if (duplicateEnrollments.Any())
                    throw new InvalidOperationException("El usuario ya está inscrito en una o más actividades.");

                // Verificar conflictos horarios entre actividades nuevas
                await ValidateTimeConflictsAsync(selectedActivities);

                // Verificar conflictos horarios con inscripciones existentes
                await ValidateTimeConflictsWithExistingEnrollmentsAsync(selectedActivities, userExistingEnrollments);

                // Validar capacidad antes de crear inscripciones
                await ValidateCapacityAsync(selectedActivities);

                // Crear inscripciones
                foreach (var activity in selectedActivities)
                {
                    // Calcular número de asiento con verificación de capacidad en tiempo real
                    var enrollmentCount = await _context.Enrollments
                        .CountAsync(e => e.ActivityId == activity.Id);
                    
                    var seatNumber = enrollmentCount + 1;

                    // Verificación final de cupo (por si cambió durante la transacción)
                    if (activity.Capacity.HasValue && seatNumber > activity.Capacity.Value)
                        throw new InvalidOperationException($"CUPO_LLENO: Ya no hay cupos disponibles para esta actividad.");

                    var qrCodeId = Guid.NewGuid().ToString();
                    var enrollment = new Enrollment
                    {
                        UserId = userId,
                        ActivityId = activity.Id,
                        SeatNumber = seatNumber,
                        QrCodeId = qrCodeId,
                        Attended = false
                    };

                    _context.Enrollments.Add(enrollment);
                    
                    results.Add(new EnrollmentResult
                    {
                        ActivityId = activity.Id,
                        SeatNumber = seatNumber,
                        QrCodeId = qrCodeId
                    });
                }

                await _context.SaveChangesAsync();

                // Generar tokens de check-in para cada inscripción
                var enrollments = await _context.Enrollments
                    .Where(e => e.UserId == userId && activityIds.Contains(e.ActivityId))
                    .ToListAsync();

                foreach (var enrollment in enrollments)
                {
                    var token = await _checkInTokenService.GenerateTokenAsync(
                        enrollment.UserId, 
                        enrollment.ActivityId, 
                        enrollment.Id);
                    
                    // Actualizar el resultado con el token generado
                    var result = results.FirstOrDefault(r => r.ActivityId == enrollment.ActivityId);
                    if (result != null)
                    {
                        result.CheckInToken = token;
                    }
                }

                await transaction.CommitAsync();
                
                // Enviar emails de confirmación después de la inscripción exitosa
                await SendConfirmationEmailsAsync(userId, results);
                
                return results;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private Task ValidateTimeConflictsAsync(List<Activity> activities)
        {
            for (int i = 0; i < activities.Count; i++)
            {
                for (int j = i + 1; j < activities.Count; j++)
                {
                    var activityA = activities[i];
                    var activityB = activities[j];

                    // Regla de conflicto: A.Start < B.End && B.Start < A.End
                    if (activityA.StartTime < activityB.EndTime && activityB.StartTime < activityA.EndTime)
                    {
                        throw new InvalidOperationException($"CONFLICTO_HORARIO: Las actividades seleccionadas se traslapan en fecha/hora; elige una sola.");
                    }
                }
            }
            return Task.CompletedTask;
        }

        private Task ValidateTimeConflictsWithExistingEnrollmentsAsync(List<Activity> newActivities, List<Enrollment> existingEnrollments)
        {
            foreach (var newActivity in newActivities)
            {
                foreach (var existingEnrollment in existingEnrollments)
                {
                    var existingActivity = existingEnrollment.Activity;
                    
                    // Verificar si hay solapamiento de horarios
                    if (newActivity.StartTime < existingActivity.EndTime && existingActivity.StartTime < newActivity.EndTime)
                    {
                        throw new InvalidOperationException($"CONFLICTO_HORARIO: Las actividades seleccionadas se traslapan en fecha/hora; elige una sola.");
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Valida que todas las actividades tengan cupo disponible
        /// </summary>
        private async Task ValidateCapacityAsync(List<Activity> activities)
        {
            foreach (var activity in activities)
            {
                if (!activity.Capacity.HasValue)
                    continue; // Sin límite de capacidad

                var currentEnrollmentCount = await _context.Enrollments
                    .CountAsync(e => e.ActivityId == activity.Id);

                if (currentEnrollmentCount >= activity.Capacity.Value)
                {
                    throw new InvalidOperationException($"CUPO_AGOTADO: Ya no hay cupos disponibles para esta actividad.");
                }
            }
        }

        /// <summary>
        /// Valida si una actividad específica tiene conflictos horarios con las inscripciones existentes del usuario
        /// </summary>
        public async Task<TimeConflictValidationResult> ValidateTimeConflictAsync(Guid userId, Guid activityId)
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
                return new TimeConflictValidationResult { HasConflict = false, Message = "Actividad no encontrada." };

            var userEnrollments = await _context.Enrollments
                .Include(e => e.Activity)
                .Where(e => e.UserId == userId)
                .ToListAsync();

            foreach (var enrollment in userEnrollments)
            {
                var existingActivity = enrollment.Activity;
                
                // Verificar solapamiento
                if (activity.StartTime < existingActivity.EndTime && existingActivity.StartTime < activity.EndTime)
                {
                    return new TimeConflictValidationResult 
                    { 
                        HasConflict = true, 
                        ConflictingActivity = existingActivity,
                        Message = $"Conflicto de horario con '{existingActivity.Title}'"
                    };
                }
            }

            return new TimeConflictValidationResult { HasConflict = false };
        }

        /// <summary>
        /// Valida conflictos de horario entre múltiples actividades
        /// </summary>
        public async Task<TimeConflictResult> ValidateTimeConflictsAsync(List<Guid> activityIds)
        {
            var result = new TimeConflictResult();

            if (activityIds == null || !activityIds.Any())
            {
                return result;
            }

            var activities = await _context.Activities
                .Where(a => activityIds.Contains(a.Id))
                .ToListAsync();

            if (activities.Count != activityIds.Count)
            {
                result.HasConflicts = true;
                result.Conflicts.Add("Una o más actividades no fueron encontradas");
                return result;
            }

            // Verificar conflictos entre las actividades seleccionadas
            for (int i = 0; i < activities.Count; i++)
            {
                for (int j = i + 1; j < activities.Count; j++)
                {
                    var activityA = activities[i];
                    var activityB = activities[j];

                    // Regla de conflicto: A.Start < B.End && B.Start < A.End
                    if (activityA.StartTime < activityB.EndTime && activityB.StartTime < activityA.EndTime)
                    {
                        result.HasConflicts = true;
                        result.Conflicts.Add($"'{activityA.Title}' ({activityA.StartTime:HH:mm}-{activityA.EndTime:HH:mm}) se solapa con '{activityB.Title}' ({activityB.StartTime:HH:mm}-{activityB.EndTime:HH:mm})");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Valida conflictos de horario entre actividades seleccionadas y las ya inscritas del usuario
        /// Utiliza tstzrange de PostgreSQL para detección eficiente de solapamientos
        /// </summary>
        public async Task<TimeConflictValidationResult> ValidateTimeConflictWithUserEnrollmentsAsync(Guid userId, List<Guid> activityIds)
        {
            var result = new TimeConflictValidationResult();

            if (activityIds == null || !activityIds.Any())
            {
                return result;
            }

            // Usar SQL crudo con tstzrange para detección eficiente de conflictos
            var sql = @"
                WITH selected_activities AS (
                    SELECT id, title, start_time, end_time, 
                           tstzrange(start_time, end_time, '[)') as time_range
                    FROM activities 
                    WHERE id = ANY(@activityIds)
                ),
                user_enrolled_activities AS (
                    SELECT a.id, a.title, a.start_time, a.end_time,
                           tstzrange(a.start_time, a.end_time, '[)') as time_range
                    FROM activities a
                    INNER JOIN enrollments e ON a.id = e.activity_id
                    WHERE e.user_id = @userId
                ),
                conflicts AS (
                    -- Conflictos entre actividades seleccionadas
                    SELECT s1.id as requested_id, s2.id as with_activity_id,
                           s1.title as requested_title, s2.title as with_title,
                           'selected' as conflict_type
                    FROM selected_activities s1
                    CROSS JOIN selected_activities s2
                    WHERE s1.id < s2.id 
                    AND s1.time_range && s2.time_range
                    
                    UNION ALL
                    
                    -- Conflictos entre actividades seleccionadas y ya inscritas
                    SELECT s.id as requested_id, u.id as with_activity_id,
                           s.title as requested_title, u.title as with_title,
                           'enrolled' as conflict_type
                    FROM selected_activities s
                    CROSS JOIN user_enrolled_activities u
                    WHERE s.time_range && u.time_range
                )
                SELECT requested_id, with_activity_id, requested_title, with_title, conflict_type
                FROM conflicts
                ORDER BY requested_id, with_activity_id";

            var conflicts = await _context.Database.SqlQueryRaw<ConflictQueryResult>(sql, 
                new Npgsql.NpgsqlParameter("@activityIds", activityIds.ToArray()),
                new Npgsql.NpgsqlParameter("@userId", userId))
                .ToListAsync();

            if (conflicts.Any())
            {
                result.HasConflicts = true;
                result.Conflicts = conflicts.Select(c => new ConflictDetail
                {
                    RequestedId = c.RequestedId,
                    WithActivityId = c.WithActivityId
                }).ToList();
            }

            return result;
        }

        /// <summary>
        /// Verifica el estado de capacidad de múltiples actividades
        /// </summary>
        public async Task<CapacityStatusResult> CheckCapacityStatusAsync(List<Guid> activityIds)
        {
            var result = new CapacityStatusResult();

            if (activityIds == null || !activityIds.Any())
                return result;

            var activities = await _context.Activities
                .Where(a => activityIds.Contains(a.Id))
                .ToListAsync();

            foreach (var activity in activities)
            {
                var enrollmentCount = await _context.Enrollments
                    .CountAsync(e => e.ActivityId == activity.Id);

                var status = new ActivityCapacityStatus
                {
                    ActivityId = activity.Id,
                    ActivityTitle = activity.Title,
                    CurrentEnrollments = enrollmentCount,
                    MaxCapacity = activity.Capacity,
                    HasCapacityLimit = activity.Capacity.HasValue
                };

                if (activity.Capacity.HasValue)
                {
                    status.AvailableSpots = Math.Max(0, activity.Capacity.Value - enrollmentCount);
                    status.IsFull = enrollmentCount >= activity.Capacity.Value;
                    status.CapacityPercentage = Math.Round((double)enrollmentCount / activity.Capacity.Value * 100, 1);
                    
                    if (status.IsFull)
                        status.Status = "LLENO";
                    else if (status.CapacityPercentage >= 90)
                        status.Status = "CASI_LLENO";
                    else if (status.CapacityPercentage >= 75)
                        status.Status = "POCOS_CUPOS";
                    else
                        status.Status = "DISPONIBLE";
                }
                else
                {
                    status.Status = "SIN_LIMITE";
                    status.AvailableSpots = null;
                    status.CapacityPercentage = 0;
                }

                result.Activities.Add(status);
            }

            return result;
        }

        public async Task<bool> DeleteEnrollmentAsync(Guid enrollmentId, Guid userId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.UserId == userId);

            if (enrollment == null)
                return false;

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Envía emails de confirmación para las inscripciones realizadas
        /// </summary>
        private async Task SendConfirmationEmailsAsync(Guid userId, List<EnrollmentResult> enrollmentResults)
        {
            try
            {
                // Obtener información del usuario
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("Usuario {UserId} no encontrado para envío de emails de confirmación", userId);
                    return;
                }

                // Obtener actividades de las inscripciones
                var activityIds = enrollmentResults.Select(r => r.ActivityId).ToList();
                var activities = await _context.Activities
                    .Where(a => activityIds.Contains(a.Id))
                    .ToListAsync();

                // Obtener enrollments con IDs reales
                var enrollments = await _context.Enrollments
                    .Where(e => e.UserId == userId && activityIds.Contains(e.ActivityId))
                    .ToListAsync();

                // Decidir si enviar email múltiple o individual
                if (enrollmentResults.Count > 1)
                {
                    // Enviar email de confirmación múltiple
                    await SendMultipleActivitiesConfirmationEmailAsync(user, activities, enrollments, enrollmentResults);
                }
                else
                {
                    // Enviar email individual (comportamiento original)
                    await SendSingleActivityConfirmationEmailAsync(user, activities, enrollments, enrollmentResults);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al procesar envío de emails de confirmación para usuario {UserId}", userId);
            }
        }

        /// <summary>
        /// Envía email de confirmación para múltiples actividades
        /// </summary>
        private Task SendMultipleActivitiesConfirmationEmailAsync(
            User user, 
            List<Activity> activities, 
            List<Enrollment> enrollments, 
            List<EnrollmentResult> enrollmentResults)
        {
            var activitiesData = new List<ActivityConfirmationDetail>();

            foreach (var result in enrollmentResults)
            {
                var activity = activities.FirstOrDefault(a => a.Id == result.ActivityId);
                var enrollment = enrollments.FirstOrDefault(e => e.ActivityId == result.ActivityId);
                
                if (activity == null || enrollment == null) continue;

                var durationMinutes = activity.EndTime.HasValue && activity.StartTime.HasValue 
                    ? (int)(activity.EndTime.Value - activity.StartTime.Value).TotalMinutes 
                    : 60;

                activitiesData.Add(new ActivityConfirmationDetail
                {
                    ActivityId = (int)activity.Id.GetHashCode(),
                    ActivityName = activity.Title,
                    ActivityDescription = activity.Description ?? "",
                    ActivityDateTime = activity.StartTime ?? DateTime.UtcNow,
                    ActivityLocation = activity.Location ?? "Por definir",
                    QrToken = result.CheckInToken ?? result.QrCodeId,
                    QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(result.CheckInToken ?? result.QrCodeId)}",
                    EnrollmentId = (int)enrollment.Id.GetHashCode(),
                    ActivityType = activity.GetActivityTypeDescription(),
                    Duration = $"{durationMinutes} minutos",
                    Speaker = "Por confirmar", // Activity model doesn't have Speaker property
                    SpecialNotes = $"Número de asiento: {result.SeatNumber}"
                });
            }

            var multipleActivitiesData = new MultipleActivitiesConfirmationEmail
            {
                StudentName = user.FullName ?? "Estudiante",
                StudentEmail = user.Email ?? "sin-correo@congreso.umg.edu.gt",
                Activities = activitiesData,
                ConfirmationDate = DateTime.UtcNow
            };

            // Enviar email de forma asíncrona sin bloquear el proceso principal
            _ = Task.Run(async () =>
            {
                try
                {
                    var emailSent = await _emailService.SendMultipleActivitiesConfirmationAsync(multipleActivitiesData);
                    if (emailSent)
                    {
                        _logger.LogInformation("Email de confirmación múltiple enviado exitosamente a {Email} para {Count} actividades", 
                            user.Email, activitiesData.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Falló el envío de email de confirmación múltiple a {Email} para {Count} actividades", 
                            user.Email, activitiesData.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email de confirmación múltiple a {Email} para {Count} actividades", 
                        user.Email, activitiesData.Count);
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Envía email de confirmación para una sola actividad (comportamiento original)
        /// </summary>
        private Task SendSingleActivityConfirmationEmailAsync(
            User user, 
            List<Activity> activities, 
            List<Enrollment> enrollments, 
            List<EnrollmentResult> enrollmentResults)
        {
            foreach (var result in enrollmentResults)
            {
                var activity = activities.FirstOrDefault(a => a.Id == result.ActivityId);
                var enrollment = enrollments.FirstOrDefault(e => e.ActivityId == result.ActivityId);
                
                if (activity == null || enrollment == null) continue;

                var confirmationData = new EnrollmentConfirmationEmail
                {
                    StudentName = user.FullName ?? "Estudiante",
                    StudentEmail = user.Email ?? "sin-correo@congreso.umg.edu.gt",
                    ActivityName = activity.Title,
                    ActivityDescription = activity.Description ?? "",
                    ActivityDateTime = activity.StartTime ?? DateTime.UtcNow,
                    ActivityLocation = activity.Location ?? "Por definir",
                    QrToken = result.CheckInToken ?? result.QrCodeId,
                    QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(result.CheckInToken ?? result.QrCodeId)}",
                    EnrollmentId = (int)enrollment.Id.GetHashCode(),
                    AdditionalInfo = $"Número de asiento: {result.SeatNumber}"
                };

                // Enviar email de forma asíncrona sin bloquear el proceso principal
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var emailSent = await _emailService.SendEnrollmentConfirmationAsync(confirmationData);
                        if (emailSent)
                        {
                            _logger.LogInformation("Email de confirmación enviado exitosamente a {Email} para actividad {Activity}", 
                                user.Email, activity.Title);
                        }
                        else
                        {
                            _logger.LogWarning("Falló el envío de email de confirmación a {Email} para actividad {Activity}", 
                                user.Email, activity.Title);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar email de confirmación a {Email} para actividad {Activity}", 
                            user.Email, activity.Title);
                    }
                });
            }
            return Task.CompletedTask;
        }
    }

    public class EnrollmentResult
    {
        public Guid ActivityId { get; set; }
        public int SeatNumber { get; set; }
        public string QrCodeId { get; set; } = string.Empty;
        public string? CheckInToken { get; set; }
    }

    public class TimeConflictValidationResult
    {
        public bool HasConflict { get; set; }
        public bool HasConflicts { get; set; }
        public string Message { get; set; } = string.Empty;
        public Activity? ConflictingActivity { get; set; }
        public List<ConflictDetail> Conflicts { get; set; } = new List<ConflictDetail>();
    }

    public class TimeConflictResult
    {
        public bool HasConflicts { get; set; }
        public List<string> Conflicts { get; set; } = new List<string>();
    }

    public class CapacityStatusResult
    {
        public List<ActivityCapacityStatus> Activities { get; set; } = new List<ActivityCapacityStatus>();
    }

    public class ActivityCapacityStatus
    {
        public Guid ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public int CurrentEnrollments { get; set; }
        public int? MaxCapacity { get; set; }
        public int? AvailableSpots { get; set; }
        public bool HasCapacityLimit { get; set; }
        public bool IsFull { get; set; }
        public double CapacityPercentage { get; set; }
        public string Status { get; set; } = string.Empty; // LLENO, CASI_LLENO, POCOS_CUPOS, DISPONIBLE, SIN_LIMITE
    }

    public class ConflictDetail
    {
        public Guid RequestedId { get; set; }
        public Guid WithActivityId { get; set; }
    }

    public class ConflictQueryResult
    {
        public Guid RequestedId { get; set; }
        public Guid WithActivityId { get; set; }
        public string RequestedTitle { get; set; } = string.Empty;
        public string WithTitle { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty;
    }
}