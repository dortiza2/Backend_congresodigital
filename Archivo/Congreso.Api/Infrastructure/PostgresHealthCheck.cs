using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Congreso.Api.Infrastructure
{
    public class PostgresHealthCheck : IHealthCheck
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresHealthCheck(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy("Postgres is reachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Postgres check failed", ex);
            }
        }
    }
}