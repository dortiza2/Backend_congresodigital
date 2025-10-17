using Congreso.Api.Services;
using Congreso.Api.Models.Enrollments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Congreso.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/enrollments")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly EnrollmentService _enrollmentService;

        public EnrollmentsController(EnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEnrollmentEnvelope body)
        {
            var req = body.Request
                ?? (body.UserId.HasValue && body.ActivityIds is { Count: > 0 }
                    ? new CreateEnrollmentRequest(body.UserId.Value, body.ActivityIds!)
                    : null);

            if (req is null)
                return BadRequest(new { message = "The request field is required." });

            try
            {
                var results = await _enrollmentService.EnrollManyAsync(req.UserId, req.ActivityIds);
                return Created(string.Empty, new { data = new { id = Guid.NewGuid() } });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Determinar el código de estado basado en el tipo de error
                if (ex.Message.StartsWith("CONFLICTO_HORARIO:"))
                {
                    return StatusCode(422, new { 
                        message = ex.Message.Replace("CONFLICTO_HORARIO: ", ""), 
                        type = "time_conflict",
                        error_code = "TIME_CONFLICT"
                    });
                }
                else if (ex.Message.StartsWith("CUPO_AGOTADO:") || ex.Message.StartsWith("CUPO_LLENO:"))
                {
                    return StatusCode(409, new { 
                        message = ex.Message.Replace("CUPO_AGOTADO: ", "").Replace("CUPO_LLENO: ", ""), 
                        type = "capacity_full",
                        error_code = "CAPACITY_EXCEEDED"
                    });
                }
                else if (ex.Message.Contains("ya está inscrito"))
                {
                    return Conflict(new { 
                        message = ex.Message, 
                        type = "already_enrolled",
                        error_code = "DUPLICATE_ENROLLMENT"
                    });
                }
                else
                {
                    return Conflict(new { 
                        message = ex.Message, 
                        type = "general_conflict",
                        error_code = "GENERAL_ERROR"
                    });
                }
            }
            catch (DbUpdateException ex)
            {
                // Npgsql 23505 = unique_violation (duplicado)
                var inner = ex.InnerException;
                if (inner != null && inner.Message.Contains("23505"))
                {
                    return Conflict(new {
                        message = "Usuario ya inscrito en esta actividad",
                        type = "already_enrolled",
                        error_code = "DUPLICATE_ENROLLMENT"
                    });
                }

                return Conflict(new { message = "Error al procesar la inscripción. Posible duplicado o cupo lleno.", type = "database_error" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }

        [HttpPost("validate-conflict")]
        public async Task<ActionResult> ValidateTimeConflict([FromBody] ValidateConflictRequest request)
        {
            try
            {
                var result = await _enrollmentService.ValidateTimeConflictAsync(request.UserId, request.ActivityId);
                
                if (result.HasConflict)
                {
                    return Ok(new 
                    { 
                        hasConflict = true, 
                        message = result.Message,
                        conflictingActivity = result.ConflictingActivity != null ? new
                        {
                            id = result.ConflictingActivity.Id,
                            title = result.ConflictingActivity.Title,
                            startTime = result.ConflictingActivity.StartTime,
                            endTime = result.ConflictingActivity.EndTime
                        } : null
                    });
                }

                return Ok(new { hasConflict = false });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }

        [HttpPost("validate-time-conflicts")]
        [AllowAnonymous]
        public async Task<ActionResult> ValidateTimeConflicts([FromBody] ValidateTimeConflictsRequest request)
        {
            try
            {
                if (request.ActivityIds == null || !request.ActivityIds.Any())
                {
                    return Ok(new 
                    { 
                        hasConflicts = false, 
                        conflicts = new List<string>(),
                        message = "No se detectaron conflictos de horario"
                    });
                }

                var result = await _enrollmentService.ValidateTimeConflictsAsync(request.ActivityIds);
                
                return Ok(new 
                { 
                    hasConflicts = result.HasConflicts, 
                    conflicts = result.Conflicts,
                    message = result.HasConflicts ? "Se detectaron conflictos de horario" : "No se detectaron conflictos de horario"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor.", details = ex.Message });
            }
        }

        [HttpPost("validate-time-conflict")]
        [Authorize]
        public async Task<ActionResult> ValidateTimeConflict([FromBody] ValidateTimeConflictRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new { message = "Usuario no autenticado." });
                }

                var result = await _enrollmentService.ValidateTimeConflictWithUserEnrollmentsAsync(userGuid, request.ActivityIds);
                
                if (result.HasConflicts)
                {
                    return Conflict(new { conflicts = result.Conflicts });
                }

                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor.", details = ex.Message });
            }
        }

        [HttpPost("check-capacity")]
        [AllowAnonymous]
        public async Task<ActionResult> CheckCapacity([FromBody] CheckCapacityRequest request)
        {
            try
            {
                var result = await _enrollmentService.CheckCapacityStatusAsync(request.ActivityIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor.", details = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, [FromQuery] Guid userId)
        {
            try
            {
                var deleted = await _enrollmentService.DeleteEnrollmentAsync(id, userId);
                if (!deleted)
                    return NotFound("Inscripción no encontrada.");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error interno del servidor.");
            }
        }
    }

    // Legacy DTO for backward compatibility
    public class EnrollmentRequest
    {
        public Guid UserId { get; set; }
        public List<Guid> ActivityIds { get; set; } = new List<Guid>();
    }

    public class ValidateConflictRequest
    {
        public Guid UserId { get; set; }
        public Guid ActivityId { get; set; }
    }

    public class ValidateTimeConflictsRequest
    {
        public List<Guid> ActivityIds { get; set; } = new List<Guid>();
    }

    public class ValidateTimeConflictRequest
    {
        public List<Guid> ActivityIds { get; set; } = new List<Guid>();
    }

    public class CheckCapacityRequest
    {
        public List<Guid> ActivityIds { get; set; } = new List<Guid>();
    }
}