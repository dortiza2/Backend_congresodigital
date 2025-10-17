using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StaffRole = Congreso.Api.Models.StaffRole;

namespace Congreso.Api.Services;

public class StaffService : IStaffService
{
    private readonly CongresoDbContext _context;
    private readonly IUserService _userService;
    private readonly IProfileService _profileService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<StaffService> _logger;

    public StaffService(
        CongresoDbContext context, 
        IUserService userService, 
        IProfileService profileService,
        IPasswordHasher passwordHasher,
        ILogger<StaffService> logger)
    {
        _context = context;
        _userService = userService;
        _profileService = profileService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<StaffInviteResponseDto> InviteStaffAsync(StaffInviteRequest inviteRequest, Guid invitedByUserId)
    {
        try
        {
            // Verificar permisos - solo admins pueden invitar staff
            var canInvite = await CanInviteStaffAsync(invitedByUserId);
            if (!canInvite)
            {
                throw new UnauthorizedAccessException("No tiene permisos para invitar al staff");
            }

            // Verificar que el rol existe
            var role = await _context.Roles.FindAsync(inviteRequest.RoleId);
            if (role == null)
            {
                throw new ArgumentException("El rol especificado no existe");
            }

            // Verificar si el usuario ya existe
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == inviteRequest.Email);

            if (existingUser != null)
            {
                // Verificar si ya es staff
                var existingStaff = await _context.StaffAccounts
                    .FirstOrDefaultAsync(sa => sa.UserId == existingUser.Id);

                if (existingStaff != null)
                {
                    throw new InvalidOperationException("El usuario ya es miembro del staff");
                }

                // Crear cuenta de staff para usuario existente
                    var staffAccount = new StaffAccount
                    {
                        UserId = existingUser.Id,
                        StaffRole = (StaffRole)inviteRequest.RoleId,
                        DisplayName = inviteRequest.FullName
                    };

                _context.StaffAccounts.Add(staffAccount);
                await _context.SaveChangesAsync();

                // Asignar rol al usuario
                var userRole = new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = inviteRequest.RoleId,
                    AssignedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                return new StaffInviteResponseDto
                {
                    UserId = existingUser.Id,
                    Email = existingUser.Email,
                    Role = (StaffRole)inviteRequest.RoleId,
                    Status = "existing",
                    InvitationToken = string.Empty, // No token for existing users
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Crear nuevo usuario y cuenta de staff
            var user = new User
            {
                Email = inviteRequest.Email,
                FullName = inviteRequest.FullName,
                IsActive = false, // Se activará cuando acepte la invitación
                Status = "invited",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var staffAccountNew = new StaffAccount
                {
                    UserId = user.Id,
                    StaffRole = (StaffRole)inviteRequest.RoleId,
                    DisplayName = inviteRequest.FullName
                };

            _context.StaffAccounts.Add(staffAccountNew);
            await _context.SaveChangesAsync();

            // Generar token de invitación
            var invitationToken = Guid.NewGuid().ToString();
            var invitation = new UserInvitation
            {
                UserId = user.Id,
                Token = invitationToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Asignar rol al usuario
            var newUserRole = new UserRole
            {
                UserId = user.Id,
                RoleId = inviteRequest.RoleId,
                AssignedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(newUserRole);
            await _context.SaveChangesAsync();

            // TODO: Enviar email de invitación

            return new StaffInviteResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = (StaffRole)inviteRequest.RoleId,
                Status = "invited",
                InvitationToken = invitationToken,
                CreatedAt = invitation.CreatedAt
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al invitar al staff");
            throw new InvalidOperationException("Error al procesar la invitación del staff", ex);
        }
    }

    public async Task<List<StaffInviteResponseDto>> GetStaffInvitationsAsync()
    {
        try
        {
            // For now, return empty list as we don't have invitation tracking
            // In a real implementation, this would query an invitations table
            return new List<StaffInviteResponseDto>();
        }
        catch
        {
            return new List<StaffInviteResponseDto>();
        }
    }

    public async Task<StaffInviteResponseDto?> GetInvitationByEmailAsync(string email)
    {
        try
        {
            // For now, return null as we don't have invitation tracking
            // In a real implementation, this would query an invitations table
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RevokeInvitationAsync(Guid invitationId)
    {
        try
        {
            // For now, return false as we don't have invitation tracking
            // In a real implementation, this would update an invitations table
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CanInviteStaffAsync(Guid userId)
    {
        try
        {
            return await IsAdminAsync(userId) || await IsSuperAdminAsync(userId);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<StaffDto>> GetAllStaffAsync()
    {
        try
        {
            var staffAccounts = await _profileService.GetAllStaffAccountsAsync();
            return staffAccounts.Select(MapStaffAccountToStaffDto).ToList();
        }
        catch
        {
            return new List<StaffDto>();
        }
    }

    public async Task<List<StaffDto>> SearchStaffAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<StaffDto>();

            var allStaff = await _profileService.GetAllStaffAccountsAsync();
            var searchLower = searchTerm.ToLower();

            var filteredStaff = allStaff.Where(s => 
                s.User?.FullName.ToLower().Contains(searchLower) == true ||
                s.User?.Email.ToLower().Contains(searchLower) == true ||
                s.DisplayName?.ToLower().Contains(searchLower) == true ||
                s.RoleDescription.ToLower().Contains(searchLower)
            ).ToList();

            return filteredStaff.Select(MapStaffAccountToStaffDto).ToList();
        }
        catch
        {
            return new List<StaffDto>();
        }
    }

    public async Task<List<StaffDto>> GetStaffByRoleAsync(StaffRole role)
        {
            var roleName = role.ToString();
            var staffUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName) && u.IsActive)
                .ToListAsync();

            var staffDtos = new List<StaffDto>();
            
            foreach (var user in staffUsers)
            {
                var staffAccount = await _context.StaffAccounts
                    .Include(sa => sa.User)
                    .FirstOrDefaultAsync(sa => sa.UserId == user.Id);

                if (staffAccount != null)
                {
                    staffDtos.Add(new StaffDto
                    {
                        Id = user.Id.GetHashCode(), // Convert Guid to int for UserId
                        Email = user.Email,
                        FullName = user.FullName ?? string.Empty,
                        Role = staffAccount.StaffRole.ToString(),
                        RoleId = (int)staffAccount.StaffRole,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLogin
                    });
                }
            }

            return staffDtos;
        }

    public async Task<bool> IsAdminAsync(Guid userId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user?.Roles.Contains("ADMIN") == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsSuperAdminAsync(Guid userId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user?.Roles.Contains("DVADMIN") == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateStaffRoleAsync(Guid userId, int roleId, Guid updatedByUserId)
    {
        try
        {
            // Verify updater has permission
            if (!await IsAdminAsync(updatedByUserId) && !await IsSuperAdminAsync(updatedByUserId))
            {
                throw new UnauthorizedAccessException("insufficient_permissions");
            }

            // Get the user
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Get staff account
            var staffAccount = await _context.StaffAccounts
                .FirstOrDefaultAsync(sa => sa.UserId == userId);
            
            if (staffAccount == null)
            {
                return false;
            }

            // Convert roleId to StaffRole enum
            var newRole = (StaffRole)roleId;

            // Update staff role
            staffAccount.StaffRole = newRole;
            staffAccount.UpdatedAt = DateTime.UtcNow;

            // Update user roles in UserRoles table
            var roleName = GetRoleNameFromStaffRole(newRole);
            
            // Remove existing staff roles
            var existingStaffRoles = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && 
                            (ur.Role.Name == "ADMIN" || ur.Role.Name == "DVADMIN" || 
                             ur.Role.Name == "ASISTENTE" || ur.Role.Name == "MODERADOR"))
                .ToListAsync();

            _context.UserRoles.RemoveRange(existingStaffRoles);

            // Add new role
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateStaffStatusAsync(Guid userId, bool isActive, Guid updatedByUserId)
    {
        try
        {
            // Verify updater has permission
            if (!await IsAdminAsync(updatedByUserId) && !await IsSuperAdminAsync(updatedByUserId))
            {
                throw new UnauthorizedAccessException("insufficient_permissions");
            }

            // Get the user
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Get staff account
            var staffAccount = await _context.StaffAccounts
                .FirstOrDefaultAsync(sa => sa.UserId == userId);
            
            if (staffAccount == null)
            {
                return false;
            }

            // Update user status
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            staffAccount.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

    private string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string GenerateInvitationToken()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }

    private string GetRoleNameFromStaffRole(Models.StaffRole role)
    {
        return role switch
        {
            Models.StaffRole.Admin => "ADMIN",
            Models.StaffRole.AdminDev => "DVADMIN",
            Models.StaffRole.Asistente => "ASISTENTE",
            _ => "ASISTENTE"
        };
    }

    private StaffDto MapStaffAccountToStaffDto(StaffAccountDto staffAccount)
    {
        return new StaffDto
        {
            Id = staffAccount.UserId.GetHashCode(), // Convert Guid to int for UserId
            Email = staffAccount.User?.Email ?? string.Empty,
            FullName = staffAccount.User?.FullName ?? staffAccount.DisplayName ?? string.Empty,
            Role = staffAccount.StaffRole.ToString(),
            RoleId = (int)staffAccount.StaffRole,
            IsActive = staffAccount.User?.IsActive ?? false,
            CreatedAt = staffAccount.User?.CreatedAt ?? DateTime.UtcNow,
            LastLoginAt = staffAccount.User?.LastLoginAt
        };
    }

    #endregion
}