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

        var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

        var formats = new[]
        {
            new WaveFormat(sampleRate, 16, channels),
            new WaveFormat(48000, 16, channels),
            new WaveFormat(44100, 16, channels),
            new WaveFormat(48000, 16, 2),
            new WaveFormat(44100, 16, 2)
        };

        Exception? lastError = null;

        foreach (var fmt in formats)
        {
            try
            {
                _capture = new WasapiCapture(device, false, 100);
                _capture.WaveFormat = fmt;

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
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _capture?.Dispose();
                _capture = null;
            }
        }

        throw new InvalidOperationException(
            $"Não foi possível capturar áudio do microfone com nenhum formato suportado. Último erro: {lastError?.Message}", lastError);
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
        try
        {
            _capture?.StopRecording();
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        _capture?.Dispose();
        _capture = null;
    }
}
