using AstronautHealthMonitoring.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AstronautHealthMonitoring.Services.Implementations;

/// <summary>
/// Probes relational database connectivity for the unauthenticated /health endpoint.
/// </summary>
public class DbHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    private readonly DatabaseReadiness? _readiness;
    public DbHealthCheck(AppDbContext dbContext,DatabaseReadiness? readiness=null)
    {
        _dbContext = dbContext;
        _readiness=readiness;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if(_readiness is {Ready:false}) return HealthCheckResult.Unhealthy(_readiness.Failure?.Code??"DATABASE_UNAVAILABLE");
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database connection nominal.")
                : HealthCheckResult.Unhealthy("Cannot connect to database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}

