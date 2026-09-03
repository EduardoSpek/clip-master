using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Enums;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Application.Services;

public class RecordingManager : IDisposable
{
    private readonly IScreenCaptureService _screenCapture;
    private readonly IAudioCaptureService? _systemAudio;
    private readonly IAudioCaptureService? _micAudio;
    private readonly IFileManagerService _fileManager;
    private readonly ISettingsService _settingsService;
    private readonly Func<IVideoEncoderService> _encoderFactory;

    private readonly SynchronizedCircularBuffer<TimestampedFrame> _frameBuffer;
    private readonly SynchronizedCircularBuffer<TimestampedFrame> _audioBuffer;

    private CancellationTokenSource? _cts;
    private Task? _captureTask;
    private Recording? _currentRecording;
    private bool _isDirectRecording;
    private bool _isHybridMode;
    private string? _currentDirectOutputPath;
    private IVideoEncoderService? _directEncoder;
    private DateTime _directRecordingStartTime;

    private readonly object _lock = new();

    public RecordingState State { get; private set; } = RecordingState.Idle;
    public Recording? CurrentRecording => _currentRecording;
    public event EventHandler<RecordingState>? StateChanged;
    public event EventHandler<string>? ClipSaved;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<TimeSpan>? RecordingTimeUpdated;

    public RecordingManager(
        IScreenCaptureService screenCapture,
        IAudioCaptureService? systemAudio,
        IAudioCaptureService? micAudio,
        IFileManagerService fileManager,
        ISettingsService settingsService,
        Func<IVideoEncoderService> encoderFactory,
        int bufferCapacitySeconds = 300)
    {
        _screenCapture = screenCapture;
        _systemAudio = systemAudio;
        _micAudio = micAudio;
        _fileManager = fileManager;
        _settingsService = settingsService;
        _encoderFactory = encoderFactory;

        var settings = settingsService.Load();
        var bufferCapacity = bufferCapacitySeconds * settings.Fps;
        _frameBuffer = new SynchronizedCircularBuffer<TimestampedFrame>(bufferCapacity);
        _audioBuffer = new SynchronizedCircularBuffer<TimestampedFrame>(bufferCapacity);
    }

    public async Task StartContinuousCaptureAsync()
    {
        if (State != RecordingState.Idle) return;

        var settings = _settingsService.Load();

        _cts = new CancellationTokenSource();

        await _screenCapture.StartAsync(settings.Width, settings.Height, settings.Fps, _cts.Token);

        if (settings.MicrophoneEnabled && _micAudio != null)
        {
            await _micAudio.StartAsync(44100, 1, _cts.Token);
        }

        _captureTask = Task.Run(() => CaptureLoopAsync(_cts.Token));

        State = RecordingState.Recording;
        StateChanged?.Invoke(this, State);
    }

