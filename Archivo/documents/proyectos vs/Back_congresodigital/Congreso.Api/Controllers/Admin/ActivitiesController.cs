using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Congreso.Api.Services;
using Congreso.Api.DTOs;
using System.Security.Claims;


namespace Congreso.Api.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [System.Obsolete("Use AdminActivitiesController instead")]
    [Route("api/legacy/admin/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly CongresoDbContext _db;
        private readonly IActivityAdminService _activityAdminService;

        public ActivitiesController(CongresoDbContext context, IActivityAdminService activityAdminService)
        {
            _db = context;
            _activityAdminService = activityAdminService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminActivityDto>>> GetActivities()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                var activities = await _activityAdminService.GetAdminActivitiesAsync();
                return Ok(activities);
            }
            catch (Exception ex)
            {
                return Ok(new List<ActivityAdminDto>());
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ActivityAdminDto>> GetActivity(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                var activity = await _activityAdminService.GetAdminActivityByIdAsync(id);
                if (activity == null)
                    return NotFound(new { message = "activity_not_found" });

                return Ok(activity);
            }
            catch (Exception ex)
            {
                return Ok(new { message = "error_retrieving_activity", success = false });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ActivityAdminDto>> CreateActivity([FromBody] CreateAdminActivityDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var activity = await _activityAdminService.CreateAdminActivityAsync(dto);
                return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, activity);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { message = "error_creating_activity", success = false });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateAdminActivityDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                if (dto.ScheduledAt.HasValue && dto.ScheduledAt.Value < DateTime.UtcNow)
                    return BadRequest(new { message = "scheduledAt must be in the future" });

                var activity = await _activityAdminService.UpdateAdminActivityAsync(id, dto);
                if (activity == null)
                    return NotFound(new { message = "activity_not_found" });

                return Ok(new { data = activity });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { message = "error_updating_activity", success = false });
            }
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody] UpdateAdminActivityDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                if (dto.ScheduledAt.HasValue && dto.ScheduledAt.Value < DateTime.UtcNow)
                    return BadRequest(new { message = "scheduledAt must be in the future" });

                var activity = await _activityAdminService.UpdateAdminActivityAsync(id, dto);
                if (activity == null)
                    return NotFound(new { message = "activity_not_found" });

                return Ok(new { data = activity });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { message = "error_updating_activity", success = false });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                var success = await _activityAdminService.DeleteAdminActivityAsync(id);
                if (!success)
                    return NotFound(new { message = "activity_not_found" });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { message = "error_deleting_activity", success = false });
            }
        }

        [HttpPatch("{id:guid}/publish")]
        public async Task<IActionResult> PublishActivity(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                var activity = await _activityAdminService.PublishAdminActivityAsync(id);
                if (activity == null)
                    return NotFound(new { message = "activity_not_found" });

                return Ok(new { data = activity, message = "Activity published successfully" });
            }
            catch (Exception ex)
            {
                return Ok(new { message = "error_publishing_activity", success = false });
            }
        }

        [HttpPatch("{id:guid}/unpublish")]
        public async Task<IActionResult> UnpublishActivity(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized();

                if (!await _activityAdminService.CanManageActivitiesAsync(currentUserId.Value))
                    return StatusCode(403, new { message = "insufficient_permissions" });

                var activity = await _activityAdminService.PublishAdminActivityAsync(id);
                if (activity == null)
                    return NotFound(new { message = "activity_not_found" });

                return Ok(new { data = activity, message = "Activity unpublished successfully" });
            }
            catch (Exception ex)
            {
                return Ok(new { message = "error_unpublishing_activity", success = false });
            }
        }

        private async Task<bool> ActivityExists(Guid id)
        {
            return await _db.Activities.AnyAsync(e => e.Id == id);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}