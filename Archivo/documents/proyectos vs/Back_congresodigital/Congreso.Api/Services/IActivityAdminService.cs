using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Congreso.Api.DTOs;

namespace Congreso.Api.Services;

/// <summary>
/// Interface para servicio de administración de actividades
/// </summary>
public interface IActivityAdminService
{
    /// <summary>
    /// Obtener todas las actividades para administración
    /// </summary>
    Task<List<AdminActivityDto>> GetAdminActivitiesAsync();

    /// <summary>
    /// Obtener actividad por ID para administración
    /// </summary>
    Task<AdminActivityDto?> GetAdminActivityByIdAsync(Guid activityId);

    /// <summary>
    /// Crear nueva actividad para administración
    /// </summary>
    Task<AdminActivityDto> CreateAdminActivityAsync(CreateAdminActivityDto dto);

    /// <summary>
    /// Actualizar actividad para administración
    /// </summary>
    Task<AdminActivityDto> UpdateAdminActivityAsync(Guid activityId, UpdateAdminActivityDto dto);

    /// <summary>
    /// Eliminar actividad para administración
    /// </summary>
    Task<bool> DeleteAdminActivityAsync(Guid activityId);

    /// <summary>
    /// Actualizar estado de actividad
    /// </summary>
    Task<bool> UpdateAdminActivityStatusAsync(Guid activityId, string status);

    /// <summary>
    /// Publicar actividad
    /// </summary>
    Task<AdminActivityDto> PublishAdminActivityAsync(Guid activityId);

    /// <summary>
    /// Obtener actividades por tipo
    /// </summary>
    Task<List<AdminActivityDto>> GetActivitiesByTypeAsync(ActivityType type);

    /// <summary>
    /// Obtener actividades con cupos disponibles
    /// </summary>
    Task<List<AdminActivityDto>> GetActivitiesWithAvailableSlotsAsync();

    /// <summary>
    /// Obtener estadísticas de actividades administrativas
    /// </summary>
    Task<AdminActivityStatsDto> GetAdminActivityStatsAsync();

    /// <summary>
    /// Obtener actividades recientes
    /// </summary>
    Task<List<AdminActivityDto>> GetRecentAdminActivitiesAsync(int count = 10);

    /// <summary>
    /// Obtener actividades por usuario
    /// </summary>
    Task<List<AdminActivityDto>> GetAdminActivitiesByUserAsync(Guid userId);

    /// <summary>
    /// Verificar si usuario puede administrar actividades
    /// </summary>
    Task<bool> CanManageActivitiesAsync(Guid userId);
}