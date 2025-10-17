using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Congreso.Api.Controllers;

[ApiController]
[Route("api/admin/outbox")]
public class AdminOutboxController : ControllerBase
{
    private readonly NpgsqlDataSource _ds;
    public AdminOutboxController(NpgsqlDataSource ds) => _ds = ds;

    [HttpGet("pending")]
    public async Task<IActionResult> Pending([FromQuery]int take=50)
    {
        take = Math.Clamp(take, 1, 200);
        const string sql = @"SELECT id, created_at, topic, payload, status FROM v_outbox_pending ORDER BY created_at ASC LIMIT @t;";
        await using var cmd = _ds.CreateCommand(sql);
        cmd.Parameters.AddWithValue("@t", take);

        var items = new List<object>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            items.Add(new {
                id = rd.GetInt32(0),
                created_at = rd.GetDateTime(1),
                topic = rd.IsDBNull(2) ? null : rd.GetString(2),
                payload = rd.IsDBNull(3) ? null : rd.GetString(3),
                status = rd.IsDBNull(4) ? null : rd.GetString(4)
            });
        }
        return Ok(new { count = items.Count, items });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        const string sql = @"SELECT * FROM v_outbox_stats LIMIT 100;";
        await using var cmd = _ds.CreateCommand(sql);
        var rows = new List<Dictionary<string, object?>>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var obj = new Dictionary<string, object?>();
            for (int i=0; i<rd.FieldCount; i++)
                obj[rd.GetName(i)] = await rd.IsDBNullAsync(i) ? null : rd.GetValue(i);
            rows.Add(obj);
        }
        return Ok(new { count = rows.Count, rows });
    }
}