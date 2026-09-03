using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Interfaces;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace ClipMaster.Infrastructure.Capture;

public class WasapiMicCaptureService : IAudioCaptureService
{
    private WasapiCapture? _capture;
    private bool _isCapturing;
    private readonly object _lock = new();
    private byte[] _latestSamples = Array.Empty<byte>();

    public bool IsCapturing => _isCapturing;

    public Task StartAsync(int sampleRate, int channels, CancellationToken ct)
    {
        if (_isCapturing) return Task.CompletedTask;

        try
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

            _capture = new WasapiCapture(device, false, 100);
            _capture.WaveFormat = new WaveFormat(sampleRate, 16, channels);

            _capture.DataAvailable += (s, e) =>
            {
                if (e.BytesRecorded > 0)
                {
                    var data = new byte[e.BytesRecorded];
                    Array.Copy(e.Buffer, data, e.BytesRecorded);
                    lock (_lock)
                    {
                        _latestSamples = data;
                    }
                }
            };

            _capture.StartRecording();
            _isCapturing = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível capturar áudio do microfone. Detalhes: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    public Task<byte[]> CaptureSamplesAsync(CancellationToken ct)
    {
        byte[] samples;
        lock (_lock)
        {
            samples = _latestSamples;
            _latestSamples = Array.Empty<byte>();
        }
        return Task.FromResult(samples);
    }

    public void Stop()
    {
        _isCapturing = false;
        _capture?.StopRecording();
    }

    public void Dispose()
    {
        Stop();
        _capture?.Dispose();
        _capture = null;
    }
}
