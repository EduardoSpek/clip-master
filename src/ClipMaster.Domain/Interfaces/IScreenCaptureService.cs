using ClipMaster.Domain.Entities;

namespace ClipMaster.Domain.Interfaces;

public interface IScreenCaptureService : IDisposable
{
    int Width { get; }
    int Height { get; }
    int Fps { get; }
    Task StartAsync(int width, int height, int fps, CancellationToken ct);
    Task<MediaFrame> CaptureFrameAsync(CancellationToken ct);
    void Stop();
}
