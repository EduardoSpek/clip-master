using ClipMaster.Domain.Entities;

namespace ClipMaster.Domain.Interfaces;

public interface IWebcamCaptureService : IDisposable
{
    bool IsCapturing { get; }
    Task StartAsync(int width, int height, CancellationToken ct);
    Task<MediaFrame> CaptureFrameAsync(CancellationToken ct);
    void Stop();
}
