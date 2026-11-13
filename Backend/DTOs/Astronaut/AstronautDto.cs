namespace AstronautHealthMonitoring.DTOs.Astronaut;

public class AstronautDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string AstronautCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public string? EmergencyContactInfo { get; set; }
    public int? MissionId { get; set; }
    public string? MissionName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

