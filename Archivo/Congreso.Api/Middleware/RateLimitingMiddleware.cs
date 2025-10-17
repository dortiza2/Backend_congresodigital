using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Congreso.Api.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IRateLimitCounter _rateLimitCounter;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IRateLimitCounter rateLimitCounter)
        {
            _next = next;
            _logger = logger;
            _rateLimitCounter = rateLimitCounter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var endpoint = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;
            
            var limitConfig = GetRateLimitConfig(endpoint, method);
            
            if (limitConfig != null)
            {
                var key = $"{clientIp}:{endpoint}:{method}";
                var currentCount = _rateLimitCounter.Increment(key, limitConfig.WindowSeconds);
                
                // Log métrica de intento
                _logger.LogInformation("Rate limit attempt: {IP} {Endpoint} {Method} {Count}/{Limit}", 
                    clientIp, endpoint, method, currentCount, limitConfig.Limit);
                
                if (currentCount > limitConfig.Limit)
                {
                    // Log métrica de bloqueo
                    _logger.LogWarning("Rate limit exceeded: {IP} {Endpoint} {Method} {Count}/{Limit}", 
                        clientIp, endpoint, method, currentCount, limitConfig.Limit);
                    
                    context.Response.StatusCode = 429;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        error = "Límite de solicitudes alcanzado",
                        message = $"Has excedido el límite de {limitConfig.Limit} solicitudes por {limitConfig.WindowSeconds} segundos",
                        retryAfter = limitConfig.WindowSeconds
                    };
                    
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
            }
            
            await _next(context);
        }

        private RateLimitConfig GetRateLimitConfig(string endpoint, string method)
        {
            // Auth endpoints
            if (endpoint.Contains("/auth/") || endpoint.Contains("/login"))
            {
                return new RateLimitConfig { Limit = 5, WindowSeconds = 60 };
            }
            
            // Admin endpoints
            if (endpoint.Contains("/api/admin/") || endpoint.Contains("/api/users/"))
            {
                return new RateLimitConfig { Limit = 10, WindowSeconds = 60 };
            }
            
            // Public endpoints
            if (endpoint.Contains("/api/public/"))
            {
                return new RateLimitConfig { Limit = 60, WindowSeconds = 60 };
            }
            
            return null;
        }
    }

    public class RateLimitConfig
    {
        public int Limit { get; set; }
        public int WindowSeconds { get; set; }
    }

    public interface IRateLimitCounter
    {
        int Increment(string key, int windowSeconds);
    }

    public class InMemoryRateLimitCounter : IRateLimitCounter
    {
        private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();
        private readonly ILogger<InMemoryRateLimitCounter> _logger;

        public InMemoryRateLimitCounter(ILogger<InMemoryRateLimitCounter> logger)
        {
            _logger = logger;
        }

        public int Increment(string key, int windowSeconds)
        {
            var counter = _counters.GetOrAdd(key, k => new SlidingWindowCounter(windowSeconds));
            var count = counter.Increment();
            
            // Limpiar contadores antiguos
            CleanupOldCounters();
            
            return count;
        }

        private void CleanupOldCounters()
        {
            var now = DateTime.UtcNow;
            var keysToRemove = _counters.Where(kvp => kvp.Value.IsExpired(now)).Select(kvp => kvp.Key).ToList();
            
            foreach (var key in keysToRemove)
            {
                _counters.TryRemove(key, out _);
            }
        }
    }

    public class SlidingWindowCounter
    {
        private readonly Queue<DateTime> _requests = new();
        private readonly int _windowSeconds;
        private readonly object _lock = new();

        public SlidingWindowCounter(int windowSeconds)
        {
            _windowSeconds = windowSeconds;
        }

        public int Increment()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddSeconds(-_windowSeconds);
                
                // Remover requests fuera de la ventana
                while (_requests.Count > 0 && _requests.Peek() < windowStart)
                {
                    _requests.Dequeue();
                }
                
                _requests.Enqueue(now);
                return _requests.Count;
            }
        }

        public bool IsExpired(DateTime now)
        {
            lock (_lock)
            {
                return _requests.Count == 0 || _requests.Peek() < now.AddSeconds(-_windowSeconds * 2);
            }
        }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddSingleton<IRateLimitCounter, InMemoryRateLimitCounter>();
            return services;
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}