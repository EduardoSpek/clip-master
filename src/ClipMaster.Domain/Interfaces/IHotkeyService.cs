using ClipMaster.Domain.Entities;

namespace ClipMaster.Domain.Interfaces;

public interface IHotkeyService : IDisposable
{
    event EventHandler<string>? HotkeyPressed;
    void RegisterAll(HotkeySettings settings);
    void UnregisterAll();
    void UpdateKeybinding(string action, string keyBinding);
}
