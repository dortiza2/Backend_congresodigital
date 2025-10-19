using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Congreso.Api.DTOs;

namespace Congreso.Api.Controllers;

[ApiController]
[Route("api/activities")]
public class PublicActivitiesController : ControllerBase
{
    private readonly NpgsqlDataSource _ds;
    private readonly ILogger<PublicActivitiesController> _logger;
    public PublicActivitiesController(NpgsqlDataSource ds, ILogger<PublicActivitiesController> logger)
    {
        _ds = ds;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? kinds = null)
    {
        // Build dynamic WHERE based on kinds CSV per contract
        string whereClause = BuildWhereClauseForKinds(kinds);
        string sql = $@"
            SELECT 
                a.id,
                a.title,
                a.location,
                a.start_time,
                a.end_time,
                a.capacity,
                a.published,
                CASE
                    WHEN EXISTS (
                        SELECT 1 FROM public.activity_tags at
                        JOIN public.tags t ON t.id = at.tag_id
                        WHERE at.activity_id = a.id AND LOWER(t.name) IN ('workshop','taller')
                    ) THEN 'workshop'
                    WHEN EXISTS (
                        SELECT 1 FROM public.activity_tags at
                        JOIN public.tags t ON t.id = at.tag_id
                        WHERE at.activity_id = a.id AND LOWER(t.name) IN ('competition','competencia')
                    ) THEN 'competition'
                    WHEN LOWER(COALESCE((a.activity_type)::text, '')) IN ('talk','charla') THEN 'talk'
                    ELSE 'activity'
                END AS activity_type_out,
                s_speaker.id AS speaker_id,
                s_speaker.full_name AS speaker_name,
                s_speaker.role_title AS speaker_role_title,
                s_speaker.company AS speaker_company,
                s_speaker.avatar_url AS speaker_avatar_url
            FROM public.vw_api_activities a
            LEFT JOIN LATERAL (
                SELECT s.id, s.full_name, s.role_title, s.company, s.avatar_url
                FROM public.activity_speakers asp
                JOIN public.speakers s ON s.id = asp.speaker_id
                WHERE asp.activity_id = a.id
                ORDER BY s.full_name ASC
                LIMIT 1
            ) AS s_speaker ON TRUE
            {whereClause}
            ORDER BY a.start_time ASC, a.id ASC;
        ";

        try
        {
            var items = new List<ActivityDto>();
            await using var cmd = _ds.CreateCommand(sql);
            await using var rd = await cmd.ExecuteReaderAsync();

            // Compute ordinals defensively
            int ordId = TryGetOrdinal(rd, "id");
            int ordTitle = TryGetOrdinal(rd, "title");
            int ordLocation = TryGetOrdinal(rd, "location");
            int ordStartsAt = TryGetOrdinal(rd, "starts_at", fallback: "start_time");
            int ordEndsAt = TryGetOrdinal(rd, "ends_at", fallback: "end_time");
            int ordCapacity = TryGetOrdinal(rd, "capacity");
            // published may not exist in some schemas; fallback to is_active
            int ordPublished = TryGetOrdinal(rd, "published");
            if (ordPublished < 0) ordPublished = TryGetOrdinal(rd, "is_active");
            int ordTypeOut = TryGetOrdinal(rd, "activity_type_out", fallback: "activity_type");

            int ordSpeakerId = TryGetOrdinal(rd, "speaker_id");
            int ordSpeakerName = TryGetOrdinal(rd, "speaker_name");
            int ordSpeakerRoleTitle = TryGetOrdinal(rd, "speaker_role_title");
            int ordSpeakerCompany = TryGetOrdinal(rd, "speaker_company");
            int ordSpeakerAvatarUrl = TryGetOrdinal(rd, "speaker_avatar_url");

            while (await rd.ReadAsync())
            {
                // id: activities.id may be uuid or int; output requires string per frontend contract
                var id = ReadIdAsString(rd, ordId) ?? string.Empty;

                var title = ReadString(rd, ordTitle) ?? string.Empty;
                var location = ReadString(rd, ordLocation);
                var startTime = ReadDateTime(rd, ordStartsAt);
                var endTime = ReadDateTime(rd, ordEndsAt);
                var capacity = ReadInt(rd, ordCapacity) ?? 0;
                var published = SafeGetBoolean(rd, ordPublished, defaultValue: true);
                var activityType = ReadString(rd, ordTypeOut) ?? "activity";

                // Speaker block (nullable)
                SpeakerDto? speaker = null;
                try
                {
                    bool hasSpeaker = (ordSpeakerId >= 0 && !rd.IsDBNull(ordSpeakerId)) || (ordSpeakerName >= 0 && !rd.IsDBNull(ordSpeakerName));
                    if (hasSpeaker)
                    {
                        var sid = ReadIdAsString(rd, ordSpeakerId) ?? string.Empty;
                        var sname = ReadString(rd, ordSpeakerName) ?? string.Empty;
                        var srole = ReadString(rd, ordSpeakerRoleTitle);
                        var scompany = ReadString(rd, ordSpeakerCompany);
                        var savatar = ReadString(rd, ordSpeakerAvatarUrl);
                        if (string.IsNullOrWhiteSpace(savatar)) savatar = null;

                        speaker = new SpeakerDto(
                            Id: sid,
                            Name: sname,
                            Bio: null,
                            Company: scompany,
                            RoleTitle: srole,
                            AvatarUrl: savatar,
                            Links: null
                        );
                    }
                }
                catch
                {
                    speaker = null;
                }

                // EnrolledCount and AvailableSpots per contract (temporary values)
                int enrolledCount = 0;
                int availableSpots = Math.Max((capacity) - enrolledCount, 0);

                items.Add(new ActivityDto(
                    Id: id,
                    Title: string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim(),
                    ActivityType: string.IsNullOrWhiteSpace(activityType) ? "activity" : activityType,
                    Location: string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
                    StartTime: startTime,
                    EndTime: endTime,
                    Capacity: Math.Max(capacity, 0),
                    Published: published,
                    EnrolledCount: enrolledCount,
                    AvailableSpots: availableSpots,
                    Speaker: speaker
                ));
            }
            return Ok(items);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogWarning(ex, "GET /api/activities failed. SQL: {Sql}", sql);
            return Ok(new List<ActivityDto>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GET /api/activities unexpected error. SQL: {Sql}", sql);
            return Ok(new List<ActivityDto>());
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { message = "Invalid activity id." });
        }

            const string sql = @"SELECT * FROM public.vw_api_activities_full WHERE id::text=@id;";
            await using var cmd = _ds.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@id", id);
            await using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return NotFound(new { message = "Activity not found", id });

            int ordId = TryGetOrdinal(rd, "id");
            int ordTitle = TryGetOrdinal(rd, "title");
            int ordDesc = TryGetOrdinal(rd, "description");
            int ordLoc = TryGetOrdinal(rd, "location");
            int ordStartsAt = TryGetOrdinal(rd, "starts_at", fallback: "start_time");
            int ordEndsAt = TryGetOrdinal(rd, "ends_at", fallback: "end_time");
            int ordCapacity = TryGetOrdinal(rd, "capacity");
            int ordIsActive = TryGetOrdinal(rd, "published");
            if (ordIsActive < 0) ordIsActive = TryGetOrdinal(rd, "is_active");
            int ordType = TryGetOrdinal(rd, "activity_type", fallback: "type");

            var dto = new
            {
                id = ReadIdAsString(rd, ordId),
                activity_type = ReadString(rd, ordType),
                title = ReadString(rd, ordTitle),
                description = ReadString(rd, ordDesc),
                location = ReadString(rd, ordLoc),
                starts_at = ReadDateTime(rd, ordStartsAt),
                ends_at = ReadDateTime(rd, ordEndsAt),
                capacity = ReadInt(rd, ordCapacity),
                is_active = ordIsActive >= 0 && !rd.IsDBNull(ordIsActive) && rd.GetBoolean(ordIsActive)
            };
            return Ok(dto);
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> Upcoming([FromQuery] int take = 10)
        {
            const string sql = @"SELECT id, title, start_time
                                 FROM public.vw_api_activities_upcoming
                                 WHERE start_time IS NOT NULL AND start_time >= now()
                                 ORDER BY start_time ASC, id ASC
                                 LIMIT @take;";
            try
            {
                take = Math.Clamp(take, 1, 50);
                await using var cmd = _ds.CreateCommand(sql);
                cmd.Parameters.AddWithValue("@take", take);
                var items = new List<object>();
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    items.Add(new
                    {
                        id = SafeGetInt32(rd, 0, 0),
                        title = rd.IsDBNull(1) ? null : rd.GetString(1),
                        start_time = rd.IsDBNull(2) ? (DateTime?)null : rd.GetDateTime(2)
                    });
                }
                return Ok(new { count = items.Count, items });
            }
            catch (NpgsqlException ex)
            {
                _logger.LogWarning(ex, "GET /api/activities/upcoming failed. SQL: {Sql}", sql);
                return Ok(new { count = 0, items = Array.Empty<object>() });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GET /api/activities/upcoming unexpected error. SQL: {Sql}", sql);
                return Ok(new { count = 0, items = Array.Empty<object>() });
            }
        }

