using ClipMaster.Domain.Entities;

namespace ClipMaster.Domain.Interfaces;

public interface IAudioCaptureService : IDisposable
{
    bool IsCapturing { get; }
    Task StartAsync(int sampleRate, int channels, CancellationToken ct);
    Task<byte[]> CaptureSamplesAsync(CancellationToken ct);
    void Stop();
}
