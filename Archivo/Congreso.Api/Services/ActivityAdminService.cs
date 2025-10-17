using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActivityType = Congreso.Api.DTOs.ActivityType;

namespace Congreso.Api.Services;

public class ActivityAdminService : IActivityAdminService
{
    private readonly CongresoDbContext _context;
    private readonly IStaffService _staffService;

    public ActivityAdminService(CongresoDbContext context, IStaffService staffService)
    {
        _context = context;
        _staffService = staffService;
    }

    public async Task<List<AdminActivityDto>> GetAdminActivitiesAsync()
    {
        try
        {
            var activities = await _context.Activities
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return activities.Select(MapToAdminActivityDto).ToList();
        }
        catch
        {
            return new List<AdminActivityDto>();
        }
    }

    public async Task<AdminActivityDto?> GetAdminActivityByIdAsync(Guid activityId)
    {
        try
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId);

            return activity != null ? MapToAdminActivityDto(activity) : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdminActivityDto> CreateAdminActivityAsync(CreateAdminActivityDto dto)
    {
        try
        {
            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                ActivityType = (Models.ActivityType)Enum.Parse<ActivityType>(dto.Type),
                Location = dto.Location,
                StartTime = dto.ScheduledAt,
                EndTime = dto.ScheduledAt.AddHours(1), // Default 1 hour duration
                Capacity = dto.MaxParticipants,
                Published = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            return MapToAdminActivityDto(activity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating activity: {ex.Message}");
        }
    }

    public async Task<AdminActivityDto> UpdateAdminActivityAsync(Guid id, UpdateAdminActivityDto dto)
    {
        try
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return null;

            if (!string.IsNullOrEmpty(dto.Title))
                activity.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.Description))
                activity.Description = dto.Description;
            if (!string.IsNullOrEmpty(dto.Type))
                activity.ActivityType = (Models.ActivityType)Enum.Parse<ActivityType>(dto.Type);
            if (dto.Location != null)
                activity.Location = dto.Location;
            if (dto.ScheduledAt.HasValue)
            {
                activity.StartTime = dto.ScheduledAt.Value;
                activity.EndTime = dto.ScheduledAt.Value.AddHours(1); // Default 1 hour duration
            }
            if (dto.MaxParticipants.HasValue)
                activity.Capacity = dto.MaxParticipants.Value;
            if (dto.Status != null)
                activity.Published = dto.Status.ToLower() == "published";

            activity.UpdatedAt = DateTime.UtcNow;
            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();

