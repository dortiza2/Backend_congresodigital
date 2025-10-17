using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Congreso.Api.DTOs;
using StaffRole = Congreso.Api.Models.StaffRole;

namespace Congreso.Api.Services;

/// <summary>
/// Interface para servicio de gestión de staff
/// </summary>
public interface IStaffService
{
    /// <summary>
    /// Invitar nuevo miembro del staff
    /// </summary>
    Task<StaffInviteResponseDto> InviteStaffAsync(StaffInviteRequest inviteRequest, Guid invitedByUserId);

    /// <summary>
    /// Obtener todas las invitaciones de staff
    /// </summary>
    Task<List<StaffInviteResponseDto>> GetStaffInvitationsAsync();

    /// <summary>
    /// Obtener invitación por email
    /// </summary>
    Task<StaffInviteResponseDto?> GetInvitationByEmailAsync(string email);

    /// <summary>
    /// Revocar invitación
    /// </summary>
    Task<bool> RevokeInvitationAsync(Guid invitationId);

    /// <summary>
    /// Verificar si usuario puede invitar staff
    /// </summary>
    Task<bool> CanInviteStaffAsync(Guid userId);

    /// <summary>
    /// Obtener todos los miembros del staff
    /// </summary>
    Task<List<StaffDto>> GetAllStaffAsync();

    /// <summary>
    /// Buscar miembros del staff
    /// </summary>
    Task<List<StaffDto>> SearchStaffAsync(string searchTerm);

    /// <summary>
    /// Obtener staff por rol
    /// </summary>
    Task<List<StaffDto>> GetStaffByRoleAsync(StaffRole role);

    /// <summary>
    /// Verificar si usuario es admin o superadmin
    /// </summary>
    Task<bool> IsAdminAsync(Guid userId);

    /// <summary>
    /// Verificar si usuario es superadmin
    /// </summary>
    Task<bool> IsSuperAdminAsync(Guid userId);

    /// <summary>
    /// Actualizar rol del staff
    /// </summary>
    Task<bool> UpdateStaffRoleAsync(Guid staffId, int roleId, Guid updatedByUserId);

    /// <summary>
    /// Actualizar estado del staff
    /// </summary>
    Task<bool> UpdateStaffStatusAsync(Guid staffId, bool isActive, Guid updatedByUserId);
}