    // Nuevo alias explícito para evitar que "public" se enrute a {id}
    [HttpGet("public")]
    public Task<IActionResult> PublicList([FromQuery] string? kinds = null)
    {
        return List(kinds);
    }

    private static int TryGetOrdinal(Npgsql.NpgsqlDataReader rd, string name, string? fallback = null)
    {
        try { return rd.GetOrdinal(name); } catch { }
        if (fallback != null)
        {
            try { return rd.GetOrdinal(fallback); } catch { }
        }
        return -1;
    }

    private static int SafeGetInt32(Npgsql.NpgsqlDataReader rd, int ordinal, int defaultValue = 0)
    {
        if (ordinal < 0 || rd.IsDBNull(ordinal)) return defaultValue;
        try
        {
            var typeName = rd.GetDataTypeName(ordinal).ToLowerInvariant();
            if (typeName is "int4") return rd.GetInt32(ordinal);
            if (typeName is "int8") return (int)rd.GetInt64(ordinal);
            var val = rd.GetValue(ordinal);
            return Convert.ToInt32(val);
        }
        catch { return defaultValue; }
    }

    private static bool SafeGetBoolean(Npgsql.NpgsqlDataReader rd, int ordinal, bool defaultValue = true)
    {
        if (ordinal < 0 || rd.IsDBNull(ordinal)) return defaultValue;
        try
        {
            var typeName = rd.GetDataTypeName(ordinal).ToLowerInvariant();
            if (typeName is "bool" or "boolean") return rd.GetBoolean(ordinal);
            var val = rd.GetValue(ordinal);
            if (val is bool b) return b;
            if (val is int i) return i != 0;
            var s = val?.ToString()?.Trim();
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
            return defaultValue;
        }
        catch { return defaultValue; }
    }

