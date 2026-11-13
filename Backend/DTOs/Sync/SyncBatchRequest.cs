using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.DTOs.VitalSigns;

namespace AstronautHealthMonitoring.DTOs.Sync;

public enum SyncItemStatus
{
    Accepted,
    Duplicate,
    Rejected
}

public class SyncItemResultDto
{
    public Guid ClientGeneratedId { get; set; }
    public string ItemType { get; set; } = string.Empty; // "VitalSign", "Sleep", "Mood", "Chat"
    public SyncItemStatus Status { get; set; }
    public string? Reason { get; set; }
    public int? ServerId { get; set; }
}

public class SyncBatchRequest
{
    public DateTime DeviceSyncTime { get; set; }

    public List<CreateVitalSignReadingRequest> VitalSignReadings { get; set; } = new();
    public List<CreateSleepRecordRequest> SleepRecords { get; set; } = new();
    public List<CreateMoodEntryRequest> MoodEntries { get; set; } = new();
    public List<SendChatMessageRequest> ChatMessages { get; set; } = new();
}

public class SyncBatchResponse
{
    public bool Success { get; set; }
    public int VitalSignReadingsProcessed { get; set; }
    public int SleepRecordsProcessed { get; set; }
    public int MoodEntriesProcessed { get; set; }
    public int ChatMessagesProcessed { get; set; }
    public int DuplicatesIgnoredCount { get; set; }
    public DateTime ServerSyncedAt { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<SyncItemResultDto> ItemResults { get; set; } = new();
}
