using System.Runtime.InteropServices;
using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Infrastructure.Hotkeys;

public class GlobalHotkeyService : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_ALT = 0x0001;

    private readonly Dictionary<int, string> _hotkeyActions = new();
    private readonly Dictionary<string, int> _actionToId = new();
    private readonly nint _windowHandle;
    private bool _isRegistered;

    public event EventHandler<string>? HotkeyPressed;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    public GlobalHotkeyService(nint windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public void RegisterAll(HotkeySettings settings)
    {
        UnregisterAll();

        var bindings = new Dictionary<string, string>
        {
            ["StartStopRecording"] = settings.StartStopRecording,
            ["ClipInstant"] = settings.ClipInstant,
            ["ClipAndContinue"] = settings.ClipAndContinue,
            ["MinimizeRestore"] = settings.MinimizeRestore,
            ["ToggleWebcam"] = settings.ToggleWebcam,
            ["ToggleMicrophone"] = settings.ToggleMicrophone,
            ["OpenOutputFolder"] = settings.OpenOutputFolder,
            ["CloseApp"] = settings.CloseApp
        };

        int id = 1;
        foreach (var kvp in bindings)
        {
            if (ParseHotkey(kvp.Value, out var mod, out var vk))
            {
                if (RegisterHotKey(_windowHandle, id, mod, vk))
                {
                    _hotkeyActions[id] = kvp.Key;
                    _actionToId[kvp.Key] = id;
                }
            }
            id++;
        }

        _isRegistered = true;
    }

    public void UnregisterAll()
    {
        if (!_isRegistered) return;

        foreach (var id in _hotkeyActions.Keys)
        {
            UnregisterHotKey(_windowHandle, id);
        }

        _hotkeyActions.Clear();
        _actionToId.Clear();
        _isRegistered = false;
    }

    public void UpdateKeybinding(string action, string keyBinding)
    {
        if (_actionToId.TryGetValue(action, out var oldId))
        {
            UnregisterHotKey(_windowHandle, oldId);
            _hotkeyActions.Remove(oldId);
            _actionToId.Remove(action);
        }

        if (ParseHotkey(keyBinding, out var mod, out var vk))
        {
            var newId = _hotkeyActions.Count > 0 ? _hotkeyActions.Keys.Max() + 1 : 1;
            if (RegisterHotKey(_windowHandle, newId, mod, vk))
            {
                _hotkeyActions[newId] = action;
                _actionToId[action] = newId;
            }
        }
    }

    public bool ProcessMessage(nint lParam)
    {
        var id = lParam.ToInt32();
        if (_hotkeyActions.TryGetValue(id, out var action))
        {
            HotkeyPressed?.Invoke(this, action);
            return true;
        }
        return false;
    }

    private static bool ParseHotkey(string hotkey, out uint mod, out uint vk)
    {
        mod = 0;
        vk = 0;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                mod |= MOD_CONTROL;
            else if (trimmed.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                mod |= MOD_ALT;
            else if (uint.TryParse(trimmed, out var number) && number >= 1 && number <= 9)
                vk = 0x30 + number;
        }

        return mod != 0 && vk != 0;
    }

    public void Dispose()
    {
        UnregisterAll();
    }
}
