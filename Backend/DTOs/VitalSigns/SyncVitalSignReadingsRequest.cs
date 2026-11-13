namespace AstronautHealthMonitoring.DTOs.VitalSigns;

public class SyncVitalSignReadingsRequest
{
    public List<CreateVitalSignReadingRequest> Readings { get; set; } = new();
}

