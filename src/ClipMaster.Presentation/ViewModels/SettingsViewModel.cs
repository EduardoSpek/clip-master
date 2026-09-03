using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Presentation.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private AppSettings _settings;

    [ObservableProperty] private int _fps;
    [ObservableProperty] private int _width;
    [ObservableProperty] private int _height;
    [ObservableProperty] private int _clipDurationSeconds;
    [ObservableProperty] private bool _microphoneEnabled;
    [ObservableProperty] private int _directRecordingLimitMinutes;
    [ObservableProperty] private bool _isDarkMode;

    [ObservableProperty] private bool _webcamEnabled;
    [ObservableProperty] private int _webcamSize;
    [ObservableProperty] private int _webcamBorderThickness;
    [ObservableProperty] private string _webcamBorderColor = "#FF00FF00";

    [ObservableProperty] private string _hotkeyStartStop = "Ctrl+Alt+1";
    [ObservableProperty] private string _hotkeyClip = "Ctrl+Alt+2";
    [ObservableProperty] private string _hotkeyClipContinue = "Ctrl+Alt+3";
    [ObservableProperty] private string _hotkeyMinimize = "Ctrl+Alt+4";
    [ObservableProperty] private string _hotkeyWebcam = "Ctrl+Alt+5";
    [ObservableProperty] private string _hotkeyMic = "Ctrl+Alt+6";
    [ObservableProperty] private string _hotkeyOutput = "Ctrl+Alt+7";
    [ObservableProperty] private string _hotkeyClose = "Ctrl+Alt+8";

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Load();
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        Fps = _settings.Fps;
        Width = _settings.Width;
        Height = _settings.Height;
        ClipDurationSeconds = _settings.ClipDurationSeconds;
        MicrophoneEnabled = _settings.MicrophoneEnabled;
        DirectRecordingLimitMinutes = _settings.DirectRecordingLimitMinutes;
        IsDarkMode = _settings.IsDarkMode;

        WebcamEnabled = _settings.Webcam.Enabled;
        WebcamSize = _settings.Webcam.Size;
        WebcamBorderThickness = _settings.Webcam.BorderThickness;
        WebcamBorderColor = _settings.Webcam.BorderColor;

        HotkeyStartStop = _settings.Hotkeys.StartStopRecording;
        HotkeyClip = _settings.Hotkeys.ClipInstant;
        HotkeyClipContinue = _settings.Hotkeys.ClipAndContinue;
        HotkeyMinimize = _settings.Hotkeys.MinimizeRestore;
        HotkeyWebcam = _settings.Hotkeys.ToggleWebcam;
        HotkeyMic = _settings.Hotkeys.ToggleMicrophone;
        HotkeyOutput = _settings.Hotkeys.OpenOutputFolder;
        HotkeyClose = _settings.Hotkeys.CloseApp;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Fps = Fps;
        _settings.Width = Width;
        _settings.Height = Height;
        _settings.ClipDurationSeconds = ClipDurationSeconds;
        _settings.MicrophoneEnabled = MicrophoneEnabled;
        _settings.DirectRecordingLimitMinutes = DirectRecordingLimitMinutes;
        _settings.IsDarkMode = IsDarkMode;

        _settings.Webcam.Enabled = WebcamEnabled;
        _settings.Webcam.Size = WebcamSize;
        _settings.Webcam.BorderThickness = WebcamBorderThickness;
        _settings.Webcam.BorderColor = WebcamBorderColor;

        _settings.Hotkeys.StartStopRecording = HotkeyStartStop;
        _settings.Hotkeys.ClipInstant = HotkeyClip;
        _settings.Hotkeys.ClipAndContinue = HotkeyClipContinue;
        _settings.Hotkeys.MinimizeRestore = HotkeyMinimize;
        _settings.Hotkeys.ToggleWebcam = HotkeyWebcam;
        _settings.Hotkeys.ToggleMicrophone = HotkeyMic;
        _settings.Hotkeys.OpenOutputFolder = HotkeyOutput;
        _settings.Hotkeys.CloseApp = HotkeyClose;

        _settingsService.Save(_settings);
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        _settings = new AppSettings();
        LoadFromSettings();
    }
}
