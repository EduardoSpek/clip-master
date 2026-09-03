using System.Diagnostics;
using System.IO;

namespace ClipMaster.Infrastructure;

public static class InfraLog
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "ClipMaster_debug.log");
    private static readonly object _lock = new();

    public static void Write(string message)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] [INFRA] {message}{Environment.NewLine}");
            }
        }
        catch { }
        Debug.WriteLine($"[INFRA] {message}");
    }

    public static void WriteError(string context, Exception ex)
    {
        Write($"{context}: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
            Write($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
}
