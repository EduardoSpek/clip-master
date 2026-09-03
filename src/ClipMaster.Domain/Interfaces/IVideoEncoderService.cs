namespace ClipMaster.Domain.Interfaces;

public interface IVideoEncoderService : IDisposable
{
    bool IsEncoding { get; }
    Task StartEncodingAsync(string outputPath, int width, int height, int fps,
        bool includeAudio, int audioSampleRate, int audioChannels, CancellationToken ct);
    Task EncodeVideoFrameAsync(byte[] frameData, CancellationToken ct);
    Task EncodeAudioSamplesAsync(byte[] samples, int sampleCount, CancellationToken ct);
    Task StopEncodingAsync();
}
