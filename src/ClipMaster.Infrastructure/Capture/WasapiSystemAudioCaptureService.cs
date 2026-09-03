using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Interfaces;
using NAudio.Wave;

namespace ClipMaster.Infrastructure.Capture;

public class WasapiSystemAudioCaptureService : IAudioCaptureService
{
    private WasapiLoopbackCapture? _capture;
    private bool _isCapturing;

    public bool IsCapturing => _isCapturing;

    public Task StartAsync(int sampleRate, int channels, CancellationToken ct)
    {
        if (_isCapturing) return Task.CompletedTask;

        try
        {
            _capture = new WasapiLoopbackCapture();
            _isCapturing = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível capturar áudio do sistema. Detalhes: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    public Task<byte[]> CaptureSamplesAsync(CancellationToken ct)
    {
        if (!_isCapturing || _capture == null)
            return Task.FromResult(Array.Empty<byte>());

        var tcs = new TaskCompletionSource<byte[]>();
        var capturedBytes = new List<byte>();

        void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded > 0)
            {
                var data = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, data, e.BytesRecorded);
                capturedBytes.AddRange(data);
            }
        }

        _capture.DataAvailable += OnDataAvailable;

        var waveFormat = _capture.WaveFormat;

        var samples = capturedBytes.ToArray();
        _capture.DataAvailable -= OnDataAvailable;

        return Task.FromResult(samples);
    }

    public WaveFormat? GetWaveFormat() => _capture?.WaveFormat;

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
