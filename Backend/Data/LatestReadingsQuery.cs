using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Data;

public static class LatestReadingsQuery
{
    // Translates to NOT EXISTS in PostgreSQL. Only the latest rows cross the network.
    // Id breaks ties deterministically when a device submits equal timestamps.
    public static IQueryable<VitalSignReading> LatestPerMetric(this IQueryable<VitalSignReading> readings) =>
        readings.Where(r=>!readings.Any(newer=>newer.AstronautId==r.AstronautId && newer.VitalSignTypeId==r.VitalSignTypeId &&
            (newer.RecordedAt>r.RecordedAt || newer.RecordedAt==r.RecordedAt && newer.Id>r.Id)));
}
