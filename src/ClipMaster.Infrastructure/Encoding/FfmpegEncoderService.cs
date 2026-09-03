using System.Diagnostics;
using System.Text;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Infrastructure.Encoding;

public class FfmpegEncoderService : IVideoEncoderService
{
    private Process? _ffmpegProcess;
    private Stream? _videoInputStream;
    private Stream? _audioInputStream;
    private bool _isEncoding;
    private readonly object _videoLock = new();
    private readonly object _audioLock = new();

    public bool IsEncoding => _isEncoding;

    public Task StartEncodingAsync(string outputPath, int width, int height, int fps,
        bool includeAudio, int audioSampleRate, int audioChannels, CancellationToken ct)
    {
        if (_isEncoding) return Task.CompletedTask;

        var args = BuildFfmpegArgs(outputPath, width, height, fps, includeAudio, audioSampleRate, audioChannels);

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        _ffmpegProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Falha ao iniciar FFmpeg. Verifique se ffmpeg.exe está no PATH.");

        _videoInputStream = _ffmpegProcess.StandardInput.BaseStream;

        if (includeAudio)
        {
            _audioInputStream = _ffmpegProcess.StandardInput.BaseStream;
        }

        _isEncoding = true;

        return Task.CompletedTask;
    }

    private string BuildFfmpegArgs(string outputPath, int width, int height, int fps,
        bool includeAudio, int audioSampleRate, int audioChannels)
    {
        var sb = new StringBuilder();
        sb.Append("-y ");
        sb.Append("-f rawvideo ");
        sb.Append("-vcodec rawvideo ");
        sb.Append($"-pix_fmt bgra ");
        sb.Append($"-s {width}x{height} ");
        sb.Append($"-r {fps} ");
        sb.Append("-i pipe:0 ");

        if (includeAudio)
        {
            sb.Append("-f s16le ");
            sb.Append($"-ar {audioSampleRate} ");
            sb.Append($"-ac {audioChannels} ");
            sb.Append("-i pipe:0 ");
        }

        sb.Append("-c:v libx264 ");
        sb.Append("-preset ultrafast ");
        sb.Append("-tune zerolatency ");
        sb.Append("-pix_fmt yuv420p ");
        sb.Append("-crf 18 ");

        if (includeAudio)
        {
            sb.Append("-c:a aac ");
            sb.Append("-b:a 192k ");
            sb.Append("-shortest ");
        }

        sb.Append($"\"{outputPath}\"");

        return sb.ToString();
    }

    public async Task EncodeVideoFrameAsync(byte[] frameData, CancellationToken ct)
    {
        if (!_isEncoding || _videoInputStream == null) return;

        try
        {
            await _videoInputStream.WriteAsync(frameData, ct);
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    public async Task EncodeAudioSamplesAsync(byte[] samples, int sampleCount, CancellationToken ct)
    {
        if (!_isEncoding || _audioInputStream == null) return;

        try
        {
            await _audioInputStream.WriteAsync(samples, ct);
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    public Task StopEncodingAsync()
    {
        if (!_isEncoding) return Task.CompletedTask;

        _isEncoding = false;

        try
        {
            _videoInputStream?.Flush();
            _videoInputStream?.Close();
            _audioInputStream?.Close();

            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                _ffmpegProcess.StandardInput.Close();

                var exited = _ffmpegProcess.WaitForExit(10000);
                if (!exited)
                {
                    _ffmpegProcess.Kill();
                }
            }
        }
        catch { }
        finally
        {
            _ffmpegProcess?.Dispose();
            _ffmpegProcess = null;
            _videoInputStream?.Dispose();
            _videoInputStream = null;
            _audioInputStream?.Dispose();
            _audioInputStream = null;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopEncodingAsync().GetAwaiter().GetResult();
    }
}
