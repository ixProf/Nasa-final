namespace AstronautHealthMonitoring.Models.Backlog.Enums;

public enum BaselineType
{
    PreFlight = 1,
    InFlight = 2,
    Current = 3
}

public enum SymptomSeverity
{
    Mild = 1,
    Moderate = 2,
    Severe = 3
}

public enum ExamType
{
    Routine = 1,
    Ultrasound = 2,
    DXA = 3,
    OCT_Fundoscopy = 4,
    LabTests = 5,
    Audiometry = 6,
    VisualAcuity = 7
}

public enum MissionEnvironmentType
{
    LowEarthOrbit_ISS = 1,
    LunarOrbit_Gateway = 2,
    LunarSurface = 3,
    MarsTransit = 4,
    MarsSurface = 5
}

public enum CrewRole
{
    Commander = 1,
    Pilot = 2,
    FlightEngineer = 3,
    MissionSpecialist = 4,
    MedicalOfficer = 5
}

public enum FlightStatus
{
    Assigned = 1,
    ActiveFlight = 2,
    PostMission = 3,
    Standby = 4
}

public enum ActivityCategory
{
    Cardio = 1,
    Strength = 2,
    EVA = 3,
    Flexibility = 4
}

public enum ExerciseIntensity
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum TaskType
{
    HealthCheck = 1,
    Exercise = 2,
    Medication = 3,
    Survey = 4,
    Rest = 5
}

public enum DailyTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4
}

public enum CheckupType
{
    RoutineDaily = 1,
    WeeklyReview = 2,
    PostEVA = 3,
    Emergency = 4
}

public enum CheckupStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum EmergencyIncidentStatus
{
    Active = 1,
    UnderControl = 2,
    Resolved = 3
}

public enum UrgencyLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Immediate = 4
}

public enum SupportRequestStatus
{
    Open = 1,
    InProgress = 2,
    Answered = 3,
    Closed = 4
}

public enum SimulationDifficulty
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3
}

public enum ReportType
{
    DailySummary = 1,
    WeeklyAggregate = 2,
    MissionReadiness = 3
}

public enum ConnectionState
{
    Connected = 1,
    Intermittent = 2,
    Blackout = 3
}

