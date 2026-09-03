using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClipMaster.Application.Services;
using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Enums;
using ClipMaster.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipMaster.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly RecordingManager _recordingManager;
    private readonly ISettingsService _settingsService;
    private readonly IFileManagerService _fileManager;
    private readonly DispatcherTimer _uiTimer;

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
        _settingsService = new Infrastructure.Settings.JsonSettingsService();
        _fileManager = new Infrastructure.Settings.FileManagerService();
        var settings = _settingsService.Load();

        var screenCapture = new Infrastructure.Capture.DxgiScreenCaptureService();
        var micCapture = new Infrastructure.Capture.WasapiMicCaptureService();

        _recordingManager = new RecordingManager(
            screenCapture, null, micCapture, _fileManager, _settingsService,
            () => new Infrastructure.Encoding.FfmpegEncoderService());

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
    }

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        try
        {
            if (IsRecording)
            {
                var path = await _recordingManager.StopDirectRecordingAsync();
                if (!string.IsNullOrEmpty(path))
                {
                    StatusText = $"Gravação salva: {Path.GetFileName(path)}";
                }
            }
            else
            {
                await _recordingManager.StartContinuousCaptureAsync();
                await _recordingManager.StartDirectRecordingAsync();
                StatusText = "Gravando...";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateClipAsync()
    {
        try
        {
            StatusText = "Criando clipe...";
            var path = await _recordingManager.CreateInstantClipAsync();
            if (!string.IsNullOrEmpty(path))
            {
                StatusText = $"Clipe salvo: {Path.GetFileName(path)}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StartHybridAsync()
    {
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
                await _recordingManager.StartContinuousCaptureAsync();
                await _recordingManager.StartHybridRecordingAsync();
                StatusText = "Gravação híbrida ativa...";
            }
        }
        catch (Exception ex)
        {
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
        IsWebcamOn = !IsWebcamOn;
        StatusText = IsWebcamOn ? "Webcam ligada" : "Webcam desligada";
    }

    [RelayCommand]
    private void ToggleMicrophone()
    {
        IsMicOn = !IsMicOn;
        var settings = _settingsService.Load();
        settings.MicrophoneEnabled = IsMicOn;
        _settingsService.Save(settings);
        StatusText = IsMicOn ? "Microfone ligado" : "Microfone desligado";
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
        _recordingManager.StopAll();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnStateChanged(object? sender, RecordingState state)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsRecording = state == RecordingState.Recording;
            IsDirectRecording = state == RecordingState.Recording;
        });
    }

    private void OnClipSaved(object? sender, string path)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LastClipPath = path;
            RecentClips.Insert(0, path);
            if (RecentClips.Count > 10) RecentClips.RemoveAt(RecentClips.Count - 1);
        });
    }

    private void OnError(object? sender, string error)
    {
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

    public void Dispose()
    {
        _uiTimer.Stop();
        _recordingManager.StateChanged -= OnStateChanged;
        _recordingManager.ClipSaved -= OnClipSaved;
        _recordingManager.ErrorOccurred -= OnError;
        _recordingManager.RecordingTimeUpdated -= OnRecordingTimeUpdated;
        _recordingManager.Dispose();
    }
}
