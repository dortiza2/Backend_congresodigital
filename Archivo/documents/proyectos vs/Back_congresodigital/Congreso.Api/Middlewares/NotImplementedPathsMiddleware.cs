using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Congreso.Api.Middlewares
{
    public class NotImplementedPathsMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly string[] BlockedPrefixes = new[]
        {
            "/api/attendances",
            "/api/admin/speakers",
            "/api/admin/organizations",
            "/api/public/faq"
        };

        public NotImplementedPathsMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx)
        {
            var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            if (BlockedPrefixes.Any(p => path.StartsWith(p)))
            {
                ctx.Response.StatusCode = StatusCodes.Status410Gone;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                var payload = new { ok = false, status = 410, reason = "Not implemented in current DB schema", path };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
                return;
            }
            await _next(ctx);
        }
    }
}