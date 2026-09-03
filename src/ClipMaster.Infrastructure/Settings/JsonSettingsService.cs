using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Infrastructure.Settings;

public class JsonSettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public JsonSettingsService(string? settingsPath = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "ClipMaster");
        Directory.CreateDirectory(dir);
        _settingsPath = settingsPath ?? Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
        }
    }
}
