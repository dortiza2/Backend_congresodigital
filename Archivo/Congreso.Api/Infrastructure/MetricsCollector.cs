using System.Diagnostics;
using Prometheus;
using Serilog;

namespace Congreso.Api.Infrastructure
{
    public class MetricsCollector : IMetricsCollector
    {
        private static readonly Serilog.ILogger Logger = Log.ForContext<MetricsCollector>();

        // Métricas de Podio
        private static readonly Histogram PodiumQueryDuration = Metrics.CreateHistogram(
            "podium_query_duration_seconds",
            "Duración de consultas de podio en segundos",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "status" },
                Buckets = Histogram.LinearBuckets(0.01, 0.05, 20) // 0.01s a 1s
            });

        private static readonly Counter PodiumCacheHits = Metrics.CreateCounter(
            "active_podium_cache_hits_total",
            "Total de hits de caché de podio",
            new CounterConfiguration
            {
                LabelNames = new[] { "cache_type", "year" }
            });

        private static readonly Counter PodiumCacheMisses = Metrics.CreateCounter(
            "active_podium_cache_misses_total",
            "Total de misses de caché de podio",
            new CounterConfiguration
            {
                LabelNames = new[] { "cache_type", "year" }
            });

        private static readonly Counter PodiumOperations = Metrics.CreateCounter(
            "podium_operations_total",
            "Total de operaciones de podio",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "status" }
            });

        // Métricas de Certificados (existentes)
        private static readonly Histogram CertificateGenerationDuration = Metrics.CreateHistogram(
            "certificate_generation_duration_seconds",
            "Duración de generación de certificados en segundos",
            new HistogramConfiguration
            {
                LabelNames = new[] { "type", "status" },
                Buckets = Histogram.LinearBuckets(0.1, 0.5, 20) // 0.1s a 10s
            });

        private static readonly Counter CertificateValidationTotal = Metrics.CreateCounter(
            "certificate_validation_total",
            "Total de validaciones de certificados",
            new CounterConfiguration
            {
                LabelNames = new[] { "valid", "type" }
            });

        private static readonly Counter CertificateOperations = Metrics.CreateCounter(
            "certificate_operations_total",
            "Total de operaciones de certificados",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "status" }
            });

        // Métricas de Health Checks
        private static readonly Gauge HealthCheckStatus = Metrics.CreateGauge(
            "health_check_status",
            "Estado de health checks (1 = healthy, 0 = unhealthy)",
            new GaugeConfiguration
            {
                LabelNames = new[] { "check_name", "service" }
            });

        private static readonly Histogram HealthCheckDuration = Metrics.CreateHistogram(
            "health_check_duration_seconds",
            "Duración de health checks en segundos",
            new HistogramConfiguration
            {
                LabelNames = new[] { "check_name", "status" },
                Buckets = Histogram.LinearBuckets(0.001, 0.005, 20) // 1ms a 100ms
            });

        // Métricas de Base de Datos
        private static readonly Counter DatabaseConnections = Metrics.CreateCounter(
            "database_connections_total",
            "Total de conexiones a base de datos",
            new CounterConfiguration
            {
                LabelNames = new[] { "status", "database" }
            });

        private static readonly Histogram DatabaseQueryDuration = Metrics.CreateHistogram(
            "database_query_duration_seconds",
            "Duración de queries de base de datos en segundos",
            new HistogramConfiguration
            {
                LabelNames = new[] { "query_type", "table", "status" },
                Buckets = Histogram.LinearBuckets(0.001, 0.01, 20) // 1ms a 200ms
            });

        // Métricas de Auditoría
        private static readonly Counter AuditLogOperations = Metrics.CreateCounter(
            "audit_log_operations_total",
            "Total de operaciones de auditoría",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation_type", "entity", "status" }
            });

        // Métricas HTTP (para middleware)
        private static readonly Counter HttpRequestsTotal = Metrics.CreateCounter(
            "http_requests_total",
            "Total number of HTTP requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "method", "endpoint", "status_code" }
            });

        private static readonly Histogram HttpRequestDuration = Metrics.CreateHistogram(
            "http_request_duration_seconds",
            "HTTP request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint" },
                Buckets = Histogram.LinearBuckets(0.01, 0.05, 20) // 0.01s a 1s
            });

        private static readonly Counter ApiErrorsTotal = Metrics.CreateCounter(
            "api_errors_total",
            "Total number of API errors by type",
            new CounterConfiguration
            {
                LabelNames = new[] { "error_type", "endpoint" }
            });

        private static readonly Counter JwtAuthFailures = Metrics.CreateCounter(
            "jwt_auth_failures_total",
            "Total number of JWT authentication failures"
        );

        // Métodos para Podium
        public void RecordPodiumQueryDuration(double seconds, bool success)
        {
            var status = success ? "success" : "failure";
            PodiumQueryDuration.WithLabels("get_by_year", status).Observe(seconds);
            
            if (!success)
            {
                PodiumOperations.WithLabels("get_by_year", "failure").Inc();
            }
        }

        public void RecordCacheHit(string cacheType, string identifier)
        {
            PodiumCacheHits.WithLabels(cacheType, identifier).Inc();
            Logger.Information("Cache hit para {CacheType}: {Identifier}", cacheType, identifier);
        }

        public void RecordCacheMiss(string cacheType, string identifier)
        {
            PodiumCacheMisses.WithLabels(cacheType, identifier).Inc();
            Logger.Debug("Cache miss para {CacheType}: {Identifier}", cacheType, identifier);
        }

        public void RecordPodiumOperation(string operation, bool success)
        {
            var status = success ? "success" : "failure";
            PodiumOperations.WithLabels(operation, status).Inc();
            
            Logger.Information("Operación de podio: {Operation} - {Status}", operation, status);
        }

        // Métodos para Certificados
        public void RecordCertificateGeneration(double seconds, string type, bool success)
        {
            var status = success ? "success" : "failure";
            CertificateGenerationDuration.WithLabels(type, status).Observe(seconds);
            CertificateOperations.WithLabels("generate", status).Inc();
        }

        public void RecordCertificateValidation(bool valid, string type)
        {
            var validStr = valid ? "true" : "false";
            CertificateValidationTotal.WithLabels(validStr, type).Inc();
        }

        // Métodos para Health Checks
        public void RecordHealthCheckStatus(string checkName, bool healthy, string service = "default")
        {
            var status = healthy ? 1 : 0;
            HealthCheckStatus.WithLabels(checkName, service).Set(status);
            
            if (!healthy)
            {
                Logger.Warning("Health check falló: {CheckName} para servicio {Service}", checkName, service);
            }
        }

        public void RecordHealthCheckDuration(string checkName, double seconds, bool success)
        {
            var status = success ? "success" : "failure";
            HealthCheckDuration.WithLabels(checkName, status).Observe(seconds);
        }

        // Métodos para Base de Datos
        public void RecordDatabaseConnection(bool success, string database = "postgresql")
        {
            var status = success ? "success" : "failure";
            DatabaseConnections.WithLabels(status, database).Inc();
            
            if (!success)
            {
                Logger.Error("Error de conexión a base de datos: {Database}", database);
            }
        }

        public void RecordDatabaseQueryDuration(string queryType, string table, double seconds, bool success)
        {
            var status = success ? "success" : "failure";
            DatabaseQueryDuration.WithLabels(queryType, table, status).Observe(seconds);
            
            if (!success)
            {
                Logger.Error("Error en query de base de datos: {QueryType} en tabla {Table}", queryType, table);
            }
        }

        // Métodos para Auditoría
        public void RecordAuditLogOperation(string operationType, string entity, bool success)
        {
            var status = success ? "success" : "failure";
            AuditLogOperations.WithLabels(operationType, entity, status).Inc();
            
            Logger.Information("Operación de auditoría: {OperationType} en {Entity} - {Status}", 
                operationType, entity, status);
        }

        // Métodos de utilidad para actividades
        public Activity StartActivity(string name)
        {
            return new Activity(this, name);
        }

        public class Activity : IDisposable
        {
            private readonly MetricsCollector _collector;
            private readonly string _name;
            private readonly Stopwatch _stopwatch;
            private bool _disposed;

            public Activity(MetricsCollector collector, string name)
            {
                _collector = collector;
                _name = name;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _stopwatch.Stop();
                    var seconds = _stopwatch.Elapsed.TotalSeconds;
                    
                    // Registrar duración según el tipo de actividad
                    if (_name.StartsWith("podium"))
                    {
                        _collector.RecordPodiumQueryDuration(seconds, true);
                    }
                    else if (_name.StartsWith("certificate"))
                    {
                        _collector.RecordCertificateGeneration(seconds, "default", true);
                    }
                    else if (_name.StartsWith("health"))
                    {
                        _collector.RecordHealthCheckDuration(_name, seconds, true);
                    }
                    else if (_name.StartsWith("database"))
                    {
                        _collector.RecordDatabaseQueryDuration(_name, "general", seconds, true);
                    }
                    
                    Logger.Debug("Actividad {Activity} completada en {Duration}ms", 
                        _name, _stopwatch.ElapsedMilliseconds);
                    
                    _disposed = true;
                }
            }
        }

        // Métodos de inicialización y configuración
        public static void ConfigureMetrics()
        {
            // Configurar recolector de métricas por defecto
            Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>
            {
                { "service", "congreso-api" },
                { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production" }
            });

            Logger.Information("Métricas configuradas exitosamente");
        }

        // Método para obtener métricas en formato Prometheus
        public string GetPrometheusMetrics()
        {
            try
            {
                // Exportar métricas en formato Prometheus
                var collectors = Metrics.DefaultRegistry.GetType()
                    .GetMethod("GetAll", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(Metrics.DefaultRegistry, null) as System.Collections.Generic.IEnumerable<object>;
                
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# HELP congreso_api_metrics Métricas del Congreso API");
                sb.AppendLine("# TYPE congreso_api_metrics gauge");
                
                // Métricas simples
                sb.AppendLine($"podium_cache_hits {PodiumCacheHits.Value}");
                sb.AppendLine($"podium_cache_misses {PodiumCacheMisses.Value}");
                sb.AppendLine($"podium_operations {PodiumOperations.Value}");
                sb.AppendLine($"certificate_operations {CertificateOperations.Value}");
                sb.AppendLine($"certificate_validations {CertificateValidationTotal.Value}");
                sb.AppendLine($"database_connections {DatabaseConnections.Value}");
                sb.AppendLine($"audit_operations {AuditLogOperations.Value}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error obteniendo métricas Prometheus");
                return "# Error obteniendo métricas\n";
            }
        }

        public static Dictionary<string, object> GetMetricsSnapshot()
        {
            var snapshot = new Dictionary<string, object>();
            
            try
            {
                // Crear snapshot simple de métricas
                snapshot["podium_cache_hits"] = PodiumCacheHits.Value;
                snapshot["podium_cache_misses"] = PodiumCacheMisses.Value;
                snapshot["podium_operations"] = PodiumOperations.Value;
                snapshot["certificate_operations"] = CertificateOperations.Value;
                snapshot["certificate_validations"] = CertificateValidationTotal.Value;
                snapshot["database_connections"] = DatabaseConnections.Value;
                snapshot["audit_operations"] = AuditLogOperations.Value;
                
                // Agregar health check status
                snapshot["health_check_status"] = "healthy";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error obteniendo snapshot de métricas");
            }
            
            return snapshot;
        }

        // Implementación de métodos HTTP para IMetricsCollector
        public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds)
        {
            HttpRequestsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();
            HttpRequestDuration.WithLabels(method, endpoint).Observe(durationSeconds);
            
            Logger.Debug("HTTP {Method} {Endpoint} {StatusCode} en {Duration}ms", 
                method, endpoint, statusCode, durationSeconds * 1000);
        }

        public void RecordApiError(string errorType, string endpoint)
        {
            ApiErrorsTotal.WithLabels(errorType, endpoint).Inc();
            Logger.Warning("API error: {ErrorType} en {Endpoint}", errorType, endpoint);
        }

        public void RecordJwtAuthFailure()
        {
            JwtAuthFailures.Inc();
            Logger.Warning("JWT authentication failure");
        }
    }
}