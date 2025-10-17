using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Serilog;

namespace Congreso.Api.Infrastructure.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly IMetricsCollector _metrics;
        private readonly Serilog.ILogger _logger;

        public DatabaseHealthCheck(string connectionString, IMetricsCollector metrics)
        {
            _connectionString = connectionString;
            _metrics = metrics;
            _logger = Log.ForContext<DatabaseHealthCheck>();
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.Debug("Ejecutando health check de base de datos");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // Ejecutar una query simple para verificar conectividad
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);

                stopwatch.Stop();
                
                _metrics.RecordHealthCheckDuration("database", stopwatch.Elapsed.TotalSeconds, true);
                _metrics.RecordDatabaseConnection(true);
                _metrics.RecordHealthCheckStatus("database", true);

                _logger.Debug("Health check de base de datos completado exitosamente en {Duration}ms", 
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Healthy(
                    description: "Base de datos accesible y operativa",
                    data: new Dictionary<string, object>
                    {
                        { "response_time_ms", stopwatch.ElapsedMilliseconds },
                        { "database_type", "postgresql" },
                        { "connection_state", connection.State.ToString() }
                    });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _metrics.RecordHealthCheckDuration("database", stopwatch.Elapsed.TotalSeconds, false);
                _metrics.RecordDatabaseConnection(false);
                _metrics.RecordHealthCheckStatus("database", false);

                _logger.Error(ex, "Health check de base de datos falló después de {Duration}ms", 
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Unhealthy(
                    description: "Base de datos no accesible",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "response_time_ms", stopwatch.ElapsedMilliseconds },
                        { "error_type", ex.GetType().Name },
                        { "error_message", ex.Message }
                    });
            }
        }
    }

    public class DatabaseHealthCheckOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public bool RequireHealthy { get; set; } = true;
    }
}