using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.ReferenceRanges;

public class ReferenceRangeDto
{
    public int Id { get; set; }
    public int VitalSignTypeId { get; set; }
    public string VitalSignTypeCode { get; set; } = string.Empty;

    public double MinNormal { get; set; }
    public double MaxNormal { get; set; }

    public double? EmergencyBelow { get; set; }
    public double? EmergencyAbove { get; set; }

    public double? MinSecondaryNormal { get; set; }
    public double? MaxSecondaryNormal { get; set; }

    public double? SecondaryEmergencyBelow { get; set; }
    public double? SecondaryEmergencyAbove { get; set; }

    public string? ApplicableGender { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public string Source { get; set; } = string.Empty;
    public ReferenceRangeScope Scope { get; set; }
    public bool IsMissionApproved { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateReferenceRangeRequest
{
    public double MinNormal { get; set; }
    public double MaxNormal { get; set; }

    public double? EmergencyBelow { get; set; }
    public double? EmergencyAbove { get; set; }

    public double? MinSecondaryNormal { get; set; }
    public double? MaxSecondaryNormal { get; set; }

    public double? SecondaryEmergencyBelow { get; set; }
    public double? SecondaryEmergencyAbove { get; set; }

    public string Source { get; set; } = string.Empty;
    public ReferenceRangeScope Scope { get; set; }
    public bool IsMissionApproved { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

