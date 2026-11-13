namespace AstronautHealthMonitoring.Services.Implementations;

/// <summary>Retry failed startup checks after transient database outages without restarting the process.</summary>
public class DatabaseRecoveryWorker(IServiceProvider services, DatabaseReadiness readiness,
    IConfiguration config, IWebHostEnvironment environment, ILogger<DatabaseRecoveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!readiness.Ready)
                await readiness.InitializeAsync(services, config, environment, logger, stoppingToken);
        }
    }
}