    private static string? ReadString(Npgsql.NpgsqlDataReader rd, int ordinal)
    {
        if (ordinal < 0 || rd.IsDBNull(ordinal)) return null;
        return rd.GetString(ordinal);
    }

    private static DateTime? ReadDateTime(Npgsql.NpgsqlDataReader rd, int ordinal)
    {
        if (ordinal < 0 || rd.IsDBNull(ordinal)) return null;
        return rd.GetDateTime(ordinal);
    }

    private static int? ReadInt(Npgsql.NpgsqlDataReader rd, int ordinal)
    {
        if (ordinal < 0 || rd.IsDBNull(ordinal)) return null;
        return rd.GetInt32(ordinal);
    }

    private static string? ReadIdAsString(Npgsql.NpgsqlDataReader rd, int ordinal)
    {
        if (ordinal < 0 || rd.IsDBNull(ordinal)) return null;
        try
        {
            var typeName = rd.GetDataTypeName(ordinal).ToLowerInvariant();
            if (typeName == "uuid")
            {
                return rd.GetGuid(ordinal).ToString();
            }
            if (typeName is "bigint" or "int8")
            {
                return rd.GetInt64(ordinal).ToString();
            }
            var val = rd.GetValue(ordinal);
            return val?.ToString();
        }
        catch
        {
            var val = rd.GetValue(ordinal);
            return val?.ToString();
        }
    }

    private static string BuildWhereClauseForKinds(string? kinds)
    {
        if (string.IsNullOrWhiteSpace(kinds)) return string.Empty;
        var tokens = kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var includeWorkshop = false;
        var includeCompetition = false;
        var includeTalk = false;
        var includeActivity = false;
        foreach (var t in tokens)
        {
            var k = t.Trim().ToLowerInvariant();
            includeWorkshop |= k == "workshop" || k == "taller"; // tolerate spanish input
            includeCompetition |= k == "competition" || k == "competencia";
            includeTalk |= k == "talk" || k == "charla";
            includeActivity |= k == "activity" || k == "actividad";
        }

        var filters = new List<string>();
        if (includeWorkshop)
        {
            filters.Add(@"EXISTS (SELECT 1 FROM public.activity_tags at JOIN public.tags t ON t.id = at.tag_id WHERE at.activity_id = a.id AND LOWER(t.name) IN ('workshop','taller'))");
        }
        if (includeCompetition)
        {
            filters.Add(@"EXISTS (SELECT 1 FROM public.activity_tags at JOIN public.tags t ON t.id = at.tag_id WHERE at.activity_id = a.id AND LOWER(t.name) IN ('competition','competencia'))");
        }
        if (includeTalk)
        {
            filters.Add(@"LOWER(COALESCE((a.activity_type)::text, '')) IN ('talk','charla')");
        }
        if (includeActivity)
        {
            filters.Add(@"NOT EXISTS (
                              SELECT 1 FROM public.activity_tags at JOIN public.tags t ON t.id = at.tag_id
                              WHERE at.activity_id = a.id AND LOWER(t.name) IN ('workshop','taller','competition','competencia')
                           ) AND LOWER(COALESCE((a.activity_type)::text, '')) NOT IN ('talk','charla')");
        }

        if (filters.Count == 0) return string.Empty;
        return "WHERE (" + string.Join(" OR ", filters) + ")";
    }

    [HttpGet("/api/workshops")]
    public async Task<IActionResult> Workshops()
    {
        // Reuse List with kinds=workshop
        return await List("workshop");
    }
}