using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Congreso.Api.Services;

public class UserService : IUserService
{
    private readonly CongresoDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(CongresoDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResponseDto<UserDto>> GetUsersAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var query = _context.Users.AsQueryable();
            var totalCount = await query.CountAsync();
            
            var users = await query
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userRoles = await GetUserRolesAsync(user.Id);
                var profileType = await GetUserProfileTypeAsync(user.Id);
                userDtos.Add(MapToUserDto(user, userRoles, profileType));
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResponseDto<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }
        catch (Exception ex)
        {
            // Return empty result instead of throwing
            return new PagedResponseDto<UserDto>
            {
                Items = new List<UserDto>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
                TotalPages = 0,
                HasNextPage = false,
                HasPreviousPage = false
            };
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var userRoles = await GetUserRolesAsync(user.Id);
            var profileType = await GetUserProfileTypeAsync(user.Id);
            return MapToUserDto(user, userRoles, profileType);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null) return null;

            var userRoles = await GetUserRolesAsync(user.Id);
            var profileType = await GetUserProfileTypeAsync(user.Id);
            return MapToUserDto(user, userRoles, profileType);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        try
        {
            // Check if email already exists
            if (await EmailExistsAsync(dto.Email))
            {
                throw new InvalidOperationException("email_already_exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email.ToLower(),
                FullName = dto.FullName,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                IsUmg = dto.IsUmg,
                OrgName = dto.OrgName,
                AvatarUrl = dto.AvatarUrl,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            
            // Add roles if specified
            if (dto.Roles?.Any() == true)
            {
                foreach (var role in dto.Roles)
                {
                    var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
                    if (roleEntity != null)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = roleEntity.Id
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            var userRoles = await GetUserRolesAsync(user.Id);
            var profileType = await GetUserProfileTypeAsync(user.Id);
            return MapToUserDto(user, userRoles, profileType);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating user: {ex.Message}");
        }
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(dto.FullName))
                user.FullName = dto.FullName;

            if (dto.AvatarUrl != null)
                user.AvatarUrl = dto.AvatarUrl;

            if (!string.IsNullOrEmpty(dto.Status))
                user.Status = dto.Status;

            user.UpdatedAt = DateTime.UtcNow;

            // Update roles if specified
            if (dto.Roles != null)
            {
                // Remove existing roles
                var existingRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
                _context.UserRoles.RemoveRange(existingRoles);

                // Add new roles
                foreach (var role in dto.Roles)
                {
                    var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
                    if (roleEntity != null)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = roleEntity.Id
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            var userRoles = await GetUserRolesAsync(user.Id);
            var profileType = await GetUserProfileTypeAsync(user.Id);
            return MapToUserDto(user, userRoles, profileType);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Soft delete - mark as inactive
            user.Status = "inactive";
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserDto?> UpdateUserRolesAsync(Guid userId, string[] roles)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            // Remove existing roles
            var existingRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _context.UserRoles.RemoveRange(existingRoles);

            // Add new roles
            foreach (var role in roles)
            {
                var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
                if (roleEntity != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleEntity.Id
                    });
                }
            }

            await _context.SaveChangesAsync();

            var userRoles = await GetUserRolesAsync(user.Id);
            var profileType = await GetUserProfileTypeAsync(user.Id);
            return MapToUserDto(user, userRoles, profileType);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<UserDto>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<UserDto>();

            var searchLower = searchTerm.ToLower();
            var users = await _context.Users
                .Where(u => u.FullName.ToLower().Contains(searchLower) || 
                           u.Email.ToLower().Contains(searchLower))
                .Take(50)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userRoles = await GetUserRolesAsync(user.Id);
                var profileType = await GetUserProfileTypeAsync(user.Id);
                userDtos.Add(MapToUserDto(user, userRoles, profileType));
            }

            return userDtos;
        }
        catch
        {
            return new List<UserDto>();
        }
    }

    public async Task<List<UserDto>> GetUsersByRoleAsync(string role)
    {
        try
        {
            var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
            if (roleEntity == null)
                return new List<UserDto>();

            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == roleEntity.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userRoles = await GetUserRolesAsync(user.Id);
                var profileType = await GetUserProfileTypeAsync(user.Id);
                userDtos.Add(MapToUserDto(user, userRoles, profileType));
            }

            return userDtos;
        }
        catch
        {
            return new List<UserDto>();
        }
    }

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        try
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<UserEnrollmentView>> GetUserEnrollmentsAsync(Guid userId)
    {
        try
        {
            return await _context.UserEnrollments
                .Where(ue => ue.UserId == userId)
                .OrderByDescending(ue => ue.EnrollmentDate)
                .ToListAsync();
        }
        catch
        {
            return new List<UserEnrollmentView>();
        }
    }

    #region Private Methods

    private async Task<string[]> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToArrayAsync();

            return roles;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private async Task<string> GetUserProfileTypeAsync(Guid userId)
    {
        try
        {
            var hasStaffProfile = await _context.StaffAccounts.AnyAsync(sa => sa.UserId == userId);
            if (hasStaffProfile) return "staff";

            var hasStudentProfile = await _context.StudentAccounts.AnyAsync(sa => sa.UserId == userId);
            if (hasStudentProfile) return "student";

            return "none";
        }
        catch
        {
            return "none";
        }
    }

    private UserDto MapToUserDto(User user, string[] roles, string profileType)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName ?? string.Empty,
            Status = user.Status ?? "active",
            IsUmg = user.IsUmg,
            OrgName = user.OrgName,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsActive = user.IsActive,
            Roles = roles,
            ProfileType = profileType
        };
    }

    #endregion
}