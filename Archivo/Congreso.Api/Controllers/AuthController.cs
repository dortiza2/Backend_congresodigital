using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Congreso.Api.Data;
using Congreso.Api.Services;
using Congreso.Api.Models;
using Congreso.Api.Models.Email;
using Congreso.Api.Attributes;
using Npgsql;

namespace Congreso.Api.Controllers;

public class RoleResult
{
    public string Code { get; set; } = string.Empty;
    public int Level { get; set; }
}

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CongresoDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IAuthTokenService _authTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(CongresoDbContext db, IEmailService emailService, ILogger<AuthController> logger, IConfiguration configuration, IAuthTokenService authTokenService)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        _authTokenService = authTokenService;
    }

    public record LoginDto(string Email, string Password);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });

            // Obtener roles del usuario desde la base de datos (tolerante a errores)
            string[] userRoles = Array.Empty<string>();
            int roleLevel = 1; // Mayor level = mayor privilegio
            try
            {
                var userRolesData = await _db.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Include(ur => ur.Role)
                    .Select(ur => new { ur.Role.Code, ur.Role.Level })
                    .ToArrayAsync();

                userRoles = userRolesData.Select(r => r.Code).ToArray();
                roleLevel = userRolesData.Any() ? userRolesData.Max(r => r.Level ?? 1) : 1;
            }
            catch (Exception exRoles)
            {
                // Nunca 500 si falla lectura de roles; continuar con roles vacíos
                _logger.LogWarning(exRoles, "POST /api/auth/login: fallo al obtener roles, continuando con roles vacíos");
                userRoles = Array.Empty<string>();
                roleLevel = 1;
            }

            // Emitir JWT (sin cookies)
            var (token, exp) = _authTokenService.CreateToken(user.Id, user.Email, userRoles, roleLevel, user.FullName);
            return Ok(new
            {
                message = "Login exitoso",
                token = token,
                tokenType = "Bearer",
                expiresAtUtc = exp,
                user = new { user.Id, user.Email, FullName = user.FullName, roles = userRoles, roleLevel }
            });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogWarning(ex, "POST /api/auth/login: error de BD, devolviendo 401 controlado");
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "POST /api/auth/login: error inesperado, devolviendo 401 controlado");
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        
        // Obtener roleLevel desde la base de datos
        var roleLevel = 1; // Default para asistente
        if (Guid.TryParse(userId, out var userGuid))
        {
            var userRolesData = await _db.UserRoles
                .Where(ur => ur.UserId == userGuid)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Level)
                .ToArrayAsync();
            
            if (userRolesData.Any())
            {
                roleLevel = userRolesData.Max() ?? 1; // Mayor level = mayor privilegio
            }
        }
        
        return Ok(new
        {
            userId = userId,
            email = email,
            roles = roles,
            roleLevel = roleLevel
        });
    }

    [Authorize]
    [HttpGet("session")]
    public async Task<IActionResult> Session()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var fullName = User.FindFirstValue(ClaimTypes.Name);
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        
        // Obtener roleLevel desde la base de datos
        var roleLevel = 1; // Default para asistente
        if (Guid.TryParse(userId, out var userGuid))
        {
            var userRolesData = await _db.UserRoles
                .Where(ur => ur.UserId == userGuid)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Level)
                .ToArrayAsync();
            
            if (userRolesData.Any())
            {
                roleLevel = userRolesData.Max() ?? 1; // Mayor level = mayor privilegio
            }
        }
        
        return Ok(new
        {
            isAuthenticated = true,
            user = new
            {
                id = userId,
                email = email,
                name = fullName,
                roles = roles
            },
            roleLevel = roleLevel
        });
    }

    public record GoogleLoginDto(string Email, string Name, string? Picture);

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        // Obtener dominios permitidos desde configuración
        var allowedDomainsConfig = Environment.GetEnvironmentVariable("GOOGLE_ALLOWED_DOMAINS") ?? "umg.edu.gt";
        var allowedDomains = allowedDomainsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(d => d.Trim())
                                                .ToArray();

        // Validar que el email pertenezca a un dominio permitido
        var emailDomain = dto.Email.Split('@').LastOrDefault();
        if (string.IsNullOrEmpty(emailDomain) || !allowedDomains.Any(domain => emailDomain.Equals(domain, StringComparison.OrdinalIgnoreCase)))
        {
            var domainsText = string.Join(", ", allowedDomains.Select(d => $"@{d}"));
            return StatusCode(403, new { message = $"Solo se permiten correos de los dominios: {domainsText}" });
        }

        // Buscar o crear usuario
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
        
        if (user == null)
        {
            // Crear nuevo usuario UMG
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                FullName = dto.Name ?? dto.Email.Split('@')[0],
                IsUmg = true,
                OrgName = "Universidad Mariano Gálvez",
                AvatarUrl = dto.Picture,
                Status = "ACTIVE",
                PasswordHash = string.Empty // Para usuarios de Google OAuth no necesitamos password
            };
            
            try
            {
                _logger.LogInformation("Creando usuario para email: {Email}", dto.Email);
                
                // Crear usuario
                _db.Users.Add(user);
                _logger.LogInformation("Usuario agregado al contexto con ID: {UserId}", user.Id);
                
                // Asignar rol de asistente (nivel 1)
                var asistenteRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == "ASISTENTE");
                if (asistenteRole != null)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = asistenteRole.Id
                    });
                    _logger.LogInformation("Rol de asistente asignado al usuario {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("No se encontró el rol 'ASISTENTE' en la base de datos");
                }

                // Crear perfil de estudiante automáticamente para usuarios UMG
                var carnet = ExtractStudentIdFromEmail(dto.Email);
                _logger.LogInformation("Carnet extraído del email: {Carnet}", carnet);
                
                var studentAccount = new StudentAccount
                {
                    UserId = user.Id,
                    Carnet = carnet,
                    Career = "Por definir", // Se puede actualizar después
                    CohortYear = DateTime.Now.Year,
                    Organization = "UMG" // Default organization for Google login
                };
                
                _db.StudentAccounts.Add(studentAccount);
                _logger.LogInformation("Perfil de estudiante agregado al contexto para usuario {UserId}", user.Id);

                _logger.LogInformation("Guardando cambios en la base de datos...");
                await _db.SaveChangesAsync();
                _logger.LogInformation("Cambios guardados exitosamente");

                // Enviar email de confirmación de registro para nuevos usuarios
                try
                {
                    // Verificar si el usuario tiene inscripciones
                    var hasEnrollments = await _db.Enrollments
                        .AnyAsync(e => e.UserId == user.Id);

                    // Obtener URL base del frontend desde configuración
                    var frontBaseUrl = _configuration["FRONT_BASE_URL"] ?? "http://localhost:3000";

                    var registrationData = new RegistrationConfirmationEmail
                    {
                        UserId = user.Id,
                        UserEmail = user.Email,
                        FullName = user.FullName,
                        IsUmg = user.IsUmg,
                        OrgName = user.OrgName,
                        HasEnrollments = hasEnrollments,
                        FrontBaseUrl = frontBaseUrl
                    };

                    var emailSent = await _emailService.SendRegistrationConfirmationAsync(registrationData);
                    if (emailSent)
                    {
                        _logger.LogInformation("Email de confirmación enviado exitosamente a {Email}", user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("No se pudo enviar el email de confirmación a {Email}", user.Email);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Error al enviar email de confirmación a {Email}", user.Email);
                    // No fallar el registro por error de email
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario y perfil de estudiante para {Email}. Detalles: {Message}", dto.Email, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
                }
                return StatusCode(500, new { error = "Error al crear usuario", details = ex.Message });
            }
        }

        // Obtener roles del usuario
        var userRolesData = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .Select(ur => new { ur.Role.Code, ur.Role.Level })
            .ToArrayAsync();

        var userRoles = userRolesData.Select(r => r.Code).ToArray();
        var roleLevel = userRolesData.Any() ? userRolesData.Max(r => r.Level ?? 1) : 1; // Mayor level = mayor privilegio

        // Emitir JWT en lugar de cookie
        var (token, exp) = _authTokenService.CreateToken(user.Id, user.Email, userRoles, roleLevel, user.FullName);
        return Ok(new
        {
            message = "Login exitoso",
            token = token,
            tokenType = "Bearer",
            expiresAtUtc = exp,
            user = new 
            { 
                user.Id, 
                user.Email, 
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                IsUmg = user.IsUmg,
                OrgName = user.OrgName,
                roles = userRoles,
                roleLevel = roleLevel
            }
        });
    }

    private string ExtractStudentIdFromEmail(string email)
    {
        // Extraer el carnet del email institucional
        // Formato esperado: carnet@umg.edu.gt
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            return GenerateUniqueCarnet();
            
        var localPart = email.Split('@')[0];
        
        // Si es numérico, es probablemente el carnet
        if (localPart.All(char.IsDigit))
            return localPart;
            
        // Si contiene números, extraer solo los números
        var numbers = new string(localPart.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(numbers) ? GenerateUniqueCarnet() : numbers;
    }

    private string GenerateUniqueCarnet()
    {
        // Generar un carnet único basado en timestamp y random
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var random = new Random().Next(100, 999);
        return $"EXT{timestamp.Substring(timestamp.Length - 6)}{random}";
    }

    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(2, ErrorMessage = "El nombre completo debe tener al menos 2 caracteres")]
        [MaxLength(255, ErrorMessage = "El nombre completo no puede exceder 255 caracteres")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StrongPassword]
        public string Password { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "La institución no puede exceder 255 caracteres")]
        public string? Institution { get; set; }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // Validar que el email no esté ya registrado
        var existingUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == dto.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "El email ya está registrado" });
        }

        // Determinar si es email UMG
        var emailDomain = dto.Email.Split('@').LastOrDefault();
        var isUmgEmail = !string.IsNullOrEmpty(emailDomain) && emailDomain.Equals("umg.edu.gt", StringComparison.OrdinalIgnoreCase);

        // Envolver toda la unidad de trabajo en la ExecutionStrategy para soportar reintentos
        var strategy = _db.Database.CreateExecutionStrategy();
        User? user = null;

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    // Set organization info sin lookup adicional
                    Guid? orgId = null;
                    string orgName = "Externo";
                    if (isUmgEmail)
                    {
                        orgName = "Universidad Mariano Gálvez";
                    }
                    else if (!string.IsNullOrEmpty(dto.Institution))
                    {
                        orgName = dto.Institution;
                    }

                    // Crear nuevo usuario
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = dto.Email,
                        FullName = dto.FullName.Trim(),
                        IsUmg = isUmgEmail,
                        OrgId = orgId,
                        OrgName = orgName,
                        Status = "ACTIVE",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _logger.LogInformation("Creando usuario para email: {Email}", dto.Email);

                    // Crear usuario
                    _db.Users.Add(user);
                    _logger.LogInformation("Usuario agregado al contexto con ID: {UserId}", user.Id);

                    // Guardar usuario primero
                    _logger.LogInformation("Guardando usuario en la base de datos...");
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Usuario guardado exitosamente");

                    // Crear perfil de estudiante
                    var carnet = ExtractStudentIdFromEmail(dto.Email);
                    _logger.LogInformation("Carnet extraído del email: {Carnet}", carnet);

                    _logger.LogInformation("Creando perfil de estudiante...");
                    var studentAccount = new StudentAccount
                    {
                        UserId = user.Id,
                        Carnet = carnet,
                        Career = "Por definir",
                        CohortYear = DateTime.Now.Year,
                        Organization = dto.Institution
                    };

                    _db.StudentAccounts.Add(studentAccount);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Usuario registrado exitosamente");

                    // Commit
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transacción confirmada exitosamente");
                }
                catch
                {
                    try { await transaction.RollbackAsync(); } catch { }
                    throw;
                }
            });
        }
        catch (DbUpdateException ex)
        {
            // Capturar detalles de Postgres si están disponibles
            var root = ex.GetBaseException();
            string? inner = ex.InnerException?.Message ?? root?.Message;
            _logger.LogError(ex, "DbUpdateException al registrar usuario {Email}: {Message}", dto.Email, ex.Message);
            if (!string.IsNullOrWhiteSpace(inner))
            {
                _logger.LogError("Inner: {Inner}", inner);
            }
            return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message, inner = inner });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar usuario para {Email}. Detalles: {Message}", dto.Email, ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
            return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message, inner = ex.InnerException?.Message });
        }

        // Enviar email de confirmación (fuera de la transacción y fuera de los reintentos)
        try
        {
            var hasEnrollments = await _db.Enrollments.AnyAsync(e => e.UserId == user!.Id);
            var frontBaseUrl = _configuration["FRONT_BASE_URL"] ?? "http://localhost:3000";

            var registrationData = new RegistrationConfirmationEmail
            {
                UserId = user.Id,
                UserEmail = user.Email,
                FullName = user.FullName,
                IsUmg = user.IsUmg,
                OrgName = user.OrgName,
                HasEnrollments = hasEnrollments,
                FrontBaseUrl = frontBaseUrl
            };

            var emailSent = await _emailService.SendRegistrationConfirmationAsync(registrationData);
            if (emailSent)
            {
                _logger.LogInformation("Email de confirmación enviado exitosamente a {Email}", user.Email);
            }
            else
            {
                _logger.LogWarning("No se pudo enviar el email de confirmación a {Email}", user.Email);
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Error al enviar email de confirmación a {Email}", user?.Email);
            // No fallar el registro por error de email
        }

        // Crear claims para la cookie de autenticación y devolver respuesta
        var userRoles = new[] { "STUDENT" };
        var roleLevel = 0;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user!.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("roleLevel", roleLevel.ToString())
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);

        return Ok(new
        {
            message = "Usuario registrado exitosamente",
            user = new
            {
                user!.Id,
                user.Email,
                user.FullName,
                user.IsUmg,
                user.OrgName,
                roles = userRoles,
                roleLevel = roleLevel
            }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Cerrar sesión de cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        return Ok(new { message = "Logout exitoso" });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        // Verificar si hay una cookie de autenticación válida
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
            return Unauthorized(new { message = "No hay sesión válida para refrescar" });
        }

        var userId = authenticateResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
        {
            return Unauthorized(new { message = "Datos de sesión inválidos" });
        }

        // Verificar que el usuario aún existe y está activo
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id.ToString() == userId);
        if (user == null || user.Status != "active")
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new { message = "Usuario no válido o inactivo" });
        }

        // Renovar la cookie con nueva expiración
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Renovar por 7 días más
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal, authProperties);
        
        return Ok(new { message = "Sesión refrescada exitosamente" });
    }

}