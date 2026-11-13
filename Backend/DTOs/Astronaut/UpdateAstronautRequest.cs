namespace AstronautHealthMonitoring.DTOs.Astronaut;

public class UpdateAstronautRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public string? EmergencyContactInfo { get; set; }
    public int? MissionId { get; set; }
}

