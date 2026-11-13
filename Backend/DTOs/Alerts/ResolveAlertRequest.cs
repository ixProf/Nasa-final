namespace AstronautHealthMonitoring.DTOs.Alerts;

/// <summary>
/// Payload for formally resolving a health safety alert by flight monitor staff.
/// </summary>
public class ResolveAlertRequest
{
    /// <summary>
    /// Clinical or operational notes detailing resolution countermeasures taken.
    /// </summary>
    public string? ResolutionNotes { get; set; }
}

