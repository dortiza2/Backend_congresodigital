using Congreso.Api.DTOs;
using Congreso.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Congreso.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/admin/activities")]
    public class AdminActivitiesController : ControllerBase
    {
        private readonly IActivityAdminService _activityAdminService;
        private readonly ILogger<AdminActivitiesController> _logger;

        public AdminActivitiesController(IActivityAdminService activityAdminService, ILogger<AdminActivitiesController> logger)
        {
            _activityAdminService = activityAdminService;
            _logger = logger;
        }

        // GET /api/admin/activities
        [HttpGet]
        [Authorize(Policy = "RequireStaffOrHigher")]
        [SwaggerOperation(Summary = "Listar actividades admin", Description = "Obtiene el listado de actividades administrativas")]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityDto>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<AdminActivityDto>>>> GetAdminActivities()
        {
            try
            {
                var activities = await _activityAdminService.GetAdminActivitiesAsync();
                return Ok(ApiResponse<List<AdminActivityDto>>.CreateSuccess(activities, "Admin activities retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin activities");
                return StatusCode(500, ApiResponse<List<AdminActivityDto>>.CreateError("error_retrieving_admin_activities"));
            }
        }

        // GET /api/admin/activities/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "RequireStaffOrHigher")]
        [SwaggerOperation(Summary = "Obtener actividad admin", Description = "Obtiene una actividad administrativa por ID")]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AdminActivityDto>>> GetAdminActivity(Guid id)
        {
            try
            {
                var activity = await _activityAdminService.GetAdminActivityByIdAsync(id);
                if (activity == null)
                {
                    return NotFound(ApiResponse<AdminActivityDto>.CreateError("admin_activity_not_found"));
                }

                return Ok(ApiResponse<AdminActivityDto>.CreateSuccess(activity, "Admin activity retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin activity by ID");
                return StatusCode(500, ApiResponse<AdminActivityDto>.CreateError("error_retrieving_admin_activity"));
            }
        }

        // POST /api/admin/activities
        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        [SwaggerOperation(Summary = "Crear actividad admin", Description = "Crea una nueva actividad administrativa")]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AdminActivityDto>>> CreateAdminActivity([FromBody] CreateAdminActivityDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var activity = await _activityAdminService.CreateAdminActivityAsync(dto);
                return Ok(ApiResponse<AdminActivityDto>.CreateSuccess(activity, "Admin activity created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return Ok(ApiResponse<AdminActivityDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin activity");
                return StatusCode(500, ApiResponse<AdminActivityDto>.CreateError("error_creating_admin_activity"));
            }
        }

        // PUT /api/admin/activities/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "RequireAdmin")]
        [SwaggerOperation(Summary = "Actualizar actividad admin", Description = "Actualiza una actividad administrativa por ID")]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AdminActivityDto>>> UpdateAdminActivity(Guid id, [FromBody] UpdateAdminActivityDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var activity = await _activityAdminService.UpdateAdminActivityAsync(id, dto);
                if (activity == null)
                {
                    return NotFound(ApiResponse<AdminActivityDto>.CreateError("admin_activity_not_found"));
                }

                return Ok(ApiResponse<AdminActivityDto>.CreateSuccess(activity, "Admin activity updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return Ok(ApiResponse<AdminActivityDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin activity");
                return StatusCode(500, ApiResponse<AdminActivityDto>.CreateError("error_updating_admin_activity"));
            }
        }

        // DELETE /api/admin/activities/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "RequireSuperAdmin")]
        [SwaggerOperation(Summary = "Eliminar actividad admin", Description = "Elimina una actividad administrativa por ID")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAdminActivity(Guid id)
        {
            try
            {
                var success = await _activityAdminService.DeleteAdminActivityAsync(id);
                if (!success)
                {
                    return NotFound(ApiResponse<bool>.CreateError("admin_activity_not_found"));
                }

                return Ok(ApiResponse<bool>.CreateSuccess(true, "Admin activity deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin activity");
                return StatusCode(500, ApiResponse<bool>.CreateError("error_deleting_admin_activity"));
            }
        }

        // PATCH /api/admin/activities/{id}/status
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "RequireAdmin")]
        [SwaggerOperation(Summary = "Actualizar estado de actividad admin", Description = "Actualiza el estado de una actividad administrativa")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateAdminActivityStatus(Guid id, [FromBody] UpdateAdminActivityStatusDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var success = await _activityAdminService.UpdateAdminActivityStatusAsync(id, dto.Status);
                if (!success)
                {
                    return NotFound(ApiResponse<bool>.CreateError("admin_activity_not_found"));
                }

                return Ok(ApiResponse<bool>.CreateSuccess(true, "Admin activity status updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin activity status");
                return StatusCode(500, ApiResponse<bool>.CreateError("error_updating_admin_activity_status"));
            }
        }

        // GET /api/admin/activities/stats
        [HttpGet("stats")]
        [Authorize(Policy = "RequireStaffOrHigher")]
        [SwaggerOperation(Summary = "Estadísticas de actividades admin", Description = "Obtiene estadísticas agregadas de actividades administrativas")]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityStatsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AdminActivityStatsDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AdminActivityStatsDto>>> GetAdminActivityStats()
        {
            try
            {
                var stats = await _activityAdminService.GetAdminActivityStatsAsync();
                return Ok(ApiResponse<AdminActivityStatsDto>.CreateSuccess(stats, "Admin activity statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin activity statistics");
                return StatusCode(500, ApiResponse<AdminActivityStatsDto>.CreateError("error_retrieving_admin_activity_stats"));
            }
        }

        // GET /api/admin/activities/recent
        [HttpGet("recent")]
        [Authorize(Policy = "RequireStaffOrHigher")]
        [SwaggerOperation(Summary = "Actividades admin recientes", Description = "Obtiene las actividades administrativas más recientes")]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityDto>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<AdminActivityDto>>>> GetRecentAdminActivities([FromQuery] int count = 10)
        {
            try
            {
                var activities = await _activityAdminService.GetRecentAdminActivitiesAsync(count);
                return Ok(ApiResponse<List<AdminActivityDto>>.CreateSuccess(activities, "Recent admin activities retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent admin activities");
                return StatusCode(500, ApiResponse<List<AdminActivityDto>>.CreateError("error_retrieving_recent_admin_activities"));
            }
        }

        // GET /api/admin/activities/by-user/{userId}
        [HttpGet("by-user/{userId:guid}")]
        [Authorize(Policy = "RequireStaffOrHigher")]
        [SwaggerOperation(Summary = "Actividades admin por usuario", Description = "Obtiene actividades administrativas filtradas por usuario")]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityDto>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<AdminActivityDto>>>> GetAdminActivitiesByUser(Guid userId)
        {
            try
            {
                var activities = await _activityAdminService.GetAdminActivitiesByUserAsync(userId);
                return Ok(ApiResponse<List<AdminActivityDto>>.CreateSuccess(activities, "User admin activities retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin activities by user");
                return StatusCode(500, ApiResponse<List<AdminActivityDto>>.CreateError("error_retrieving_user_admin_activities"));
            }
        }
    }
}