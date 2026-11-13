namespace AstronautHealthMonitoring.DTOs.Astronaut;

public class CreateAstronautRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string InitialPassword { get; set; } = string.Empty;
    public string AstronautCode { get; set; } = string.Empty;
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

