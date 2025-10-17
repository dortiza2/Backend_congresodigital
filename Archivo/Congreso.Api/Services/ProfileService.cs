using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.DTOs;
using Microsoft.Extensions.Logging;

namespace Congreso.Api.Services;

public interface IProfileService
{
    // Staff operations
    Task<StaffAccountDto?> GetStaffAccountAsync(Guid userId);
    Task<StaffAccountDto> CreateStaffAccountAsync(CreateStaffAccountDto dto);
    Task<StaffAccountDto?> UpdateStaffAccountAsync(Guid userId, UpdateStaffAccountDto dto);
    Task<bool> DeleteStaffAccountAsync(Guid userId);
    Task<List<StaffAccountDto>> GetAllStaffAccountsAsync();

    // Student operations
    Task<StudentAccountDto?> GetStudentAccountAsync(Guid userId);
    Task<StudentAccountDto> CreateStudentAccountAsync(CreateStudentAccountDto dto);
    Task<StudentAccountDto?> UpdateStudentAccountAsync(Guid userId, UpdateStudentAccountDto dto);
    Task<bool> DeleteStudentAccountAsync(Guid userId);
    Task<List<StudentAccountDto>> GetAllStudentAccountsAsync();

    // User with profile operations
    Task<UserWithProfileDto?> GetUserWithProfileAsync(Guid userId);
    Task<UserWithProfileDto> CreateUserWithProfileAsync(CreateUserWithProfileDto dto);
    Task<List<UserWithProfileDto>> GetAllUsersWithProfilesAsync();

    // Profile validation
    Task<bool> ValidateProfileExclusivityAsync(Guid userId);
    Task<string> GetUserProfileTypeAsync(Guid userId);
}

public class ProfileService : IProfileService
{
    private readonly CongresoDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(CongresoDbContext context, IPasswordHasher passwordHasher, ILogger<ProfileService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    #region Staff Operations

    public async Task<StaffAccountDto?> GetStaffAccountAsync(Guid userId)
    {
        var staff = await _context.StaffAccounts
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return staff != null ? MapToStaffAccountDto(staff) : null;
    }

    public async Task<StaffAccountDto> CreateStaffAccountAsync(CreateStaffAccountDto dto)
    {
        // Verificar que el usuario existe
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
            throw new ArgumentException("Usuario no encontrado");

        // Verificar exclusividad de perfiles
        if (!await ValidateProfileExclusivityAsync(dto.UserId))
            throw new InvalidOperationException("El usuario ya tiene un perfil asignado");

        // Use Models.StaffRole directly since DTOs.StaffRole was removed
        var modelRole = dto.StaffRole;
        
        var staff = new StaffAccount
        {
            UserId = dto.UserId,
            StaffRole = modelRole,
            DisplayName = dto.DisplayName,
            ExtraData = dto.ExtraData ?? new Dictionary<string, object>()
        };

        _context.StaffAccounts.Add(staff);
        await _context.SaveChangesAsync();

        // Recargar con navegación
        await _context.Entry(staff).Reference(s => s.User).LoadAsync();
        return MapToStaffAccountDto(staff);
    }

    public async Task<StaffAccountDto?> UpdateStaffAccountAsync(Guid userId, UpdateStaffAccountDto dto)
    {
        var staff = await _context.StaffAccounts
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (staff == null) return null;

        if (dto.StaffRole.HasValue) 
        {
            staff.StaffRole = dto.StaffRole.Value;
        }
        if (dto.DisplayName != null) staff.DisplayName = dto.DisplayName;
        if (dto.ExtraData != null) staff.ExtraData = dto.ExtraData;

        await _context.SaveChangesAsync();
        return MapToStaffAccountDto(staff);
    }

    public async Task<bool> DeleteStaffAccountAsync(Guid userId)
    {
        var staff = await _context.StaffAccounts.FindAsync(userId);
        if (staff == null) return false;

        _context.StaffAccounts.Remove(staff);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<StaffAccountDto>> GetAllStaffAccountsAsync()
    {
        var staffAccounts = await _context.StaffAccounts
            .Include(s => s.User)
            .OrderBy(s => s.StaffRole)
            .ThenBy(s => s.DisplayName ?? s.User.FullName)
            .ToListAsync();

        return staffAccounts.Select(MapToStaffAccountDto).ToList();
    }

    #endregion

    #region Student Operations

    public async Task<StudentAccountDto?> GetStudentAccountAsync(Guid userId)
    {
        var student = await _context.StudentAccounts
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return student != null ? MapToStudentAccountDto(student) : null;
    }

    public async Task<StudentAccountDto> CreateStudentAccountAsync(CreateStudentAccountDto dto)
    {
        // Verificar que el usuario existe
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
            throw new ArgumentException("Usuario no encontrado");

        // Verificar exclusividad de perfiles
        if (!await ValidateProfileExclusivityAsync(dto.UserId))
            throw new InvalidOperationException("El usuario ya tiene un perfil asignado");

        var student = new StudentAccount
        {
            UserId = dto.UserId,
            Carnet = dto.Carnet,
            Career = dto.Career,
            CohortYear = dto.CohortYear,
            Organization = "UMG" // Default organization
        };

        _context.StudentAccounts.Add(student);
        await _context.SaveChangesAsync();

        // Recargar con navegación
        await _context.Entry(student).Reference(s => s.User).LoadAsync();
        return MapToStudentAccountDto(student);
    }

    public async Task<StudentAccountDto?> UpdateStudentAccountAsync(Guid userId, UpdateStudentAccountDto dto)
    {
        var student = await _context.StudentAccounts
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null) return null;

        if (dto.Carnet != null) student.Carnet = dto.Carnet;
        if (dto.Career != null) student.Career = dto.Career;
        if (dto.CohortYear.HasValue) student.CohortYear = dto.CohortYear;
        // ExtraData property removed from StudentAccount model

        await _context.SaveChangesAsync();
        return MapToStudentAccountDto(student);
    }

    public async Task<bool> DeleteStudentAccountAsync(Guid userId)
    {
        var student = await _context.StudentAccounts.FindAsync(userId);
        if (student == null) return false;

        _context.StudentAccounts.Remove(student);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<StudentAccountDto>> GetAllStudentAccountsAsync()
    {
        var studentAccounts = await _context.StudentAccounts
            .Include(s => s.User)
            .OrderBy(s => s.Career)
            .ThenBy(s => s.CohortYear)
            .ThenBy(s => s.User.FullName)
            .ToListAsync();

        return studentAccounts.Select(MapToStudentAccountDto).ToList();
    }

    #endregion

    #region User with Profile Operations

    public async Task<UserWithProfileDto?> GetUserWithProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.StaffAccount)
            .Include(u => u.StudentAccount)
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user != null ? MapToUserWithProfileDto(user) : null;
    }

