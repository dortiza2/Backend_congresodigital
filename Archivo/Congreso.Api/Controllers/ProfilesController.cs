using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Congreso.Api.Services;
using Congreso.Api.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Congreso.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(IProfileService profileService, ILogger<ProfilesController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    #region Staff Profiles

    /// <summary>
    /// Obtiene todos los perfiles de staff (solo admins)
    /// </summary>
    [SwaggerOperation(Summary = "Listar perfiles de staff", Description = "Devuelve todos los perfiles de staff (solo admins)")]
    [ProducesResponseType(typeof(List<StaffAccountDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpGet("staff")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<StaffAccountDto>>> GetAllStaffAccounts()
    {
        var staffAccounts = await _profileService.GetAllStaffAccountsAsync();
        return Ok(staffAccounts);
    }

    /// <summary>
    /// Obtiene un perfil de staff específico
    /// </summary>
    [SwaggerOperation(Summary = "Obtener perfil de staff", Description = "Devuelve un perfil de staff por ID")]
    [ProducesResponseType(typeof(StaffAccountDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpGet("staff/{userId:guid}")]
    public async Task<ActionResult<StaffAccountDto>> GetStaffAccount(Guid userId)
    {
        // Verificar permisos: admin o el mismo usuario
        if (!CanAccessProfile(userId))
            return Forbid();

        var staff = await _profileService.GetStaffAccountAsync(userId);
        if (staff == null)
            return NotFound("Perfil de staff no encontrado");

        return Ok(staff);
    }

    /// <summary>
    /// Crea un nuevo perfil de staff (solo super admin)
    /// </summary>
    [SwaggerOperation(Summary = "Crear perfil de staff", Description = "Crea un perfil de staff (solo super admin)")]
    [ProducesResponseType(typeof(StaffAccountDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpPost("staff")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<StaffAccountDto>> CreateStaffAccount([FromBody] CreateStaffAccountDto dto)
    {
        try
        {
            var staff = await _profileService.CreateStaffAccountAsync(dto);
            return CreatedAtAction(nameof(GetStaffAccount), new { userId = staff.UserId }, staff);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Actualiza un perfil de staff
    /// </summary>
    [SwaggerOperation(Summary = "Actualizar perfil de staff", Description = "Actualiza datos de un perfil de staff")]
    [ProducesResponseType(typeof(StaffAccountDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpPut("staff/{userId:guid}")]
    public async Task<ActionResult<StaffAccountDto>> UpdateStaffAccount(Guid userId, [FromBody] UpdateStaffAccountDto dto)
    {
        // Verificar permisos: super admin o el mismo usuario (con restricciones)
        if (!CanModifyStaffProfile(userId, dto))
            return Forbid();

        var staff = await _profileService.UpdateStaffAccountAsync(userId, dto);
        if (staff == null)
            return NotFound("Perfil de staff no encontrado");

        return Ok(staff);
    }

    /// <summary>
    /// Elimina un perfil de staff (solo super admin)
    /// </summary>
    [SwaggerOperation(Summary = "Eliminar perfil de staff", Description = "Elimina un perfil de staff por ID")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpDelete("staff/{userId:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult> DeleteStaffAccount(Guid userId)
    {
        var success = await _profileService.DeleteStaffAccountAsync(userId);
        if (!success)
            return NotFound("Perfil de staff no encontrado");

        return NoContent();
    }

    #endregion

    #region Student Profiles

    /// <summary>
    /// Actualiza el perfil básico del usuario actual (FullName, OrgName)
    /// </summary>
    [SwaggerOperation(Summary = "Actualizar mi perfil", Description = "Actualiza los campos permitidos del perfil del usuario autenticado (FullName, OrgName)")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [HttpPut("me")]
    [HttpPut("/api/profile/me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] StudentController.UpdateProfileDto dto, [FromServices] Congreso.Api.Data.CongresoDbContext db)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        if (dto == null)
            return BadRequest(new { message = "Invalid payload" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;

        if (!string.IsNullOrWhiteSpace(dto.OrgName))
            user.OrgName = dto.OrgName;

        await db.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully" });
    }

    /// <summary>
    /// Obtiene todos los perfiles de estudiantes (solo staff)
    /// </summary>
    [SwaggerOperation(Summary = "Listar perfiles de estudiantes", Description = "Devuelve todos los perfiles de estudiantes (solo staff)")]
    [ProducesResponseType(typeof(List<StudentAccountDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpGet("students")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<ActionResult<List<StudentAccountDto>>> GetAllStudentAccounts()
    {
        var studentAccounts = await _profileService.GetAllStudentAccountsAsync();
        return Ok(studentAccounts);
    }

    /// <summary>
    /// Obtiene un perfil de estudiante específico
    /// </summary>
    [SwaggerOperation(Summary = "Obtener perfil de estudiante", Description = "Devuelve un perfil de estudiante por ID")]
    [ProducesResponseType(typeof(StudentAccountDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpGet("students/{userId:guid}")]
    public async Task<ActionResult<StudentAccountDto>> GetStudentAccount(Guid userId)
    {
        // Verificar permisos: staff o el mismo usuario
        if (!CanAccessProfile(userId))
            return Forbid();

        var student = await _profileService.GetStudentAccountAsync(userId);
        if (student == null)
            return NotFound("Perfil de estudiante no encontrado");

        return Ok(student);
    }

    /// <summary>
    /// Crea un nuevo perfil de estudiante
    /// </summary>
    [SwaggerOperation(Summary = "Crear perfil de estudiante", Description = "Crea un perfil de estudiante (solo staff)")]
    [ProducesResponseType(typeof(StudentAccountDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpPost("students")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<ActionResult<StudentAccountDto>> CreateStudentAccount([FromBody] CreateStudentAccountDto dto)
    {
        try
        {
            var student = await _profileService.CreateStudentAccountAsync(dto);
            return CreatedAtAction(nameof(GetStudentAccount), new { userId = student.UserId }, student);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Actualiza un perfil de estudiante
    /// </summary>
    [SwaggerOperation(Summary = "Actualizar perfil de estudiante", Description = "Actualiza datos de un perfil de estudiante")]
    [ProducesResponseType(typeof(StudentAccountDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpPut("students/{userId:guid}")]
    public async Task<ActionResult<StudentAccountDto>> UpdateStudentAccount(Guid userId, [FromBody] UpdateStudentAccountDto dto)
    {
        // Verificar permisos: staff o el mismo usuario
        if (!CanAccessProfile(userId))
            return Forbid();

        var student = await _profileService.UpdateStudentAccountAsync(userId, dto);
        if (student == null)
            return NotFound("Perfil de estudiante no encontrado");

        return Ok(student);
    }

    /// <summary>
    /// Elimina un perfil de estudiante (solo admins)
    /// </summary>
    [SwaggerOperation(Summary = "Eliminar perfil de estudiante", Description = "Elimina un perfil de estudiante por ID")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpDelete("students/{userId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> DeleteStudentAccount(Guid userId)
    {
        var success = await _profileService.DeleteStudentAccountAsync(userId);
        if (!success)
            return NotFound("Perfil de estudiante no encontrado");

        return NoContent();
    }

    #endregion

    #region User with Profiles

    /// <summary>
    /// Obtiene todos los usuarios con sus perfiles (solo admins)
    /// </summary>
    [SwaggerOperation(Summary = "Listar usuarios con perfiles", Description = "Devuelve todos los usuarios con su perfil asociado (solo admins)")]
    [ProducesResponseType(typeof(List<UserWithProfileDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpGet("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<UserWithProfileDto>>> GetAllUsersWithProfiles()
    {
        var users = await _profileService.GetAllUsersWithProfilesAsync();
        return Ok(users);
    }

    /// <summary>
    /// Obtiene un usuario con su perfil
    /// </summary>
    [SwaggerOperation(Summary = "Obtener usuario con perfil", Description = "Devuelve un usuario y su perfil")]
    [ProducesResponseType(typeof(UserWithProfileDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<UserWithProfileDto>> GetUserWithProfile(Guid userId)
    {
        // Verificar permisos: staff o el mismo usuario
        if (!CanAccessProfile(userId))
            return Forbid();

        var user = await _profileService.GetUserWithProfileAsync(userId);
        if (user == null)
            return NotFound("Usuario no encontrado");

        return Ok(user);
    }

    /// <summary>
    /// Crea un usuario con perfil (solo admins)
    /// </summary>
    [SwaggerOperation(Summary = "Crear usuario con perfil", Description = "Crea un usuario y su perfil asociado (solo admins)")]
    [ProducesResponseType(typeof(UserWithProfileDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpPost("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserWithProfileDto>> CreateUserWithProfile([FromBody] CreateUserWithProfileDto dto)
    {
        try
        {
            _logger.LogDebug("Controller received DTO: Email={Email}, ProfileType={ProfileType}, StaffRole={StaffRole}", dto.Email, dto.ProfileType, dto.StaffRole);
            
            if (!ModelState.IsValid)
            {
                _logger.LogDebug("ModelState invalid: {Errors}", string.Join(", ", ModelState.Select(e => $"{e.Key}: {string.Join("|", e.Value.Errors.Select(er => er.ErrorMessage))}")));
                return BadRequest(ModelState);
            }
            
            var user = await _profileService.CreateUserWithProfileAsync(dto);
            return CreatedAtAction(nameof(GetUserWithProfile), new { userId = user.Id }, user);
        }
        catch (ArgumentException ex)
        {
            _logger.LogDebug(ex, "ArgumentException in CreateUserWithProfile");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "InvalidOperationException in CreateUserWithProfile");
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception in CreateUserWithProfile");
            throw;
        }
    }

    /// <summary>
    /// Obtiene el perfil del usuario actual
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserWithProfileDto>> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _profileService.GetUserWithProfileAsync(userId.Value);
        if (user == null)
            return NotFound("Usuario no encontrado");

        return Ok(user);
    }

    #endregion

    #region Profile Validation

    /// <summary>
    /// Valida la exclusividad de perfiles para un usuario
    /// </summary>
    [SwaggerOperation(Summary = "Validar exclusividad de perfiles", Description = "Valida que un usuario no tenga múltiples perfiles incompatibles")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpGet("validate/{userId:guid}")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<ActionResult<object>> ValidateProfileExclusivity(Guid userId)
    {
        var isValid = await _profileService.ValidateProfileExclusivityAsync(userId);
        var profileType = await _profileService.GetUserProfileTypeAsync(userId);

        return Ok(new
        {
            UserId = userId,
            IsValid = isValid,
            ProfileType = profileType,
            Message = isValid ? "Perfiles válidos" : "Usuario tiene múltiples perfiles (violación de exclusividad)"
        });
    }

    #endregion

    #region Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst("role")?.Value;
    }

    private bool IsCurrentUserAdmin()
    {
        var role = GetCurrentUserRole();
        return role == "adminDev" || role == "admin";
    }

    private bool IsCurrentUserSuperAdmin()
    {
        return GetCurrentUserRole() == "adminDev";
    }

    private bool IsCurrentUserStaff()
    {
        var role = GetCurrentUserRole();
        return role == "adminDev" || role == "admin" || role == "asistente";
    }

    private bool CanAccessProfile(Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        
        // El mismo usuario siempre puede acceder a su perfil
        if (currentUserId == userId)
            return true;

        // Staff puede acceder a cualquier perfil
        return IsCurrentUserStaff();
    }

    private bool CanModifyStaffProfile(Guid userId, UpdateStaffAccountDto dto)
    {
        var currentUserId = GetCurrentUserId();

        // Super admin puede modificar cualquier perfil
        if (IsCurrentUserSuperAdmin())
            return true;

        // El mismo usuario puede modificar su perfil con restricciones
        if (currentUserId == userId)
        {
            // No puede cambiar su propio rol
            if (dto.StaffRole.HasValue)
                return false;

            return true;
        }

        // Admin puede modificar perfiles de asistentes
        if (IsCurrentUserAdmin())
        {
            // Verificar que no esté intentando modificar otro admin o super admin
            // (esto requeriría consultar el perfil actual, se simplifica por ahora)
            return true;
        }

        return false;
    }

    #endregion
}