using System;

namespace Congreso.Api.Infrastructure
{
    public interface IMetricsCollector
    {
        // Podium metrics
        void RecordPodiumQueryDuration(double seconds, bool success);
        void RecordCacheHit(string cacheType, string identifier);
        void RecordCacheMiss(string cacheType, string identifier);
        void RecordPodiumOperation(string operation, bool success);

        // Certificate metrics
        void RecordCertificateGeneration(double seconds, string type, bool success);
        void RecordCertificateValidation(bool valid, string type);

        // Health check metrics
        void RecordHealthCheckStatus(string checkName, bool healthy, string service = "default");
        void RecordHealthCheckDuration(string checkName, double seconds, bool success);

        // Database metrics
        void RecordDatabaseConnection(bool success, string database = "postgresql");
        void RecordDatabaseQueryDuration(string queryType, string table, double seconds, bool success);

        // Audit metrics
        void RecordAuditLogOperation(string operationType, string entity, bool success);

        // HTTP metrics (for middleware)
        void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds);
        void RecordApiError(string errorType, string endpoint = "");
        void RecordJwtAuthFailure();

        // Prometheus metrics export
        string GetPrometheusMetrics();

        // Utility methods
        MetricsCollector.Activity StartActivity(string name);
    }
}