using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Congreso.Api.DTOs;
using Congreso.Api.Models;

namespace Congreso.Api.Services;

/// <summary>
/// Interface para servicio de gestión de usuarios
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Obtener todos los usuarios con paginación
    /// </summary>
    Task<PagedResponseDto<UserDto>> GetUsersAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// Obtener usuario por ID
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Obtener usuario por email
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Crear nuevo usuario
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserDto dto);

    /// <summary>
    /// Actualizar usuario existente
    /// </summary>
    Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserDto dto);

    /// <summary>
    /// Eliminar usuario (lógicamente)
    /// </summary>
    Task<bool> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Actualizar roles de usuario
    /// </summary>
    Task<UserDto?> UpdateUserRolesAsync(Guid userId, string[] roles);

    /// <summary>
    /// Buscar usuarios por término
    /// </summary>
    Task<List<UserDto>> SearchUsersAsync(string searchTerm);

    /// <summary>
    /// Obtener usuarios por rol
    /// </summary>
    Task<List<UserDto>> GetUsersByRoleAsync(string role);

    /// <summary>
    /// Verificar si usuario existe
    /// </summary>
    Task<bool> UserExistsAsync(Guid userId);

    /// <summary>
    /// Verificar si email existe
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Obtener inscripciones de un usuario
    /// </summary>
    Task<List<UserEnrollmentView>> GetUserEnrollmentsAsync(Guid userId);
}