            return MapToAdminActivityDto(activity);
        }
        catch
        {
            throw new InvalidOperationException("Error updating activity");
        }
    }

    public async Task<bool> DeleteAdminActivityAsync(Guid activityId)
    {
        try
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
                return false;

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AdminActivityDto> PublishAdminActivityAsync(Guid activityId)
    {
        try
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
                return null;

            activity.Published = true;
            activity.UpdatedAt = DateTime.UtcNow;

            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();

            return MapToAdminActivityDto(activity);
        }
        catch
        {
            throw new InvalidOperationException("Error publishing activity");
        }
    }

    public async Task<List<AdminActivityDto>> SearchActivitiesAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<AdminActivityDto>();

            var activities = await _context.Activities
                .Where(a => 
                    a.Title.ToLower().Contains(searchTerm.ToLower()) ||
                    (a.Description != null && a.Description.ToLower().Contains(searchTerm.ToLower())) ||
                    a.ActivityType.ToString().ToLower().Contains(searchTerm.ToLower())
                )
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            return activities.Select(MapToAdminActivityDto).ToList();
        }
        catch
        {
            return new List<AdminActivityDto>();
        }
    }

    public async Task<List<AdminActivityDto>> GetActivitiesByTypeAsync(ActivityType type)
    {
        try
        {
            var activities = await _context.Activities
                .Where(a => a.ActivityType == (Models.ActivityType)type)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            return activities.Select(MapToAdminActivityDto).ToList();
        }
        catch
        {
            return new List<AdminActivityDto>();
        }
    }

    public async Task<ActivityStatsDto> GetActivityStatsAsync()
    {
        var activities = await _context.Activities
            .Where(a => a.IsActive)
            .ToListAsync();

        var totalActivities = activities.Count;
        var publishedActivities = activities.Count(a => a.Published);
        var draftActivities = activities.Count(a => !a.Published);
        var totalCapacity = activities.Sum(a => a.Capacity ?? 0);
        var totalEnrollments = await _context.Enrollments
            .CountAsync();

        var averageCapacity = totalActivities > 0 ? (double)totalCapacity / totalActivities : 0;
        var fillRate = totalCapacity > 0 ? (double)totalEnrollments / totalCapacity * 100 : 0;

        return new ActivityStatsDto
        {
            TotalActivities = totalActivities,
            PublishedActivities = publishedActivities,
            DraftActivities = draftActivities,
            TotalCapacity = totalCapacity,
            TotalEnrollments = totalEnrollments,
            AverageCapacity = averageCapacity,
            FillRate = fillRate
        };
    }

    public async Task<List<AdminActivityDto>> GetActivitiesWithAvailableSlotsAsync()
    {
        try
        {
            var activities = await _context.Activities
                .Where(a => a.Published && a.Capacity > 0)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var result = new List<AdminActivityDto>();
            foreach (var activity in activities)
            {
                var enrolledCount = await _context.Enrollments
                    .CountAsync(e => e.ActivityId == activity.Id && e.Attended);

                var availableSlots = (activity.Capacity ?? 0) - enrolledCount;
                if (availableSlots > 0)
                {
                    var dto = MapToAdminActivityDto(activity);
                    result.Add(dto);
                }
            }

            return result;
        }
        catch
        {
            return new List<AdminActivityDto>();
        }
    }

    public async Task<ActivityStatsDto> GetActivityStatsAsync(Guid activityId)
    {
        try
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
                return new ActivityStatsDto();

            var enrolledCount = await _context.Enrollments
                .CountAsync(e => e.ActivityId == activityId);

            var attendanceCount = await _context.CheckInTokens
                .CountAsync(t => t.ActivityId == activityId && t.Used);

            var availableSlots = (activity.Capacity ?? 0) - enrolledCount;

            return new ActivityStatsDto
            {
                TotalActivities = 1,
                PublishedActivities = activity.Published ? 1 : 0,
                DraftActivities = activity.Published ? 0 : 1,
                TotalCapacity = activity.Capacity ?? 0,
                TotalEnrollments = enrolledCount,
                AverageCapacity = activity.Capacity ?? 0,
                FillRate = (activity.Capacity ?? 0) > 0 ? (double)enrolledCount / (activity.Capacity ?? 1) * 100 : 0
            };
        }
        catch
        {
            return new ActivityStatsDto();
        }
    }

    public async Task<bool> UpdateAdminActivityStatusAsync(Guid activityId, string status)
    {
        try
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
                return false;

            // Map status string to published boolean
            activity.Published = status.ToLower() == "published";
            activity.UpdatedAt = DateTime.UtcNow;

            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AdminActivityStatsDto> GetAdminActivityStatsAsync()
    {
        try
        {
            var totalActivities = await _context.Activities.CountAsync();
            var publishedActivities = await _context.Activities.CountAsync(a => a.Published);
            var upcomingActivities = await _context.Activities
                .CountAsync(a => a.StartTime > DateTime.UtcNow);
            var draftActivities = await _context.Activities.CountAsync(a => !a.Published);
            var pastActivities = await _context.Activities.CountAsync(a => a.EndTime < DateTime.UtcNow);

            return new AdminActivityStatsDto
            {
                TotalActivities = totalActivities,
                PublishedActivities = publishedActivities,
                UpcomingActivities = upcomingActivities,
                DraftActivities = draftActivities,
                PastActivities = pastActivities
            };
        }
        catch
        {
            return new AdminActivityStatsDto();
        }
    }

    public async Task<List<AdminActivityDto>> GetRecentAdminActivitiesAsync(int count = 10)
    {
        try
        {
            var activities = await _context.Activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();

            return activities.Select(MapToAdminActivityDto).ToList();
        }
        catch
        {
            return new List<AdminActivityDto>();
        }
    }

    public async Task<List<AdminActivityDto>> GetAdminActivitiesByUserAsync(Guid userId)
    {
        try
        {
            // This would typically join with registrations, but for now return empty list
            return new List<AdminActivityDto>();
        }
        catch
        {
            return new List<AdminActivityDto>();
        }
    }

    public async Task<bool> CanManageActivitiesAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            return user.UserRoles.Any(ur => ur.Role.Name == "admin" || ur.Role.Name == "organizer");
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

    private AdminActivityDto MapToAdminActivityDto(Activity activity)
    {
        return new AdminActivityDto
        {
            Id = activity.Id.GetHashCode(), // Convert Guid to int
            Title = activity.Title,
            Description = activity.Description,
            Type = ((ActivityType)activity.ActivityType).ToString(),
            Location = activity.Location ?? "",
            ScheduledAt = activity.StartTime ?? DateTime.MinValue,
            MaxParticipants = activity.Capacity ?? 0,
            RequiresRegistration = activity.Capacity > 0,
            Status = activity.Published ? "published" : "draft",
            CreatedBy = 0, // No CreatedBy field in Activity model
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt
        };
    }

    #endregion
}