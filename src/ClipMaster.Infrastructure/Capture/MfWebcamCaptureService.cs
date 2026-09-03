using System.Diagnostics;
using System.Runtime.InteropServices;
using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Infrastructure.Capture;

public class MfWebcamCaptureService : IWebcamCaptureService
{
    private Process? _captureProcess;
    private bool _isCapturing;
    private int _width;
    private int _height;

    public bool IsCapturing => _isCapturing;

    public Task StartAsync(int width, int height, CancellationToken ct)
    {
        if (_isCapturing) return Task.CompletedTask;

        _width = width;
        _height = height;

        try
        {
            var ffmpegPath = FindFfmpeg();
            if (ffmpegPath == null)
                throw new InvalidOperationException("FFmpeg não encontrado para captura de webcam.");

            var args = $"-f dshow -video_size {width}x{height} -framerate 30 " +
                       $"-i video=\"Integrated Camera\" -f rawvideo -pix_fmt bgr24 pipe:1";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _captureProcess = Process.Start(psi);
            _isCapturing = true;
        }
        catch
        {
            _isCapturing = false;
        }

        return Task.CompletedTask;
    }

    public async Task<MediaFrame> CaptureFrameAsync(CancellationToken ct)
    {
        if (!_isCapturing || _captureProcess == null)
            return new MediaFrame { Width = _width, Height = _height };

        var frameSize = _width * _height * 3;
        var buffer = new byte[frameSize];
        int totalRead = 0;

        try
        {
            var stream = _captureProcess.StandardOutput.BaseStream;
            while (totalRead < frameSize)
            {
                var read = await stream.ReadAsync(
                    buffer.AsMemory(totalRead, frameSize - totalRead), ct);
                if (read == 0) break;
                totalRead += read;
            }
        }
        catch
        {
            return new MediaFrame { Width = _width, Height = _height };
        }

        return new MediaFrame
        {
            TimestampMs = Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000),
            VideoData = buffer,
            Width = _width,
            Height = _height
        };
    }

    public void Stop()
    {
        _isCapturing = false;
        if (_captureProcess != null && !_captureProcess.HasExited)
        {
            try { _captureProcess.Kill(); } catch { }
        }
    }

    public void Dispose()
    {
        Stop();
        _captureProcess?.Dispose();
        _captureProcess = null;
    }

    private static string? FindFfmpeg()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(';'))
        {
            var ffmpegPath = Path.Combine(dir.Trim(), "ffmpeg.exe");
            if (File.Exists(ffmpegPath)) return ffmpegPath;
        }
        return null;
    }
}
