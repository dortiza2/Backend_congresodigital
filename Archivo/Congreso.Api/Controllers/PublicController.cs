using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Services;
using Congreso.Api.DTOs;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Congreso.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class PublicController : ControllerBase
    {
        private readonly CongresoDbContext _context;
        private readonly IMetricsCollector _metrics;
        private readonly Npgsql.NpgsqlDataSource _ds;
        private readonly ILogger<PublicController> _logger;

        // Lightweight DTOs for raw SQL projections used in metrics
        private class TypeCountDto
        {
            public string Type { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        private class UpcomingActivityDto
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime? StartTime { get; set; }
            public string ActivityType { get; set; } = string.Empty;
        }

        private class RecentEnrollmentDto
        {
            public Guid Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string ActivityTitle { get; set; } = string.Empty;
        }

        public PublicController(CongresoDbContext context, IMetricsCollector metrics, Npgsql.NpgsqlDataSource ds, ILogger<PublicController> logger)
        {
            _context = context;
            _metrics = metrics;
            _ds = ds;
            _logger = logger;
        }

        // Evitar conflicto con PublicActivitiesController que ya maneja /api/activities
        [HttpGet("activities-view")]
        public async Task<ActionResult> GetActivities(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? type = null)
        {
            try
            {
                var query = _context.PublicActivities.AsQueryable();

                if (from.HasValue)
                    query = query.Where(a => a.StartTime >= from.Value);

                if (to.HasValue)
                    query = query.Where(a => a.EndTime <= to.Value);

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(a => a.ActivityType == type);

                var activities = await query
                    .OrderBy(a => a.StartTime)
                    .ToListAsync();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                // Fallback si la vista vw_public_activities no existe o falla
                _logger.LogWarning(ex, "Fallo consulta de vw_public_activities, usando SELECT directo");
                var items = new List<object>();
                var sql = @"SELECT id, title, description, location,
                               COALESCE(starts_at, start_time) AS starts_at,
                               COALESCE(ends_at, end_time)     AS ends_at,
                               COALESCE(activity_type, type)   AS activity_type,
                               capacity, published
                            FROM activities";
                await using var cmd = _ds.CreateCommand(sql);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    items.Add(new
                    {
                        id = rd.GetValue(0)?.ToString(),
                        title = rd.IsDBNull(1) ? null : rd.GetString(1),
                        description = rd.IsDBNull(2) ? null : rd.GetString(2),
                        location = rd.IsDBNull(3) ? null : rd.GetString(3),
                        start_time = rd.IsDBNull(4) ? (DateTime?)null : rd.GetDateTime(4),
                        end_time = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
                        activity_type = rd.IsDBNull(6) ? null : rd.GetString(6),
                        capacity = rd.IsDBNull(7) ? 0 : rd.GetInt32(7),
                        published = rd.IsDBNull(8) ? false : rd.GetBoolean(8),
                        enrolled_count = 0,
                        available_spots = rd.IsDBNull(7) ? 0 : rd.GetInt32(7)
                    });
                }
                return Ok(items);
            }
        }

        [HttpGet("faq")]
        public async Task<ActionResult<IEnumerable<FaqItem>>> GetFaq()
        {
            var faqItems = await _context.FaqItems
                .Where(f => f.Published)
                .OrderBy(f => f.Position)
                .ToListAsync();

            return Ok(faqItems);
        }

        [HttpGet("podium-view")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> GetPodium([FromQuery] int year)
        {
            if (year <= 0)
                return BadRequest("El año debe ser un valor válido.");

            const string sql = @"SELECT 
                                    id, 
                                    year, 
                                    COALESCE(place, position) AS place, 
                                    activity_id, 
                                    activity_title, 
                                    user_id, 
                                    COALESCE(winner_name, user_full_name) AS winner_name, 
                                    award_date, 
                                    team_id, 
                                    COALESCE(prize_description, prize) AS prize_description
                                FROM public.vw_podium_by_year
                                WHERE year=@year
                                ORDER BY COALESCE(place, position) ASC, id ASC;";
            try
            {
                var items = new List<PodiumDto>();
                await using var cmd = _ds.CreateCommand(sql);
                cmd.Parameters.AddWithValue("@year", year);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    System.Guid? activityId = null;
                    try
                    {
                        if (!rd.IsDBNull(3))
                        {
                            var typeName = rd.GetDataTypeName(3).ToLowerInvariant();
                            if (typeName == "uuid") activityId = rd.GetGuid(3);
                            else if (typeName == "text") activityId = System.Guid.Parse(rd.GetString(3));
                            else
                            {
                                // Fallback: intentar convertir cualquier tipo a Guid
                                var val = rd.GetValue(3)?.ToString();
                                if (!string.IsNullOrWhiteSpace(val)) activityId = System.Guid.Parse(val);
                            }
                        }
                    }
                    catch { activityId = null; }

                    items.Add(new PodiumDto(
                        Id: rd.IsDBNull(0) ? 0 : rd.GetInt32(0),
                        Year: rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                        Place: rd.IsDBNull(2) ? 0 : rd.GetInt32(2),
                        ActivityId: activityId,
                        ActivityTitle: rd.IsDBNull(4) ? null : rd.GetString(4),
                        UserId: rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5),
                        WinnerName: rd.IsDBNull(6) ? null : rd.GetString(6),
                        AwardDate: rd.IsDBNull(7) ? (DateTime?)null : rd.GetDateTime(7),
                        TeamId: rd.IsDBNull(8) ? (int?)null : rd.GetInt32(8),
                        PrizeDescription: rd.IsDBNull(9) ? null : rd.GetString(9)
                    ));
                }
                return Ok(items);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogWarning(ex, "GET /api/podium failed. SQL: {Sql} year={Year}", sql, year);
                return Ok(new List<PodiumDto>());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GET /api/podium unexpected error. SQL: {Sql} year={Year}", sql, year);
                return Ok(new List<PodiumDto>());
            }
        }

        [HttpGet("speakers")]
        public async Task<ActionResult> GetSpeakers()
        {
            try
            {
                // Discover available columns to build a resilient SELECT
                const string schemaSql = @"SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name='speakers'";
                var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using (var schemaCmd = _ds.CreateCommand(schemaSql))
                await using (var schemaRd = await schemaCmd.ExecuteReaderAsync())
                {
                    while (await schemaRd.ReadAsync())
                    {
                        cols.Add(schemaRd.GetString(0));
                    }
                }

                bool hasCompany = cols.Contains("company");
                bool hasOrgName = cols.Contains("org_name");
                bool hasAvatarUrl = cols.Contains("avatar_url");
                bool hasPhotoUrl = cols.Contains("photo_url");
                bool hasRoleTitle = cols.Contains("role_title");
                bool hasLinks = cols.Contains("social");
                bool hasIsActive = cols.Contains("is_active");

                var selectParts = new List<string>
                {
                    "id",
                    "full_name AS name",
                    "bio"
                };
                selectParts.Add(hasCompany ? "company" : (hasOrgName ? "org_name AS company" : "NULL::text AS company"));
                selectParts.Add(hasRoleTitle ? "role_title AS roleTitle" : "NULL::text AS roleTitle");
                selectParts.Add(hasAvatarUrl ? "avatar_url AS avatarUrl" : (hasPhotoUrl ? "photo_url AS avatarUrl" : "NULL::text AS avatarUrl"));
                selectParts.Add(hasLinks ? "social AS links" : "NULL::text AS links");

                var sql = $"SELECT {string.Join(", ", selectParts)} FROM public.speakers";
                if (hasIsActive)
                {
                    sql += " WHERE is_active = true";
                }
                sql += " ORDER BY full_name ASC";

                var items = new List<SpeakerDto>();
                await using var cmd = _ds.CreateCommand(sql);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    string id;
                    try
                    {
                        var typeName = rd.GetDataTypeName(0).ToLowerInvariant();
                        if (typeName == "uuid") id = rd.GetGuid(0).ToString();
                        else if (typeName == "int4") id = rd.GetInt32(0).ToString();
                        else if (typeName == "int8" || typeName == "bigint") id = rd.GetInt64(0).ToString();
                        else id = rd.GetValue(0)?.ToString() ?? string.Empty;
                    }
                    catch
                    {
                        id = rd.GetValue(0)?.ToString() ?? string.Empty;
                    }

                    Dictionary<string, object>? links = null;
                    var linksIndex = 6; // id(0), name(1), bio(2), company(3), roleTitle(4), avatarUrl(5), links(6)
                    if (!rd.IsDBNull(linksIndex))
                    {
                        try
                        {
                            var json = rd.GetString(linksIndex);
                            links = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        }
                        catch
                        {
                            links = null;
                        }
                    }

                    items.Add(new SpeakerDto(
                        Id: id,
                        Name: rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                        Bio: rd.IsDBNull(2) ? null : rd.GetString(2),
                        Company: rd.IsDBNull(3) ? null : rd.GetString(3),
                        RoleTitle: rd.IsDBNull(4) ? null : rd.GetString(4),
                        AvatarUrl: rd.IsDBNull(5) ? null : rd.GetString(5),
                        Links: links
                    ));
                }

                return Ok(items);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogWarning(ex, "GET /api/speakers failed during dynamic schema query.");
                return Ok(new List<SpeakerDto>());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GET /api/speakers unexpected error during dynamic schema query.");
                return Ok(new List<SpeakerDto>());
            }
        }

        [HttpGet("winners")]
        public async Task<ActionResult<IEnumerable<PodiumItemDto>>> GetWinners([FromQuery] int? year)
        {
            var targetYear = year.HasValue && year.Value > 0 ? year.Value : DateTime.UtcNow.Year;
            const string sql = @"
                SELECT 
                    year            AS ""Year"", 
                    ""activityId""    AS ""ActivityId"", 
                    activity_title  AS ""Title"", 
                    position        AS ""Position"", 
                    user_id         AS ""UserId"", 
                    user_full_name  AS ""UserName""
                FROM vw_podium_by_year 
                WHERE year = @year 
                ORDER BY position";

            try
            {
                var result = await _context.Database
                    .SqlQueryRaw<PodiumItemDto>(sql, new NpgsqlParameter("@year", targetYear))
                    .ToListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo consulta vw_podium_by_year en /api/winners; year={Year}; devolviendo []", targetYear);
                return Ok(Array.Empty<PodiumItemDto>());
            }
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            try
            {
                var totalParticipants = await _context.Users.CountAsync();
                var totalActivities = await _context.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM activities")
                    .SingleAsync();
                var totalEnrollments = await _context.Enrollments.CountAsync();
                
                // Métricas por tipo de actividad
                var activitiesByType = await _context.Database
                    .SqlQueryRaw<TypeCountDto>(
                        "SELECT activity_type AS \"Type\", COUNT(*) AS \"Count\" FROM activities GROUP BY activity_type")
                    .ToListAsync();
                
                // Próximas actividades (próximas 5)
                var upcomingActivities = await _context.Database
                    .SqlQueryRaw<UpcomingActivityDto>(
                        @"SELECT 
                            id              AS ""Id"", 
                            title           AS ""Title"", 
                            start_time      AS ""StartTime"", 
                            activity_type   AS ""ActivityType""
                          FROM vw_public_activities
                          WHERE start_time > NOW()
                          ORDER BY start_time
                          LIMIT 5")
                    .ToListAsync();
                
                // Inscripciones recientes (últimas 10) - necesitamos agregar CreatedAt al modelo
                var recentEnrollments = await _context.Database
                    .SqlQueryRaw<RecentEnrollmentDto>(
                        @"SELECT 
                            e.id           AS ""Id"",
                            u.full_name    AS ""UserName"",
                            a.title        AS ""ActivityTitle""
                          FROM enrollments e
                          JOIN users u ON u.id = e.user_id
                          JOIN activities a ON a.id = e.activity_id
                          ORDER BY e.id DESC
                          LIMIT 10")
                    .ToListAsync();
                
                var snap = _metrics.GetSnapshot();
                var metrics = new
                {
                    totalParticipants,
                    totalActivities,
                    totalEnrollments,
                    activitiesByType,
                    upcomingActivities,
                    recentEnrollments,
                    qr = new {
                        totalScans = snap.TotalQrScans,
                        successfulScans = snap.SuccessfulQrScans,
                        failedScans = snap.FailedQrScans,
                        secureScans = snap.SecureQrScans,
                        legacyScans = snap.LegacyQrScans,
                        totalGenerations = snap.TotalQrGenerations,
                        successfulGenerations = snap.SuccessfulQrGenerations,
                        failedGenerations = snap.FailedQrGenerations,
                        secureGenerations = snap.SecureQrGenerations,
                        legacyGenerations = snap.LegacyQrGenerations,
                        totalValidations = snap.TotalQrValidations,
                        successfulValidations = snap.SuccessfulQrValidations,
                        failedValidations = snap.FailedQrValidations
                    }
                };
                
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener métricas", error = ex.Message });
            }
        }
        
        [HttpGet("agenda")]
        public async Task<ActionResult> GetAgenda(
            [FromQuery] string? day = null,
            [FromQuery] string? type = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var query = _context.PublicActivities.AsQueryable();

                if (!string.IsNullOrWhiteSpace(type))
                {
                    var t = type!.Trim().ToLower();
                    query = query.Where(a => a.ActivityType.ToLower() == t);
                }
                if (from.HasValue)
                    query = query.Where(a => a.StartTime >= from.Value);
                if (to.HasValue)
                    query = query.Where(a => a.EndTime <= to.Value);

                var items = await query
                    .OrderBy(a => a.StartTime)
                    .Select(a => new
                    {
                        id = a.Id,
                        title = a.Title,
                        startISO = a.StartTime,
                        endISO = a.EndTime,
                        place = a.Location,
                        type = a.ActivityType.ToLower(),
                        day = a.StartTime.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                if (!string.IsNullOrWhiteSpace(day))
                {
                    var targetDay = day.Trim();
                    items = items.Where(i => i.day == targetDay).ToList();
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GET /api/agenda failed; returning []");
                return Ok(Array.Empty<object>());
            }
        }
    }
}