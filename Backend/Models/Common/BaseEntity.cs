namespace AstronautHealthMonitoring.Models.Common;

/// <summary>
/// Common base class for all persistent database entities.
/// Provides a unified identity and audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

