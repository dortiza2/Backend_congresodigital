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
using Microsoft.AspNetCore.Antiforgery;

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
    private readonly NpgsqlDataSource _dataSource;

    public AuthController(CongresoDbContext db, IEmailService emailService, ILogger<AuthController> logger, IConfiguration configuration, IAuthTokenService authTokenService, NpgsqlDataSource dataSource)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        _authTokenService = authTokenService;
        _dataSource = dataSource;
    }

    public record LoginDto(string Email, string Password);
    public record DebugCreateDto(string Email, string Password, string FullName, int RoleLevel);

    [AllowAnonymous]
    [HttpGet("debug-user")]
    public async Task<IActionResult> DebugUser([FromQuery][Required] string email)
    {
        try
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            using var cmd = _dataSource.CreateCommand("select id, email, password_hash, is_active from users where lower(email) = @email limit 1");
            cmd.Parameters.Add(new NpgsqlParameter<string>("email", normalizedEmail));
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.Read())
            {
                return Ok(new { found = false, email = normalizedEmail });
            }
            var idType = reader.GetFieldType(0);
            string idValue;
            if (idType == typeof(Guid))
            {
                idValue = reader.GetGuid(0).ToString();
            }
            else if (idType == typeof(long))
            {
                idValue = reader.GetInt64(0).ToString();
            }
            else
            {
                idValue = reader.GetValue(0)?.ToString() ?? string.Empty;
            }
            var dbEmail = reader.IsDBNull(1) ? normalizedEmail : reader.GetString(1);
            var pwdHash = reader.IsDBNull(2) ? null : reader.GetString(2);
            var isActive = !reader.IsDBNull(3) && reader.GetBoolean(3);
            var hashPreview = string.IsNullOrEmpty(pwdHash) ? "<null>" : (pwdHash.Length > 20 ? pwdHash.Substring(0, 20) + "..." : pwdHash);
            return Ok(new { found = true, id = idValue, email = dbEmail, isActive, hasHash = !string.IsNullOrEmpty(pwdHash), hashPreview });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GET /api/auth/debug-user: error al leer usuario");
            return StatusCode(400, new { statusCode = 400, code = "invalid_operation", message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("debug-create-user")]
    public async Task<IActionResult> DebugCreateUser([FromBody] DebugCreateDto dto)
    {
        try
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Email y password son requeridos" });
            }

            // Verificar si ya existe
            using (var checkCmd = _dataSource.CreateCommand("select id from users where lower(email) = @email limit 1"))
            {
                checkCmd.Parameters.Add(new Npgsql.NpgsqlParameter<string>("email", email));
                using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Conflict(new { message = "El usuario ya existe" });
                }
            }

            // Hash de password
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Insertar usuario y obtener id
            Guid? userGuid = null;
            long? userLong = null;
            using (var insertCmd = _dataSource.CreateCommand("insert into users (email, full_name, password_hash, is_active, created_at) values (@email, @full_name, @password_hash, true, now()) returning id"))
            {
                insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter<string>("email", email));
                insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter<string>("full_name", dto.FullName ?? ""));
                insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter<string>("password_hash", hash));

                using var reader = await insertCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return StatusCode(500, new { message = "No se pudo crear usuario" });
                }
                var idType = reader.GetFieldType(0);
                if (idType == typeof(Guid))
                {
                    userGuid = reader.GetGuid(0);
                }
                else if (idType == typeof(long))
                {
                    userLong = reader.GetInt64(0);
                }
                else
                {
                    var raw = reader.GetValue(0);
                    if (raw is Guid g) userGuid = g; else if (raw is long l) userLong = l; else return StatusCode(500, new { message = "Tipo de id no soportado" });
                }
            }

            // Determinar roleId según roleLevel solicitado
            int roleId = dto.RoleLevel switch
            {
                3 => 3, // superadmin
                2 => 2, // admin
                1 => 1, // staff/asistente
                0 => 4, // participant/estudiante
                _ => 4
            };

            // Insertar rol
            using (var roleCmd = _dataSource.CreateCommand("insert into user_roles (user_id, role_id) values (@user_id, @role_id)"))
            {
                if (userGuid.HasValue)
                {
                    roleCmd.Parameters.Add(new Npgsql.NpgsqlParameter<Guid>("user_id", userGuid.Value));
                }
                else if (userLong.HasValue)
                {
                    roleCmd.Parameters.Add(new Npgsql.NpgsqlParameter<long>("user_id", userLong.Value));
                }
                else
                {
                    return StatusCode(500, new { message = "Usuario sin id válido" });
                }
                roleCmd.Parameters.Add(new Npgsql.NpgsqlParameter<int>("role_id", roleId));
                await roleCmd.ExecuteNonQueryAsync();
            }

            var idString = userGuid?.ToString() ?? (userLong?.ToString() ?? string.Empty);
            return Ok(new { created = true, id = idString, email, roleId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en debug-create-user");
            return StatusCode(400, new { statusCode = 400, code = "invalid_operation", message = ex.Message });
        }
    }
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        Guid? userGuid = null;
        long? userLong = null;
        string normalizedEmail = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        string email = normalizedEmail;
        string? fullName = null;
        string? pwdHash = null;
        bool isActive = true;
        string idString = string.Empty;
    
        try
        {
            using var cmd = _dataSource.CreateCommand("select id, email, full_name, password_hash, is_active from users where lower(email) = @email limit 1");
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter<string>("email", normalizedEmail));
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                _logger.LogWarning("Login fallido: usuario no encontrado para email {Email}", normalizedEmail);
                return Unauthorized(new { message = "Invalid credentials" });
            }
            var idType = reader.GetFieldType(0);
            if (idType == typeof(Guid))
            {
                userGuid = reader.GetGuid(0);
            }
            else if (idType == typeof(long))
            {
                userLong = reader.GetInt64(0);
            }
            else
            {
                var raw = reader.GetValue(0);
                if (raw is Guid g) userGuid = g; else if (raw is long l) userLong = l;
            }
            idString = userGuid?.ToString() ?? (userLong?.ToString() ?? string.Empty);
            email = reader.IsDBNull(1) ? normalizedEmail : reader.GetString(1);
            fullName = reader.IsDBNull(2) ? null : reader.GetString(2);
            pwdHash = reader.IsDBNull(3) ? null : reader.GetString(3);
            isActive = !reader.IsDBNull(4) && reader.GetBoolean(4);
    
            var hasHash = !string.IsNullOrEmpty(pwdHash);
            bool verified = hasHash && BCrypt.Net.BCrypt.Verify(dto.Password, pwdHash);
            var hashPreview = string.IsNullOrEmpty(pwdHash) ? "<null>" : (pwdHash!.Length > 12 ? pwdHash.Substring(0, 12) + "..." : pwdHash);
            _logger.LogInformation("Diagnóstico login(raw): userId={UserId}, email={Email}, isActive={IsActive}, hasHash={HasHash}, verified={Verified}", idString, email, isActive, hasHash, verified);
            if (!verified || !isActive)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "POST /api/auth/login: error al leer usuario");
            return StatusCode(400, new { statusCode = 400, code = "invalid_operation", message = ex.Message });
        }
    
        // Obtener roles y roleLevel desde SQL directo
        string[] roles = Array.Empty<string>();
        int roleLevel = 1; // default asistente
        try
        {
            using var rolesCmd = _dataSource.CreateCommand("select r.code, coalesce(r.level, 1) as level from user_roles ur join roles r on r.id = ur.role_id where ur.user_id = @userId");
            if (userGuid.HasValue)
            {
                rolesCmd.Parameters.Add(new Npgsql.NpgsqlParameter<Guid>("userId", userGuid.Value));
            }
            else if (userLong.HasValue)
            {
                rolesCmd.Parameters.Add(new Npgsql.NpgsqlParameter<long>("userId", userLong.Value));
            }
            else
            {
                rolesCmd.Parameters.Add(new Npgsql.NpgsqlParameter<long>("userId", 0L));
            }
            using var rReader = await rolesCmd.ExecuteReaderAsync();
            var codes = new List<string>();
            var levels = new List<int>();
            while (await rReader.ReadAsync())
            {
                codes.Add(rReader.GetString(0));
                levels.Add(rReader.IsDBNull(1) ? 1 : rReader.GetInt32(1));
            }
            roles = codes.ToArray();
            roleLevel = levels.Count > 0 ? levels.Max() : 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "POST /api/auth/login: error obteniendo roles para {UserId}", idString);
        }
    
        // Flags de acceso según lógica acordada
        var canAccessDashboard = roleLevel <= 3;
        var canAccessAccount = roleLevel == 0 || roleLevel == 4;
    
        // Emitir JWT
        var (token, exp) = userGuid.HasValue
            ? _authTokenService.CreateToken(userGuid.Value, email, roles, roleLevel, fullName)
            : _authTokenService.CreateToken(userLong ?? 0L, email, roles, roleLevel, fullName);
    
        return Ok(new
        {
            token,
            exp,
            user = new
            {
                id = idString,
                email,
                fullName,
                roles,
                roleLevel,
                access = new { dashboard = canAccessDashboard, account = canAccessAccount }
            }
        });
    }
    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrf([FromServices] IAntiforgery antiforgery)
    {
        // Emite cookie XSRF-TOKEN mediante Antiforgery y devuelve el token para header X-XSRF-TOKEN
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        var token = tokens.RequestToken ?? string.Empty;
        return Ok(new { token, headerName = "X-XSRF-TOKEN" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        
        // Obtener roleLevel desde claim o calcular desde BD
        var roleLevel = 1; // Default para asistente
        var roleLevelClaim = User.FindFirst("roleLevel")?.Value;
        if (!string.IsNullOrEmpty(roleLevelClaim) && int.TryParse(roleLevelClaim, out var parsed))
        {
            roleLevel = parsed;
        }
        else if (Guid.TryParse(userId, out var userGuid))
        {
            var levels = await _db.UserRoles
                .Where(ur => ur.UserId == userGuid)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Level)
                .ToArrayAsync();
            roleLevel = levels.Any() ? levels.Max(l => l ?? 1) : 1;
        }
        
        // Flags de acceso según lógica de negocio
        var rolesLower = roles.Select(r => r?.ToLowerInvariant()).ToArray();
        var canAccessDashboard = roleLevel <= 3;
        var canAccessAccount = roleLevel == 0 || roleLevel == 4;

        return Ok(new
        {
            userId = userId,
            email = email,
            roles = roles,
            roleLevel = roleLevel,
            access = new { dashboard = canAccessDashboard, account = canAccessAccount }
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
        
        // Obtener roleLevel desde claim o calcular desde BD
        var roleLevel = 1; // Default para asistente
        var roleLevelClaim = User.FindFirst("roleLevel")?.Value;
        if (!string.IsNullOrEmpty(roleLevelClaim) && int.TryParse(roleLevelClaim, out var parsed))
        {
            roleLevel = parsed;
        }
        else if (Guid.TryParse(userId, out var userGuid))
        {
            var levels = await _db.UserRoles
                .Where(ur => ur.UserId == userGuid)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Level)
                .ToArrayAsync();
            roleLevel = levels.Any() ? levels.Max(l => l ?? 1) : 1;
        }
        
        // Flags de acceso según lógica de negocio
        var rolesLower = roles.Select(r => r?.ToLowerInvariant()).ToArray();
        var canAccessDashboard = roleLevel <= 3;
        var canAccessAccount = roleLevel == 0 || roleLevel == 4;

        return Ok(new
        {
            isAuthenticated = true,
            user = new
            {
                id = userId,
                email = email,
                name = fullName,
                roles = roles,
                roleLevel = roleLevel,
                access = new { dashboard = canAccessDashboard, account = canAccessAccount }
            }
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
        var userRoles = new[] { "STUDENT" };
        var roleLevel = 0;

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

                    // Asignar rol de estudiante (roleId 4) de forma robusta
                    var studentRole = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == 4)
                        ?? await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Level == 0)
                        ?? await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Code.ToLower() == "participant" || r.Code.ToLower() == "student");

                    if (studentRole != null)
                    {
                        _db.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = studentRole.Id
                        });
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Rol de estudiante asignado al usuario {UserId}", user.Id);
                    }
                    else
                    {
                        _logger.LogWarning("No se encontró rol estudiante (id=4/level=0). Usuario {UserId} sin rol explícito", user.Id);
                    }

                    _logger.LogInformation("Guardando cambios en la base de datos tras asignar rol...");
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Cambios guardados exitosamente.");

                    // Confirmar transacción
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transacción confirmada exitosamente");
                }
                catch (Exception ex)
                {
                    try { await transaction.RollbackAsync(); } catch { }
                    throw;
                }
            });

            // Enviar email de confirmación (fuera de la transacción y fuera de los reintentos)
            if (user != null)
            {
                try
                {
                    var hasEnrollments = await _db.Enrollments.AnyAsync(e => e.UserId == user.Id);
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

                // Crear claims para la cookie de autenticación
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
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
            }
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

        if (user == null)
        {
            return StatusCode(500, new { message = "Error interno del servidor: usuario no encontrado después del registro" });
        }

        return Ok(new
        {
            message = "Usuario registrado exitosamente",
            user = new
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsUmg = user.IsUmg,
                OrgName = user.OrgName,
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