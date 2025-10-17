using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace Congreso.Api.Infrastructure.HealthChecks
{
    public class CacheHealthCheck : IHealthCheck
    {
        private readonly IMemoryCache _cache;
        private readonly IMetricsCollector _metrics;
        private readonly Serilog.ILogger _logger;

        public CacheHealthCheck(IMemoryCache cache, IMetricsCollector metrics)
        {
            _cache = cache;
            _metrics = metrics;
            _logger = Log.ForContext<CacheHealthCheck>();
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.Debug("Ejecutando health check de caché");

                // Verificar que el caché esté funcionando correctamente
                var testKey = $"health_check_{DateTime.UtcNow.Ticks}";
                var testValue = "healthy";

                // Intentar agregar un valor al caché
                _cache.Set(testKey, testValue, TimeSpan.FromSeconds(10));

                // Intentar recuperar el valor
                if (_cache.TryGetValue(testKey, out string? retrievedValue) && retrievedValue == testValue)
                {
                    // Limpiar el valor de prueba
                    _cache.Remove(testKey);

                    stopwatch.Stop();
                    
                    _metrics.RecordHealthCheckDuration("cache", stopwatch.Elapsed.TotalSeconds, true);
                    _metrics.RecordHealthCheckStatus("cache", true);

                    _logger.Debug("Health check de caché completado exitosamente en {Duration}ms", 
                        stopwatch.ElapsedMilliseconds);

                    return Task.FromResult(HealthCheckResult.Healthy(
                        description: "Caché operativo y accesible",
                        data: new Dictionary<string, object>
                        {
                            { "response_time_ms", stopwatch.ElapsedMilliseconds },
                            { "cache_type", "memory" },
                            { "test_result", "successful" }
                        }));
                }
                else
                {
                    stopwatch.Stop();
                    
                    _metrics.RecordHealthCheckDuration("cache", stopwatch.Elapsed.TotalSeconds, false);
                    _metrics.RecordHealthCheckStatus("cache", false);

                    _logger.Error("Health check de caché falló: no se pudo recuperar el valor de prueba");

                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        description: "Caché no funcional correctamente",
                        data: new Dictionary<string, object>
                        {
                            { "response_time_ms", stopwatch.ElapsedMilliseconds },
                            { "cache_type", "memory" },
                            { "test_result", "failed" },
                            { "error", "Cache retrieval failed" }
                        }));
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _metrics.RecordHealthCheckDuration("cache", stopwatch.Elapsed.TotalSeconds, false);
                _metrics.RecordHealthCheckStatus("cache", false);

                _logger.Error(ex, "Health check de caché falló con excepción después de {Duration}ms", 
                    stopwatch.ElapsedMilliseconds);

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    description: "Error al verificar el caché",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "response_time_ms", stopwatch.ElapsedMilliseconds },
                        { "cache_type", "memory" },
                        { "error_type", ex.GetType().Name },
                        { "error_message", ex.Message }
                    }));
            }
        }
    }

    public class CertificateEngineHealthCheck : IHealthCheck
    {
        private readonly IMetricsCollector _metrics;
        private readonly Serilog.ILogger _logger;
        private readonly IConfiguration _configuration;

        public CertificateEngineHealthCheck(IMetricsCollector metrics, IConfiguration configuration)
        {
            _metrics = metrics;
            _configuration = configuration;
            _logger = Log.ForContext<CertificateEngineHealthCheck>();
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.Debug("Ejecutando health check del motor de certificados");

                // Verificar que las plantillas de certificados estén configuradas
                var templatePath = _configuration["CertificateSettings:TemplatePath"];
                var fontsPath = _configuration["CertificateSettings:FontsPath"];

                var issues = new List<string>();

                // Verificar existencia de rutas de plantillas
                if (string.IsNullOrEmpty(templatePath))
                {
                    issues.Add("Ruta de plantillas no configurada");
                }
                else if (!Directory.Exists(templatePath))
                {
                    issues.Add($"Directorio de plantillas no existe: {templatePath}");
                }

                // Verificar existencia de fuentes
                if (string.IsNullOrEmpty(fontsPath))
                {
                    issues.Add("Ruta de fuentes no configurada");
                }
                else if (!Directory.Exists(fontsPath))
                {
                    issues.Add($"Directorio de fuentes no existe: {fontsPath}");
                }

                // Verificar configuración de PDF
                var pdfSettings = _configuration.GetSection("CertificateSettings:PdfSettings");
                if (!pdfSettings.Exists())
                {
                    issues.Add("Configuración de PDF no encontrada");
                }

                stopwatch.Stop();

                if (issues.Count == 0)
                {
                    _metrics.RecordHealthCheckDuration("certificate_engine", stopwatch.Elapsed.TotalSeconds, true);
                    _metrics.RecordHealthCheckStatus("certificate_engine", true);

                    _logger.Debug("Health check del motor de certificados completado exitosamente en {Duration}ms", 
                        stopwatch.ElapsedMilliseconds);

                    return Task.FromResult(HealthCheckResult.Healthy(
                        description: "Motor de certificados configurado y listo",
                        data: new Dictionary<string, object>
                        {
                            { "response_time_ms", stopwatch.ElapsedMilliseconds },
                            { "template_path", templatePath },
                            { "fonts_path", fontsPath },
                            { "status", "configured" }
                        }));
                }
                else
                {
                    _metrics.RecordHealthCheckDuration("certificate_engine", stopwatch.Elapsed.TotalSeconds, false);
                    _metrics.RecordHealthCheckStatus("certificate_engine", false);

                    _logger.Warning("Health check del motor de certificados encontró {Count} problemas: {Issues}", 
                        issues.Count, string.Join(", ", issues));

                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        description: $"Motor de certificados no configurado correctamente: {string.Join(", ", issues)}",
                        data: new Dictionary<string, object>
                        {
                            { "response_time_ms", stopwatch.ElapsedMilliseconds },
                            { "issues_count", issues.Count },
                            { "issues", issues },
                            { "status", "misconfigured" }
                        }));
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _metrics.RecordHealthCheckDuration("certificate_engine", stopwatch.Elapsed.TotalSeconds, false);
                _metrics.RecordHealthCheckStatus("certificate_engine", false);

                _logger.Error(ex, "Health check del motor de certificados falló con excepción después de {Duration}ms", 
                    stopwatch.ElapsedMilliseconds);

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    description: "Error al verificar el motor de certificados",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "response_time_ms", stopwatch.ElapsedMilliseconds },
                        { "error_type", ex.GetType().Name },
                        { "error_message", ex.Message }
                    }));
            }
        }
    }

    public class HealthCheckOptions
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool IncludeDetailedErrors { get; set; } = false;
        public Dictionary<string, string> CustomChecks { get; set; } = new();
    }
}