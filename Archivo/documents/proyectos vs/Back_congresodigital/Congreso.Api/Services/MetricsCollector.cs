using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Congreso.Api.Services
{
    public interface IMetricsCollector
    {
        void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds);
        void RecordApiError(string errorType);
        void RecordJwtAuthFailure();
        void RecordRateLimitHit();
        void RecordRateLimitBlocked();
        void RecordCertificateGenerated();
        void RecordPodiumQueryDuration(double durationSeconds);
        string GetPrometheusMetrics();
        
        // QR Code metrics
        void TrackQrScan(bool success, bool secure);
        void TrackQrGeneration(bool success, bool secure);
        void TrackQrValidation(bool success);
        
        // Metrics snapshot
        MetricsSnapshot GetSnapshot();
    }
    
    public class MetricsSnapshot
    {
        public long TotalQrScans { get; set; }
        public long SuccessfulQrScans { get; set; }
        public long FailedQrScans { get; set; }
        public long SecureQrScans { get; set; }
        public long LegacyQrScans { get; set; }
        public long TotalQrGenerations { get; set; }
        public long SuccessfulQrGenerations { get; set; }
        public long FailedQrGenerations { get; set; }
        public long SecureQrGenerations { get; set; }
        public long LegacyQrGenerations { get; set; }
        public long TotalQrValidations { get; set; }
        public long SuccessfulQrValidations { get; set; }
        public long FailedQrValidations { get; set; }
    }

    public class MetricsCollector : IMetricsCollector
    {
        private readonly ILogger<MetricsCollector> _logger;
        
        // Contadores
        private readonly ConcurrentDictionary<string, long> _httpRequests = new();
        private readonly ConcurrentDictionary<string, long> _apiErrors = new();
        private long _jwtAuthFailures;
        private long _rateLimitHits;
        private long _rateLimitBlocked;
        private long _certificatesGenerated;
        private long _podiumQueryCount;
        private double _podiumQueryTotalDuration;
        
        // QR Code metrics
        private long _totalQrScans;
        private long _successfulQrScans;
        private long _failedQrScans;
        private long _secureQrScans;
        private long _legacyQrScans;
        private long _totalQrGenerations;
        private long _successfulQrGenerations;
        private long _failedQrGenerations;
        private long _secureQrGenerations;
        private long _legacyQrGenerations;
        private long _totalQrValidations;
        private long _successfulQrValidations;
        private long _failedQrValidations;
        
        // Métricas de duración
        private readonly ConcurrentDictionary<string, (long count, double totalDuration)> _requestDurations = new();

        public MetricsCollector(ILogger<MetricsCollector> logger)
        {
            _logger = logger;
        }

        public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds)
        {
            var key = $"{method}|{endpoint}|{statusCode}";
            _httpRequests.AddOrUpdate(key, 1, (_, count) => count + 1);
            
            var durationKey = $"{method}|{endpoint}";
            _requestDurations.AddOrUpdate(durationKey, (1, durationSeconds), (_, existing) =>
            {
                return (existing.count + 1, existing.totalDuration + durationSeconds);
            });
        }

        public void RecordApiError(string errorType)
        {
            _apiErrors.AddOrUpdate(errorType, 1, (_, count) => count + 1);
        }

        public void RecordJwtAuthFailure()
        {
            Interlocked.Increment(ref _jwtAuthFailures);
        }

        public void RecordRateLimitHit()
        {
            Interlocked.Increment(ref _rateLimitHits);
        }

        public void RecordRateLimitBlocked()
        {
            Interlocked.Increment(ref _rateLimitBlocked);
        }

        public void RecordCertificateGenerated()
        {
            Interlocked.Increment(ref _certificatesGenerated);
        }

        public void RecordPodiumQueryDuration(double durationSeconds)
        {
            Interlocked.Increment(ref _podiumQueryCount);
            lock (this)
            {
                _podiumQueryTotalDuration += durationSeconds;
            }
        }

        public string GetPrometheusMetrics()
        {
            var sb = new StringBuilder();
            
            // Métrica: http_requests_total
            sb.AppendLine("# HELP congreso_http_requests_total Total number of HTTP requests");
            sb.AppendLine("# TYPE congreso_http_requests_total counter");
            foreach (var kvp in _httpRequests)
            {
                var parts = kvp.Key.Split('|');
                var method = parts[0];
                var endpoint = parts[1];
                var statusCode = parts[2];
                sb.AppendLine($"congreso_http_requests_total{{method=\"{method}\",endpoint=\"{endpoint}\",status=\"{statusCode}\"}} {kvp.Value}");
            }

            // Métrica: http_request_duration_seconds
            sb.AppendLine("# HELP congreso_http_request_duration_seconds HTTP request duration in seconds");
            sb.AppendLine("# TYPE congreso_http_request_duration_seconds summary");
            foreach (var kvp in _requestDurations)
            {
                var parts = kvp.Key.Split('|');
                var method = parts[0];
                var endpoint = parts[1];
                var avgDuration = kvp.Value.totalDuration / kvp.Value.count;
                sb.AppendLine($"congreso_http_request_duration_seconds{{method=\"{method}\",endpoint=\"{endpoint}\"}} {avgDuration:F3}");
            }

            // Métrica: api_errors_total
            sb.AppendLine("# HELP congreso_api_errors_total Total number of API errors by type");
            sb.AppendLine("# TYPE congreso_api_errors_total counter");
            foreach (var kvp in _apiErrors)
            {
                sb.AppendLine($"congreso_api_errors_total{{type=\"{kvp.Key}\"}} {kvp.Value}");
            }

            // Métrica: jwt_auth_failures_total
            sb.AppendLine("# HELP congreso_jwt_auth_failures_total Total number of JWT authentication failures");
            sb.AppendLine("# TYPE congreso_jwt_auth_failures_total counter");
            sb.AppendLine($"congreso_jwt_auth_failures_total {Volatile.Read(ref _jwtAuthFailures)}");

            // Métrica: rate_limit_hits_total
            sb.AppendLine("# HELP congreso_rate_limit_hits_total Total number of rate limit hits");
            sb.AppendLine("# TYPE congreso_rate_limit_hits_total counter");
            sb.AppendLine($"congreso_rate_limit_hits_total {Volatile.Read(ref _rateLimitHits)}");

            // Métrica: rate_limit_blocked_total
            sb.AppendLine("# HELP congreso_rate_limit_blocked_total Total number of rate limit blocked requests");
            sb.AppendLine("# TYPE congreso_rate_limit_blocked_total counter");
            sb.AppendLine($"congreso_rate_limit_blocked_total {Volatile.Read(ref _rateLimitBlocked)}");

            // Métrica: certificates_generated_total
            sb.AppendLine("# HELP congreso_certificates_generated_total Total number of certificates generated");
            sb.AppendLine("# TYPE congreso_certificates_generated_total counter");
            sb.AppendLine($"congreso_certificates_generated_total {Volatile.Read(ref _certificatesGenerated)}");

            // Métrica: podium_query_duration_seconds
            sb.AppendLine("# HELP congreso_podium_query_duration_seconds Podium query duration in seconds");
            sb.AppendLine("# TYPE congreso_podium_query_duration_seconds summary");
            var podiumCount = Volatile.Read(ref _podiumQueryCount);
            var podiumTotalDuration = _podiumQueryTotalDuration;
            if (podiumCount > 0)
            {
                var avgDuration = podiumTotalDuration / podiumCount;
                sb.AppendLine($"congreso_podium_query_duration_seconds_count {podiumCount}");
                sb.AppendLine($"congreso_podium_query_duration_seconds_sum {podiumTotalDuration:F3}");
                sb.AppendLine($"congreso_podium_query_duration_seconds {avgDuration:F3}");
            }
            else
            {
                sb.AppendLine("congreso_podium_query_duration_seconds_count 0");
                sb.AppendLine("congreso_podium_query_duration_seconds_sum 0");
                sb.AppendLine("congreso_podium_query_duration_seconds 0");
            }

            // QR Code metrics
            sb.AppendLine("# HELP congreso_qr_scans_total Total number of QR code scans");
            sb.AppendLine("# TYPE congreso_qr_scans_total counter");
            sb.AppendLine($"congreso_qr_scans_total {Volatile.Read(ref _totalQrScans)}");
            sb.AppendLine($"congreso_qr_scans_successful_total {Volatile.Read(ref _successfulQrScans)}");
            sb.AppendLine($"congreso_qr_scans_failed_total {Volatile.Read(ref _failedQrScans)}");
            sb.AppendLine($"congreso_qr_scans_secure_total {Volatile.Read(ref _secureQrScans)}");
            sb.AppendLine($"congreso_qr_scans_legacy_total {Volatile.Read(ref _legacyQrScans)}");

            sb.AppendLine("# HELP congreso_qr_generations_total Total number of QR code generations");
            sb.AppendLine("# TYPE congreso_qr_generations_total counter");
            sb.AppendLine($"congreso_qr_generations_total {Volatile.Read(ref _totalQrGenerations)}");
            sb.AppendLine($"congreso_qr_generations_successful_total {Volatile.Read(ref _successfulQrGenerations)}");
            sb.AppendLine($"congreso_qr_generations_failed_total {Volatile.Read(ref _failedQrGenerations)}");
            sb.AppendLine($"congreso_qr_generations_secure_total {Volatile.Read(ref _secureQrGenerations)}");
            sb.AppendLine($"congreso_qr_generations_legacy_total {Volatile.Read(ref _legacyQrGenerations)}");

            sb.AppendLine("# HELP congreso_qr_validations_total Total number of QR code validations");
            sb.AppendLine("# TYPE congreso_qr_validations_total counter");
            sb.AppendLine($"congreso_qr_validations_total {Volatile.Read(ref _totalQrValidations)}");
            sb.AppendLine($"congreso_qr_validations_successful_total {Volatile.Read(ref _successfulQrValidations)}");
            sb.AppendLine($"congreso_qr_validations_failed_total {Volatile.Read(ref _failedQrValidations)}");

            return sb.ToString();
        }
        
        public void TrackQrScan(bool success, bool secure)
        {
            Interlocked.Increment(ref _totalQrScans);
            if (success)
            {
                Interlocked.Increment(ref _successfulQrScans);
            }
            else
            {
                Interlocked.Increment(ref _failedQrScans);
            }
            
            if (secure)
            {
                Interlocked.Increment(ref _secureQrScans);
            }
            else
            {
                Interlocked.Increment(ref _legacyQrScans);
            }
        }
        
        public void TrackQrGeneration(bool success, bool secure)
        {
            Interlocked.Increment(ref _totalQrGenerations);
            if (success)
            {
                Interlocked.Increment(ref _successfulQrGenerations);
            }
            else
            {
                Interlocked.Increment(ref _failedQrGenerations);
            }
            
            if (secure)
            {
                Interlocked.Increment(ref _secureQrGenerations);
            }
            else
            {
                Interlocked.Increment(ref _legacyQrGenerations);
            }
        }
        
        public void TrackQrValidation(bool success)
        {
            Interlocked.Increment(ref _totalQrValidations);
            if (success)
            {
                Interlocked.Increment(ref _successfulQrValidations);
            }
            else
            {
                Interlocked.Increment(ref _failedQrValidations);
            }
        }
        
        public MetricsSnapshot GetSnapshot()
        {
            return new MetricsSnapshot
            {
                TotalQrScans = Volatile.Read(ref _totalQrScans),
                SuccessfulQrScans = Volatile.Read(ref _successfulQrScans),
                FailedQrScans = Volatile.Read(ref _failedQrScans),
                SecureQrScans = Volatile.Read(ref _secureQrScans),
                LegacyQrScans = Volatile.Read(ref _legacyQrScans),
                TotalQrGenerations = Volatile.Read(ref _totalQrGenerations),
                SuccessfulQrGenerations = Volatile.Read(ref _successfulQrGenerations),
                FailedQrGenerations = Volatile.Read(ref _failedQrGenerations),
                SecureQrGenerations = Volatile.Read(ref _secureQrGenerations),
                LegacyQrGenerations = Volatile.Read(ref _legacyQrGenerations),
                TotalQrValidations = Volatile.Read(ref _totalQrValidations),
                SuccessfulQrValidations = Volatile.Read(ref _successfulQrValidations),
                FailedQrValidations = Volatile.Read(ref _failedQrValidations)
            };
        }
    }

    // Clase auxiliar para extensión de servicios
    public static class MetricsCollectorExtensions
    {
        public static void RecordRequestMetrics(this IMetricsCollector metrics, string method, string endpoint, int statusCode, double durationSeconds)
        {
            metrics.RecordHttpRequest(method, endpoint, statusCode, durationSeconds);
            
            // Registrar errores específicos
            if (statusCode >= 500)
            {
                metrics.RecordApiError($"server_error_{statusCode}");
            }
            else if (statusCode >= 400)
            {
                metrics.RecordApiError($"client_error_{statusCode}");
            }
        }

        public static void RecordAuthMetrics(this IMetricsCollector metrics, bool success)
        {
            if (!success)
            {
                metrics.RecordJwtAuthFailure();
            }
        }

        public static void RecordRateLimitMetrics(this IMetricsCollector metrics, bool blocked)
        {
            metrics.RecordRateLimitHit();
            if (blocked)
            {
                metrics.RecordRateLimitBlocked();
            }
        }
    }
}