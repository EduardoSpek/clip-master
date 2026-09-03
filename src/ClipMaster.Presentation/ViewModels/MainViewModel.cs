using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClipMaster.Application.Services;
using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Enums;
using ClipMaster.Domain.Interfaces;
using ClipMaster.Infrastructure.Capture;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipMaster.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly RecordingManager _recordingManager;
    private readonly ISettingsService _settingsService;
    private readonly IFileManagerService _fileManager;
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _webcamTimer;

    private Views.WebcamOverlayWindow? _webcamWindow;
    private MfWebcamCaptureService? _webcamCapture;
    private CancellationTokenSource? _webcamCts;

    [ObservableProperty] private string _statusText = "Pronto";
    [ObservableProperty] private string _recordingTime = "00:00:00";
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isDirectRecording;
    [ObservableProperty] private bool _isHybridRecording;
    [ObservableProperty] private bool _isWebcamOn;
    [ObservableProperty] private bool _isMicOn;
    [ObservableProperty] private bool _isDarkMode = true;
    [ObservableProperty] private string _lastClipPath = string.Empty;
    [ObservableProperty] private string _clipDurationText = "30s";

    public ObservableCollection<string> RecentClips { get; } = new();

    public MainViewModel()
    {
        AppLog.Write("MainViewModel constructor START");
        _settingsService = new Infrastructure.Settings.JsonSettingsService();
        _fileManager = new Infrastructure.Settings.FileManagerService();
        var settings = _settingsService.Load();
        AppLog.Write($"Settings loaded: Fps={settings.Fps}, W={settings.Width}, H={settings.Height}, Mic={settings.MicrophoneEnabled}");

        AppLog.Write("Creating DxgiScreenCaptureService...");
        var screenCapture = new DxgiScreenCaptureService();
        AppLog.Write("DxgiScreenCaptureService created OK");

        AppLog.Write("Creating WasapiMicCaptureService...");
        var micCapture = new WasapiMicCaptureService();
        AppLog.Write("WasapiMicCaptureService created OK");

        AppLog.Write("Creating RecordingManager...");
        _recordingManager = new RecordingManager(
            screenCapture, null, micCapture, _fileManager, _settingsService,
            () => new Infrastructure.Encoding.FfmpegEncoderService());
        AppLog.Write("RecordingManager created OK");

        _recordingManager.StateChanged += OnStateChanged;
        _recordingManager.ClipSaved += OnClipSaved;
        _recordingManager.ErrorOccurred += OnError;
        _recordingManager.RecordingTimeUpdated += OnRecordingTimeUpdated;

        IsDarkMode = settings.IsDarkMode;
        IsMicOn = settings.MicrophoneEnabled;
        ClipDurationText = $"{settings.ClipDurationSeconds}s";

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uiTimer.Tick += (s, e) => { };
        _uiTimer.Start();

        _webcamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _webcamTimer.Tick += WebcamTimer_Tick;

        AppLog.Write("MainViewModel constructor DONE");
    }

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        AppLog.Write($"StartRecordingAsync called. IsRecording={IsRecording}");
        try
        {
            if (IsRecording)
            {
                AppLog.Write("Stopping direct recording...");
                var path = await _recordingManager.StopDirectRecordingAsync();
                AppLog.Write($"StopDirectRecording returned: {path}");
                if (!string.IsNullOrEmpty(path))
                {
                    StatusText = $"Gravação salva: {Path.GetFileName(path)}";
                }
            }
            else
            {
                AppLog.Write("Starting continuous capture...");
                await _recordingManager.StartContinuousCaptureAsync();
                AppLog.Write("Continuous capture started. Starting direct recording...");
                await _recordingManager.StartDirectRecordingAsync();
                AppLog.Write("Direct recording started OK");
                StatusText = "Gravando...";
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteError("StartRecordingAsync", ex);
            StatusText = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateClipAsync()
    {
        AppLog.Write("CreateClipAsync called");
        try
        {
            StatusText = "Criando clipe...";
            var path = await _recordingManager.CreateInstantClipAsync();
            AppLog.Write($"CreateInstantClip returned: '{path}'");
            if (!string.IsNullOrEmpty(path))
            {
                StatusText = $"Clipe salvo: {Path.GetFileName(path)}";
            }
            else
            {
                StatusText = "Clipe vazio - nenhum frame no buffer";
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteError("CreateClipAsync", ex);
            StatusText = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StartHybridAsync()
    {
        AppLog.Write($"StartHybridAsync called. IsHybrid={IsHybridRecording}");
        try
        {
            if (IsHybridRecording)
            {
                var path = await _recordingManager.StopHybridRecordingAsync();
                if (!string.IsNullOrEmpty(path))
                {
                    StatusText = $"Gravação híbrida salva: {Path.GetFileName(path)}";
                }
            }
            else
            {
                if (!_recordingManager.IsContinuousCaptureRunning)
                {
                    AppLog.Write("Starting continuous capture for hybrid...");
                    await _recordingManager.StartContinuousCaptureAsync();
                }
                AppLog.Write("Starting hybrid recording...");
                await _recordingManager.StartHybridRecordingAsync();
                AppLog.Write("Hybrid recording started OK");
                StatusText = "Gravação híbrida ativa...";
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteError("StartHybridAsync", ex);
            StatusText = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClipAndContinueAsync()
    {
        try
        {
            StatusText = "Criando clipe e continuando...";
            var path = await _recordingManager.ClipAndContinueAsync();
            if (!string.IsNullOrEmpty(path))
            {
                StatusText = $"Clipe híbrido salvo: {Path.GetFileName(path)}";
                RecentClips.Insert(0, path);
                if (RecentClips.Count > 10) RecentClips.RemoveAt(RecentClips.Count - 1);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleWebcam()
    {
        AppLog.Write($"ToggleWebcam called. Current={IsWebcamOn}");
        IsWebcamOn = !IsWebcamOn;

        if (IsWebcamOn)
        {
            StartWebcam();
        }
        else
        {
            StopWebcam();
        }
    }

    private void StartWebcam()
    {
        AppLog.Write("StartWebcam START");
        try
        {
            var settings = _settingsService.Load();

            AppLog.Write("Finding FFmpeg...");
            var ffmpegPath = FindFfmpeg();
            AppLog.Write($"FFmpeg found: {ffmpegPath ?? "NULL"}");
            if (ffmpegPath == null)
            {
                StatusText = "Webcam: FFmpeg não encontrado no PATH";
                IsWebcamOn = false;
                return;
            }

            _webcamCts = new CancellationTokenSource();
            _webcamCapture = new MfWebcamCaptureService();
            AppLog.Write("MfWebcamCaptureService created");

            AppLog.Write("Creating WebcamOverlayWindow...");
            _webcamWindow = new Views.WebcamOverlayWindow();
            _webcamWindow.SetSize(settings.Webcam.Size);
            _webcamWindow.SetBorderColor(settings.Webcam.BorderColor);
            _webcamWindow.Show();
            AppLog.Write("WebcamOverlayWindow shown");

            AppLog.Write("Starting webcam capture 320x240...");
            _webcamCapture.StartAsync(320, 240, _webcamCts.Token).Wait(3000);
            AppLog.Write($"Webcam IsCapturing={_webcamCapture.IsCapturing}");

            _webcamTimer.Start();
            StatusText = "Webcam ligada";
        }
        catch (Exception ex)
        {
            AppLog.WriteError("StartWebcam", ex);
            StatusText = $"Erro webcam: {ex.Message}";
            StopWebcam();
        }
    }

    private void StopWebcam()
    {
        _webcamTimer.Stop();
        _webcamCts?.Cancel();
        _webcamCts?.Dispose();
        _webcamCts = null;
        _webcamCapture?.Stop();
        _webcamCapture?.Dispose();
        _webcamCapture = null;
        if (_webcamWindow != null)
        {
            _webcamWindow.Close();
            _webcamWindow = null;
        }
    }

    private async void WebcamTimer_Tick(object? sender, EventArgs e)
    {
        if (_webcamCapture == null || _webcamWindow == null) return;
        try
        {
            if (_webcamCapture.IsCapturing)
            {
                var ct = _webcamCts?.Token ?? CancellationToken.None;
                var frame = await _webcamCapture.CaptureFrameAsync(ct);
                if (frame.VideoData.Length > 0)
                {
                    _webcamWindow.UpdateFrame(frame.VideoData, frame.Width, frame.Height);
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task ToggleMicrophoneAsync()
    {
        AppLog.Write($"ToggleMicrophone called. Current={IsMicOn}");
        try
        {
            IsMicOn = !IsMicOn;
            var settings = _settingsService.Load();
            settings.MicrophoneEnabled = IsMicOn;
            _settingsService.Save(settings);

            if (IsMicOn)
            {
                AppLog.Write("Enabling microphone...");
                if (_recordingManager.IsContinuousCaptureRunning)
                {
                    AppLog.Write("Recording running, enabling mic now...");
                    await _recordingManager.EnableMicrophoneAsync();
                }
                else
                {
                    AppLog.Write("Recording not running, mic will start when recording begins");
                }
                StatusText = "Microfone ligado";
            }
            else
            {
                if (_recordingManager.IsContinuousCaptureRunning)
                {
                    _recordingManager.DisableMicrophone();
                }
                StatusText = "Microfone desligado";
            }
            AppLog.Write($"ToggleMicrophone DONE. IsMicOn={IsMicOn}");
        }
        catch (Exception ex)
        {
            AppLog.WriteError("ToggleMicrophone", ex);
            IsMicOn = false;
            StatusText = $"Erro microfone: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenOutputFolder()
    {
        _fileManager.OpenOutputFolder();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        var settings = _settingsService.Load();
        settings.IsDarkMode = IsDarkMode;
        _settingsService.Save(settings);

        var app = System.Windows.Application.Current;
        if (app != null)
        {
            var themeDict = app.Resources.MergedDictionaries
                .OfType<ResourceDictionary>()
                .FirstOrDefault(d => d.Source?.ToString().Contains("Theme") == true);

            if (themeDict != null)
            {
                var themeUri = IsDarkMode
                    ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                    : new Uri("pack://application:,,,/Themes/LightTheme.xaml");
                themeDict.Source = themeUri;
            }
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new Views.SettingsWindow(_settingsService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    private void MinimizeToTray()
    {
        if (System.Windows.Application.Current.MainWindow != null)
        {
            System.Windows.Application.Current.MainWindow.WindowState = WindowState.Minimized;
            System.Windows.Application.Current.MainWindow.ShowInTaskbar = false;
        }
    }

    [RelayCommand]
    private void CloseApp()
    {
        StopWebcam();
        _recordingManager.StopAll();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnStateChanged(object? sender, RecordingState state)
    {
        AppLog.Write($"OnStateChanged: {state}");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsRecording = state == RecordingState.Recording;
            IsDirectRecording = state == RecordingState.Recording;
            if (state == RecordingState.Idle)
            {
                IsHybridRecording = false;
            }
        });
    }

    private void OnClipSaved(object? sender, string path)
    {
        AppLog.Write($"OnClipSaved: {path}");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LastClipPath = path;
            RecentClips.Insert(0, path);
            if (RecentClips.Count > 10) RecentClips.RemoveAt(RecentClips.Count - 1);
        });
    }

    private void OnError(object? sender, string error)
    {
        AppLog.Write($"OnError: {error}");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = error;
        });
    }

    private void OnRecordingTimeUpdated(object? sender, TimeSpan time)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            RecordingTime = time.ToString(@"hh\:mm\:ss");
        });
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

    public void Dispose()
    {
        _uiTimer.Stop();
        _webcamTimer.Stop();
        StopWebcam();
        _recordingManager.StateChanged -= OnStateChanged;
        _recordingManager.ClipSaved -= OnClipSaved;
        _recordingManager.ErrorOccurred -= OnError;
        _recordingManager.RecordingTimeUpdated -= OnRecordingTimeUpdated;
        _recordingManager.Dispose();
    }
}