    public async Task<UserWithProfileDto> CreateUserWithProfileAsync(CreateUserWithProfileDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogDebug("Starting user creation for email: {Email}", dto.Email);
            
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("El email ya está registrado");

            _logger.LogDebug("Email verification passed");

            var (orgId, orgName, isUmg) = await ResolveOrganizationByEmailAsync(dto.Email);
            _logger.LogDebug("Organization resolved: {OrgName} (ID: {OrgId}, IsUmg: {IsUmg})", orgName, orgId, isUmg);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                AvatarUrl = dto.AvatarUrl,
                OrgId = orgId,
                OrgName = orgName,
                IsUmg = isUmg,
                Status = "active"
            };
            _logger.LogDebug("User object created with ID: {UserId}", user.Id);

            _context.Users.Add(user);
            _logger.LogDebug("User added to context, attempting first SaveChanges...");
            await _context.SaveChangesAsync();
            _logger.LogDebug("First SaveChanges completed successfully");

            _logger.LogDebug("Creating profile of type: {ProfileType}", dto.ProfileType);
            if (dto.ProfileType == "staff")
            {
                if (!dto.StaffRole.HasValue)
                    throw new ArgumentException("StaffRole es requerido para perfil de staff");

                _logger.LogDebug("Creating staff account with role: {Role}", dto.StaffRole.Value);
                var staff = new StaffAccount
                {
                    UserId = user.Id,
                    StaffRole = dto.StaffRole.Value,
                    DisplayName = dto.StaffRole.Value == Models.StaffRole.AdminDev ? "Desarrollador/Super Admin" : dto.FullName,
                    ExtraData = new Dictionary<string, object>()
                };
                _context.StaffAccounts.Add(staff);
                _logger.LogDebug("Staff account added to context");
            }
            else if (dto.ProfileType == "student")
            {
                _logger.LogDebug("Creating student account");
                var student = new StudentAccount
                {
                    UserId = user.Id,
                    Carnet = dto.Carnet,
                    Career = dto.Career,
                    CohortYear = dto.CohortYear,
                    Organization = "UMG"
                };
                _context.StudentAccounts.Add(student);
                _logger.LogDebug("Student account added to context");
            }
            else
            {
                throw new ArgumentException("ProfileType debe ser 'staff' o 'student'");
            }

            _logger.LogDebug("Attempting second SaveChanges...");
            await _context.SaveChangesAsync();
            _logger.LogDebug("Second SaveChanges completed successfully");

            await transaction.CommitAsync();
            _logger.LogDebug("Committing transaction...");
            _logger.LogDebug("Transaction committed successfully");

            await _context.Entry(user).Reference(u => u.StaffAccount).LoadAsync();
            await _context.Entry(user).Reference(u => u.StudentAccount).LoadAsync();
            await _context.Entry(user).Reference(u => u.Organization).LoadAsync();

