using System.Diagnostics;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Infrastructure.Settings;

public class FileManagerService : IFileManagerService
{
    private readonly string _outputDirectory;

    public string OutputDirectory => _outputDirectory;

    public FileManagerService(string? outputDirectory = null)
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "ClipMaster");
        _outputDirectory = outputDirectory ?? baseDir;
        EnsureOutputDirectory();
    }

    public void EnsureOutputDirectory()
    {
        Directory.CreateDirectory(_outputDirectory);
    }

    public string GenerateClipPath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(_outputDirectory, $"clip_{timestamp}.mp4");
    }

    public string GenerateRecordingPath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(_outputDirectory, $"recording_{timestamp}.mp4");
    }

    public string GenerateHybridPath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(_outputDirectory, $"hybrid_{timestamp}.mp4");
    }

    public void OpenOutputFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _outputDirectory,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}
