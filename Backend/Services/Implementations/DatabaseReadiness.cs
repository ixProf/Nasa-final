using AstronautHealthMonitoring.Data;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Services.Implementations;

public class DatabaseReadiness
{
    public bool Ready {get;private set;}
    public ApiFailure? Failure {get;private set;}
    public string[] PendingMigrations {get;private set;}=[];
    public async Task InitializeAsync(IServiceProvider services,IConfiguration config,IWebHostEnvironment environment,ILogger logger,CancellationToken ct=default)
    {
        try
        {
            using var scope=services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
            PendingMigrations=(await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
            // Schema changes are explicit opt-in, including local development.
            var migrate=config.GetValue("Database:AutoMigrate",false);
            if(PendingMigrations.Length>0 && migrate)
            {
                logger.LogInformation("Applying {Count} pending database migrations before accepting API requests",PendingMigrations.Length);
                await db.Database.MigrateAsync(ct);PendingMigrations=[];
            }
            Ready=PendingMigrations.Length==0;Failure=Ready?null:ApiFailure.Schema;
            if(!Ready)logger.LogError("Database schema is behind the application. Pending migrations: {Migrations}. Run dotnet ef database update.",string.Join(", ",PendingMigrations));
        }
        catch(Exception e)
        {
            Ready=false;Failure=ApiFailure.Classify(e);
            logger.LogError(e,"Database startup check failed: {Code}. {Message}",Failure.Code,Failure.Message);
        }
    }
}
