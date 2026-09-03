namespace ClipMaster.Domain.Entities;

public class MediaFrame
{
    public long TimestampMs { get; set; }
    public byte[] VideoData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public int AudioSampleCount { get; set; }
}
