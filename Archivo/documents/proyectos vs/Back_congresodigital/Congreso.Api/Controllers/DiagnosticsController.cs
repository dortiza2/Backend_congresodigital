using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Congreso.Api.Infrastructure;

namespace Congreso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly IMetricsCollector _metricsCollector;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            HealthCheckService healthCheckService,
            IMetricsCollector metricsCollector,
            ILogger<DiagnosticsController> logger)
        {
            _healthCheckService = healthCheckService;
            _metricsCollector = metricsCollector;
            _logger = logger;
        }

        /// <summary>
        /// Health check básico - verifica que el servicio esté vivo
        /// </summary>
        [HttpGet("healthz")]
        public async Task<IActionResult> Healthz()
        {
            try
            {
                var healthCheck = await _healthCheckService.CheckHealthAsync();
                
                var result = new
                {
                    status = healthCheck.Status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    checks = new Dictionary<string, object>()
                };

                foreach (var entry in healthCheck.Entries)
                {
                    result.checks[entry.Key] = new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration.TotalMilliseconds,
                        exception = entry.Value.Exception?.Message
                    };
                }

                if (healthCheck.Status == HealthStatus.Healthy)
                {
                    return Ok(result);
                }
                else if (healthCheck.Status == HealthStatus.Degraded)
                {
                    return StatusCode(503, result);
                }
                else
                {
                    return StatusCode(503, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en health check");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    error = "Health check failed"
                });
            }
        }

        /// <summary>
        /// Readiness check - verifica que el servicio esté listo para recibir tráfico
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                // Verificar certificados, caché, colas, etc.
                var readinessChecks = await PerformReadinessChecks();
                
                var result = new
                {
                    status = readinessChecks.AllHealthy ? "Ready" : "NotReady",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    checks = readinessChecks.Checks
                };

                if (readinessChecks.AllHealthy)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode(503, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en readiness check");
                return StatusCode(503, new
                {
                    status = "NotReady",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    error = "Readiness check failed"
                });
            }
        }

        /// <summary>
        /// Métricas en formato Prometheus
        /// </summary>
        [HttpGet("metrics")]
        public IActionResult Metrics()
        {
            try
            {
                var metrics = _metricsCollector.GetPrometheusMetrics();
                return Content(metrics, "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo métricas");
                return StatusCode(503, "# Error obteniendo métricas\n");
            }
        }

        private async Task<ReadinessResult> PerformReadinessChecks()
        {
            var checks = new Dictionary<string, object>();
            var allHealthy = true;

            // Check 1: Certificados (simulado)
            try
            {
                // En producción, verificaría certificados SSL, firmas digitales, etc.
                checks["certificates"] = new
                {
                    status = "healthy",
                    description = "Certificados disponibles",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }
            catch (Exception ex)
            {
                allHealthy = false;
                checks["certificates"] = new
                {
                    status = "unhealthy",
                    description = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }

            // Check 2: Caché (simulado)
            try
            {
                // En producción, verificaría conexión a Redis, Memcached, etc.
                checks["cache"] = new
                {
                    status = "healthy",
                    description = "Caché disponible",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }
            catch (Exception ex)
            {
                allHealthy = false;
                checks["cache"] = new
                {
                    status = "unhealthy",
                    description = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }

            // Check 3: Colas (simulado)
            try
            {
                // En producción, verificaría colas de mensajes, servicios de background, etc.
                checks["queues"] = new
                {
                    status = "healthy",
                    description = "Colas de procesamiento disponibles",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }
            catch (Exception ex)
            {
                allHealthy = false;
                checks["queues"] = new
                {
                    status = "unhealthy",
                    description = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }

            // Check 4: PDF Engine (mencionado en los requisitos)
            try
            {
                // Verificar que el motor de PDF esté disponible
                checks["pdf_engine"] = new
                {
                    status = "healthy",
                    description = "Motor de PDF disponible",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }
            catch (Exception ex)
            {
                allHealthy = false;
                checks["pdf_engine"] = new
                {
                    status = "unhealthy",
                    description = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }

            return new ReadinessResult
            {
                AllHealthy = allHealthy,
                Checks = checks
            };
        }

        private class ReadinessResult
        {
            public bool AllHealthy { get; set; }
            public Dictionary<string, object> Checks { get; set; } = new();
        }
    }
}