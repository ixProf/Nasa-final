namespace AstronautHealthMonitoring.Models.Enums;

/// <summary>
/// Unified 3-tier health evaluation and triage levels.
/// Routine: Within expected baseline/limits; no special action required.
/// MedicalAttention: Out of expected range; requires check, countermeasure, or monitor review.
/// Emergency: Threat to crew safety or life; immediate emergency protocol execution.
/// </summary>
public enum VitalSignStatus
{
    Routine = 1,
    MedicalAttention = 2,
    Emergency = 3
}

public enum AlertSeverity
{
    Routine = 1,
    MedicalAttention = 2,
    Emergency = 3
}

public enum AlertStatus
{
    Active = 1,
    Acknowledged = 2,
    Resolved = 3
}

public enum BodyPosition
{
    MicrogravityFloating = 1,
    Sitting = 2,
    Supine = 3,
    Standing = 4
}

public enum ActivityContext
{
    Resting = 1,
    DuringExercise = 2,
    PostExercise = 3,
    PreSleep = 4,
    PostSleep = 5,
    ExtravehicularActivity = 6
}

public enum DataQuality
{
    Valid = 1,
    Uncertain = 2,
    Invalid = 3
}

public enum MealContext
{
    Fasting = 1,
    PostMeal = 2,
    Random = 3
}

public enum ReferenceRangeScope
{
    EarthGeneral = 1,
    MissionApproved = 2
}

public enum MissionStatus
{
    Scheduled = 1,
    Active = 2,
    Completed = 3,
    Aborted = 4
}

public enum SleepQuality
{
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}

public enum RecommendationCategory
{
    Medical = 1,
    Physical = 2,
    Psychological = 3,
    Sleep = 4,
    Environmental = 5
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum RecommendationStatus
{
    Pending = 1,
    Acknowledged = 2,
    Completed = 3,
    Dismissed = 4
}

public enum EmergencySeverity
{
    Moderate = 1,
    Severe = 2,
    Critical = 3
}

public enum VitalSignDirection
{
    Low = 1,
    High = 2,
    Either = 3
}