            return MapToUserWithProfileDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando usuario con perfil para email {Email}", dto.Email);
            try { await transaction.RollbackAsync(); } catch { }
            throw;
        }
    }

    public async Task<List<UserWithProfileDto>> GetAllUsersWithProfilesAsync()
    {
        var users = await _context.Users
            .Include(u => u.StaffAccount)
            .Include(u => u.StudentAccount)
            .Include(u => u.Organization)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return users.Select(MapToUserWithProfileDto).ToList();
    }

    #endregion

    #region Profile Validation

    public async Task<bool> ValidateProfileExclusivityAsync(Guid userId)
    {
        var hasStaff = await _context.StaffAccounts.AnyAsync(s => s.UserId == userId);
        var hasStudent = await _context.StudentAccounts.AnyAsync(s => s.UserId == userId);

        return !(hasStaff && hasStudent); // No debe tener ambos perfiles
    }

    public async Task<string> GetUserProfileTypeAsync(Guid userId)
    {
        var hasStaff = await _context.StaffAccounts.AnyAsync(s => s.UserId == userId);
        if (hasStaff) return "staff";

        var hasStudent = await _context.StudentAccounts.AnyAsync(s => s.UserId == userId);
        if (hasStudent) return "student";

        return "none";
    }

    #endregion

    #region Helper Methods

    private async Task<(Guid? orgId, string? orgName, bool isUmg)> ResolveOrganizationByEmailAsync(string email)
    {
        var domain = email.Split('@').LastOrDefault()?.ToLower();
        if (string.IsNullOrEmpty(domain))
            return (null, "EXTERNO", false);

        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Domain != null && o.Domain.ToLower() == domain);

        if (org != null)
            return (org.Id, org.Name, org.Name.Contains("UMG", StringComparison.OrdinalIgnoreCase));

        return (null, "EXTERNO", false);
    }

    private static StaffAccountDto MapToStaffAccountDto(StaffAccount staff)
    {
        // Use Models.StaffRole directly since DTOs.StaffRole was removed
        var dtoRole = staff.StaffRole;
        
        return new StaffAccountDto
        {
            UserId = staff.UserId,
            StaffRole = dtoRole,
            DisplayName = staff.DisplayName,
            ExtraData = staff.ExtraData,
            RoleDescription = staff.RoleDescription,
            IsAdmin = staff.IsAdmin,
            IsSuperAdmin = staff.IsSuperAdmin,
            User = staff.User != null ? new UserBasicDto
            {
                Id = staff.User.Id,
                Email = staff.User.Email,
                FullName = staff.User.FullName ?? string.Empty,
                AvatarUrl = staff.User.AvatarUrl,
                Status = staff.User.Status ?? "active",
                IsUmg = staff.User.IsUmg,
                OrgName = staff.User.OrgName,
                IsActive = staff.User.IsActive,
                CreatedAt = staff.User.CreatedAt,
                LastLoginAt = staff.User.LastLogin
            } : null
        };
    }

    private static StudentAccountDto MapToStudentAccountDto(StudentAccount student)
    {
        return new StudentAccountDto
        {
            UserId = student.UserId,
            Carnet = student.Carnet,
            Career = student.Career,
            CohortYear = student.CohortYear,
            ExtraData = new Dictionary<string, object>(), // ExtraData property removed from model
            IsUmgStudent = student.IsUmgStudent,
            CurrentAcademicYear = student.CurrentAcademicYear,
            User = student.User != null ? new UserBasicDto
            {
                Id = student.User.Id,
                Email = student.User.Email,
                FullName = student.User.FullName ?? string.Empty,
                AvatarUrl = student.User.AvatarUrl,
                Status = student.User.Status ?? "active",
                IsUmg = student.User.IsUmg,
                OrgName = student.User.OrgName,
                IsActive = student.User.IsActive,
                CreatedAt = student.User.CreatedAt,
                LastLoginAt = student.User.LastLogin
            } : null
        };
    }

    private static UserWithProfileDto MapToUserWithProfileDto(User user)
    {
        var staffRole = user.GetStaffRole();
        
        var dto = new UserWithProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName ?? string.Empty,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status ?? "active",
                IsUmg = user.IsUmg,
                OrgName = user.OrgName,
                ProfileType = user.GetProfileType(),
                DisplayName = user.GetDisplayName(),
                EffectiveAvatarUrl = user.GetAvatarUrl(),
                HasAdminPermissions = user.HasAdminPermissions(),
                StaffRole = staffRole
            };

        if (user.StaffAccount != null)
        {
            dto.StaffProfile = MapToStaffAccountDto(user.StaffAccount);
        }

        if (user.StudentAccount != null)
        {
            dto.StudentProfile = MapToStudentAccountDto(user.StudentAccount);
        }

        return dto;
    }

    #endregion
}

/// <summary>
/// Interface para hash de passwords (debe implementarse)
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Implementación básica de IPasswordHasher usando BCrypt
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}