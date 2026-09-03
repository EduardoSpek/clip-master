using ClipMaster.Domain.Enums;

namespace ClipMaster.Domain.Entities;

public class Recording
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public RecordingState State { get; set; } = RecordingState.Idle;
    public CaptureMode Mode { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
}
