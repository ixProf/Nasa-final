namespace AstronautHealthMonitoring.Services.Implementations;

public class AssignmentReminderWorker(IServiceScopeFactory scopes, ILogger<AssignmentReminderWorker> logger, IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Reminders:Enabled", true)) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                if(scope.ServiceProvider.GetRequiredService<DatabaseReadiness>().Ready)
                    await scope.ServiceProvider.GetRequiredService<AssignmentReminderService>().DispatchDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogWarning(e, "Assignment reminders unavailable; check database connection and pending migrations"); }
            try { if (!await timer.WaitForNextTickAsync(stoppingToken)) break; }
            catch (OperationCanceledException) { break; }
        }
    }
}
