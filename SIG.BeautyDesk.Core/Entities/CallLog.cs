namespace SIG.BeautyDesk.Core.Entities;

public sealed class CallLog
{
    public Guid Id { get; set; }

    public string CallSid { get; set; } = string.Empty;

    public string FromNumber { get; set; } = string.Empty;

    public string? RecordingUrl { get; set; }

    public int DurationSec { get; set; }

    public string? N8nWorkflowExecutionId { get; set; }

    public string? RawTranscriptJson { get; set; }
}
