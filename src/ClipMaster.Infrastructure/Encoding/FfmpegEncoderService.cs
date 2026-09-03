using System.Diagnostics;
using System.Text;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Infrastructure.Encoding;

public class FfmpegEncoderService : IVideoEncoderService
{
    private Process? _ffmpegProcess;
    private Stream? _videoInputStream;
    private bool _isEncoding;
    private string? _outputPath;
    private string? _tempAudioPath;
    private bool _includeAudio;
    private int _audioSampleRate;
    private int _audioChannels;
    private int _width;
    private int _height;
    private int _fps;

    public bool IsEncoding => _isEncoding;

    public Task StartEncodingAsync(string outputPath, int width, int height, int fps,
        bool includeAudio, int audioSampleRate, int audioChannels, CancellationToken ct)
    {
        if (_isEncoding) return Task.CompletedTask;

        _outputPath = outputPath;
        _width = width;
        _height = height;
        _fps = fps;
        _includeAudio = includeAudio;
        _audioSampleRate = audioSampleRate;
        _audioChannels = audioChannels;

        if (includeAudio)
        {
            _tempAudioPath = Path.Combine(Path.GetTempPath(), $"clipmaster_audio_{Guid.NewGuid():N}.raw");
        }

        var args = BuildVideoArgs(outputPath, width, height, fps, includeAudio, audioSampleRate, audioChannels);

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
        _isEncoding = true;

        return Task.CompletedTask;
    }

    private string BuildVideoArgs(string outputPath, int width, int height, int fps,
        bool includeAudio, int audioSampleRate, int audioChannels)
    {
        var sb = new StringBuilder();
        sb.Append("-y ");
        sb.Append("-f rawvideo ");
        sb.Append("-vcodec rawvideo ");
        sb.Append("-pix_fmt bgra ");
        sb.Append($"-s {width}x{height} ");
        sb.Append($"-framerate {fps} ");
        sb.Append("-i pipe:0 ");

        if (includeAudio)
        {
            sb.Append("-f s16le ");
            sb.Append($"-ar {audioSampleRate} ");
            sb.Append($"-ac {audioChannels} ");
            sb.Append("-i pipe:3 ");
        }

        sb.Append("-c:v libx264 ");
        sb.Append("-preset ultrafast ");
        sb.Append("-tune zerolatency ");
        sb.Append("-pix_fmt yuv420p ");
        sb.Append("-crf 18 ");
        sb.Append("-vsync cfr ");

        if (includeAudio)
        {
            sb.Append("-c:a aac ");
            sb.Append("-b:a 192k ");
            sb.Append("-shortest ");
        }
        else
        {
            sb.Append("-an ");
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
        if (!_isEncoding || !_includeAudio || string.IsNullOrEmpty(_tempAudioPath)) return;

        try
        {
            await using var fs = new FileStream(_tempAudioPath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true);
            await fs.WriteAsync(samples, ct);
        }
        catch { }
    }

    public Task StopEncodingAsync()
    {
        if (!_isEncoding) return Task.CompletedTask;

        _isEncoding = false;

        try
        {
            _videoInputStream?.Flush();
            _videoInputStream?.Close();

            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                _ffmpegProcess.StandardInput.Close();
                var exited = _ffmpegProcess.WaitForExit(30000);
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
        }

        if (_includeAudio && !string.IsNullOrEmpty(_tempAudioPath) && !string.IsNullOrEmpty(_outputPath))
        {
            try
            {
                MuxAudioVideo(_outputPath, _tempAudioPath);
            }
            catch { }
            finally
            {
                try { File.Delete(_tempAudioPath); } catch { }
            }
        }

        return Task.CompletedTask;
    }

    private void MuxAudioVideo(string videoPath, string audioRawPath)
    {
        if (!File.Exists(audioRawPath) || new FileInfo(audioRawPath).Length == 0) return;

        var tempVideoOnly = videoPath + ".tmp.mp4";
        var audioPath = Path.ChangeExtension(videoPath, ".aac");

        try
        {
            if (File.Exists(videoPath))
            {
                File.Move(videoPath, tempVideoOnly, true);
            }

            var encodePsi = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-y -f s16le -ar {_audioSampleRate} -ac {_audioChannels} -i \"{audioRawPath}\" -c:a aac -b:a 192k \"{audioPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            var encodeProcess = Process.Start(encodePsi);
            encodeProcess?.WaitForExit(30000);

            if (encodeProcess != null && !encodeProcess.HasExited)
                encodeProcess.Kill();
            encodeProcess?.Dispose();

            if (!File.Exists(audioPath) || !File.Exists(tempVideoOnly)) return;

            var muxPsi = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-y -i \"{tempVideoOnly}\" -i \"{audioPath}\" -c:v copy -c:a aac -shortest \"{videoPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            var muxProcess = Process.Start(muxPsi);
            muxProcess?.WaitForExit(30000);

            if (muxProcess != null && !muxProcess.HasExited)
                muxProcess.Kill();
            muxProcess?.Dispose();
        }
        finally
        {
            try { if (File.Exists(tempVideoOnly)) File.Delete(tempVideoOnly); } catch { }
            try { if (File.Exists(audioPath)) File.Delete(audioPath); } catch { }
        }
    }

    public void Dispose()
    {
        StopEncodingAsync().GetAwaiter().GetResult();
    }
}
