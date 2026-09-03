namespace ClipMaster.Domain.Entities;

public class AppSettings
{
    public int Fps { get; set; } = 60;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int ClipDurationSeconds { get; set; } = 30;
    public bool MicrophoneEnabled { get; set; } = false;
    public int DirectRecordingLimitMinutes { get; set; } = 60;
    public bool IsDarkMode { get; set; } = true;

    public WebcamSettings Webcam { get; set; } = new();
    public HotkeySettings Hotkeys { get; set; } = new();
}

public class WebcamSettings
{
    public bool Enabled { get; set; } = false;
    public int Size { get; set; } = 200;
    public int BorderThickness { get; set; } = 3;
    public string BorderColor { get; set; } = "#FF00FF00";
    public double PositionX { get; set; } = 50;
    public double PositionY { get; set; } = 50;
}

public class HotkeySettings
{
    public string StartStopRecording { get; set; } = "Ctrl+Alt+1";
    public string ClipInstant { get; set; } = "Ctrl+Alt+2";
    public string ClipAndContinue { get; set; } = "Ctrl+Alt+3";
    public string MinimizeRestore { get; set; } = "Ctrl+Alt+4";
    public string ToggleWebcam { get; set; } = "Ctrl+Alt+5";
    public string ToggleMicrophone { get; set; } = "Ctrl+Alt+6";
    public string OpenOutputFolder { get; set; } = "Ctrl+Alt+7";
    public string CloseApp { get; set; } = "Ctrl+Alt+8";
}
