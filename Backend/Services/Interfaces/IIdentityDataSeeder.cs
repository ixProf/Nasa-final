namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for bootstrapping identity roles and demo users from secure configuration/user-secrets.
/// </summary>
public interface IIdentityDataSeeder
{
    /// <summary>
    /// Ensures standard roles ("Astronaut", "Monitor") and initial demo users exist.
    /// Passwords must be supplied via user-secrets or environment variables.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}