    private async Task CaptureLoopAsync(CancellationToken ct)
    {
        var settings = _settingsService.Load();
        var frameInterval = 1000.0 / settings.Fps;

        while (!ct.IsCancellationRequested)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var videoFrame = await _screenCapture.CaptureFrameAsync(ct);

                byte[] audioData = Array.Empty<byte>();
                int audioSampleCount = 0;

                if (settings.MicrophoneEnabled && _micAudio != null && _micAudio.IsCapturing)
                {
                    audioData = await _micAudio.CaptureSamplesAsync(ct);
                    audioSampleCount = audioData.Length / 2;
                }

                var frame = new TimestampedFrame
                {
                    TimestampMs = videoFrame.TimestampMs,
                    VideoData = videoFrame.VideoData,
                    AudioData = audioData,
                    Width = videoFrame.Width,
                    Height = videoFrame.Height,
                    AudioSampleCount = audioSampleCount
                };

                _frameBuffer.Add(frame);

                if (audioData.Length > 0)
                {
                    _audioBuffer.Add(frame);
                }

                if (_isDirectRecording && _directEncoder != null && _directEncoder.IsEncoding)
                {
                    await _directEncoder.EncodeVideoFrameAsync(videoFrame.VideoData, ct);
                    if (audioData.Length > 0)
                    {
                        await _directEncoder.EncodeAudioSamplesAsync(audioData, audioSampleCount, ct);
                    }

                    var elapsed = DateTime.Now - _directRecordingStartTime;
                    var limit = TimeSpan.FromMinutes(settings.DirectRecordingLimitMinutes);
                    if (elapsed >= limit)
                    {
                        await StopDirectRecordingAsync();
                    }

                    RecordingTimeUpdated?.Invoke(this, elapsed);
                }

                if (_isHybridMode && _directEncoder != null && _directEncoder.IsEncoding)
                {
                    await _directEncoder.EncodeVideoFrameAsync(videoFrame.VideoData, ct);
                    if (audioData.Length > 0)
                    {
                        await _directEncoder.EncodeAudioSamplesAsync(audioData, audioSampleCount, ct);
                    }

                    RecordingTimeUpdated?.Invoke(this, DateTime.Now - _directRecordingStartTime);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Erro na captura: {ex.Message}");
            }

            sw.Stop();
            var delay = frameInterval - sw.ElapsedMilliseconds;
            if (delay > 0)
            {
                await Task.Delay((int)delay, ct).ConfigureAwait(false);
            }
        }
    }

    public async Task<string> CreateInstantClipAsync()
    {
        var settings = _settingsService.Load();
        var framesNeeded = settings.ClipDurationSeconds * settings.Fps;
        var frames = _frameBuffer.GetLastNItems(framesNeeded);

        if (frames.Length == 0)
        {
            ErrorOccurred?.Invoke(this, "Nenhum frame disponível no buffer.");
            return string.Empty;
        }

        var outputPath = _fileManager.GenerateClipPath();
        var encoder = _encoderFactory();

        try
        {
            var includeAudio = settings.MicrophoneEnabled && frames.Any(f => f.AudioData.Length > 0);

            await encoder.StartEncodingAsync(
                outputPath,
                frames[0].Width,
                frames[0].Height,
                settings.Fps,
                includeAudio,
                44100,
                1,
                CancellationToken.None);

            foreach (var frame in frames)
            {
                if (frame.VideoData.Length > 0)
                    await encoder.EncodeVideoFrameAsync(frame.VideoData, CancellationToken.None);

                if (includeAudio && frame.AudioData.Length > 0)
                    await encoder.EncodeAudioSamplesAsync(frame.AudioData, frame.AudioSampleCount, CancellationToken.None);
            }

            await encoder.StopEncodingAsync();
            ClipSaved?.Invoke(this, outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Erro ao criar clipe: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            encoder.Dispose();
        }
    }

    public async Task StartDirectRecordingAsync()
    {
        if (State != RecordingState.Idle) return;

        var settings = _settingsService.Load();
        var outputPath = _fileManager.GenerateRecordingPath();

        _directEncoder = _encoderFactory();
        var includeAudio = settings.MicrophoneEnabled;

        await _directEncoder.StartEncodingAsync(
            outputPath,
            settings.Width,
            settings.Height,
            settings.Fps,
            includeAudio,
            44100,
            1,
            CancellationToken.None);

        _isDirectRecording = true;
        _currentDirectOutputPath = outputPath;
        _directRecordingStartTime = DateTime.Now;

        _currentRecording = new Recording
        {
            StartTime = DateTime.Now,
            Mode = CaptureMode.DirectRecording,
            OutputPath = outputPath
        };

        State = RecordingState.Recording;
        StateChanged?.Invoke(this, State);
    }

    public async Task<string> StopDirectRecordingAsync()
    {
        if (!_isDirectRecording || _directEncoder == null) return string.Empty;

        _isDirectRecording = false;
        var outputPath = _currentDirectOutputPath ?? string.Empty;

        await _directEncoder.StopEncodingAsync();
        _directEncoder.Dispose();
        _directEncoder = null;

        if (_currentRecording != null)
        {
            _currentRecording.EndTime = DateTime.Now;
            _currentRecording.State = RecordingState.Idle;
        }

        State = RecordingState.Idle;
        StateChanged?.Invoke(this, State);

        return outputPath;
    }

    public async Task StartHybridRecordingAsync()
    {
        if (State != RecordingState.Idle) return;

        var settings = _settingsService.Load();
        var outputPath = _fileManager.GenerateHybridPath();

        _directEncoder = _encoderFactory();
        var includeAudio = settings.MicrophoneEnabled;

        await _directEncoder.StartEncodingAsync(
            outputPath,
            settings.Width,
            settings.Height,
            settings.Fps,
            includeAudio,
            44100,
            1,
            CancellationToken.None);

        _isHybridMode = true;
        _currentDirectOutputPath = outputPath;
        _directRecordingStartTime = DateTime.Now;

        _currentRecording = new Recording
        {
            StartTime = DateTime.Now,
            Mode = CaptureMode.HybridClipAndRecord,
            OutputPath = outputPath
        };

        State = RecordingState.Recording;
        StateChanged?.Invoke(this, State);
    }

    public async Task<string> ClipAndContinueAsync()
    {
        if (!_isHybridMode) return string.Empty;

        var settings = _settingsService.Load();
        var framesNeeded = settings.ClipDurationSeconds * settings.Fps;
        var frames = _frameBuffer.GetLastNItems(framesNeeded);

        if (frames.Length == 0)
        {
            ErrorOccurred?.Invoke(this, "Nenhum frame disponível no buffer para clipe híbrido.");
            return string.Empty;
        }

        var clipPath = _fileManager.GenerateClipPath();
        var clipEncoder = _encoderFactory();

        try
        {
            var includeAudio = settings.MicrophoneEnabled && frames.Any(f => f.AudioData.Length > 0);

            await clipEncoder.StartEncodingAsync(
                clipPath,
                frames[0].Width,
                frames[0].Height,
                settings.Fps,
                includeAudio,
                44100,
                1,
                CancellationToken.None);

            foreach (var frame in frames)
            {
                if (frame.VideoData.Length > 0)
                    await clipEncoder.EncodeVideoFrameAsync(frame.VideoData, CancellationToken.None);

                if (includeAudio && frame.AudioData.Length > 0)
                    await clipEncoder.EncodeAudioSamplesAsync(frame.AudioData, frame.AudioSampleCount, CancellationToken.None);
            }

            await clipEncoder.StopEncodingAsync();
            ClipSaved?.Invoke(this, clipPath);
            return clipPath;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Erro ao criar clipe híbrido: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            clipEncoder.Dispose();
        }
    }

    public async Task<string> StopHybridRecordingAsync()
    {
        if (!_isHybridMode || _directEncoder == null) return string.Empty;

        _isHybridMode = false;
        var outputPath = _currentDirectOutputPath ?? string.Empty;

        await _directEncoder.StopEncodingAsync();
        _directEncoder.Dispose();
        _directEncoder = null;

        if (_currentRecording != null)
        {
            _currentRecording.EndTime = DateTime.Now;
            _currentRecording.State = RecordingState.Idle;
        }

        State = RecordingState.Idle;
        StateChanged?.Invoke(this, State);

        return outputPath;
    }

    public void StopAll()
    {
        _cts?.Cancel();
        _captureTask?.Wait(2000);

        _isDirectRecording = false;
        _isHybridMode = false;

        _directEncoder?.Dispose();
        _directEncoder = null;

        _screenCapture.Stop();
        _systemAudio?.Stop();
        _micAudio?.Stop();

        State = RecordingState.Idle;
        StateChanged?.Invoke(this, State);
    }

    public void Dispose()
    {
        StopAll();
        _cts?.Dispose();
        _screenCapture.Dispose();
        _systemAudio?.Dispose();
        _micAudio?.Dispose();
    }
}
