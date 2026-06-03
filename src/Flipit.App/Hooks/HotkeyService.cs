using System.Runtime.InteropServices;
using Flipit.Core;
using Flipit.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Flipit.Hooks;

/// <summary>
/// Registers and manages a configurable global hotkey using Win32 RegisterHotKey.
/// The active hotkey is read from <see cref="IAppSettings.Hotkey"/> at registration time
/// and can be changed at runtime via <see cref="ChangeHotkey"/>.
/// Must be used from the UI thread (message loop required).
/// </summary>
public sealed class HotkeyService : IHotkeyService
{
    private const int HotkeyId   = 0x42F1; // Arbitrary unique app ID
    private const int WM_HOTKEY  = 0x0312;

    private readonly ILogger<HotkeyService> _logger;
    private readonly IAppSettings _settings;

    private HotkeyDefinition _currentHotkey;
    private IntPtr _registeredHandle = IntPtr.Zero;
    private bool _registered;
    private bool _disposed;

    public event EventHandler HotkeyPressed = delegate { };

    public HotkeyDefinition CurrentHotkey      => _currentHotkey;
    public IntPtr RegisteredWindowHandle       => _registeredHandle;

    public HotkeyService(ILogger<HotkeyService> logger, IAppSettings settings)
    {
        _logger = logger;
        _settings = settings;
        _currentHotkey = settings.Hotkey;
    }

    // ── Registration ─────────────────────────────────────────────────────────

    public bool Register(IntPtr windowHandle)
    {
        if (_registered) return true;

        _currentHotkey = _settings.Hotkey; // Sync with persisted value on startup
        bool success = NativeMethods.RegisterHotKey(
            windowHandle, HotkeyId,
            _currentHotkey.Win32Modifiers,
            _currentHotkey.Win32VirtualKey);

        if (success)
        {
            _registered = true;
            _registeredHandle = windowHandle;
            _logger.LogInformation(
                "Global hotkey registered: {Hotkey} (handle={Handle})",
                _currentHotkey.DisplayText, windowHandle);
        }
        else
        {
            int err = Marshal.GetLastWin32Error();
            _logger.LogWarning(
                "Failed to register hotkey '{Hotkey}'. Win32 error={Error}",
                _currentHotkey.DisplayText, err);
        }
        return success;
    }

    public void Unregister(IntPtr windowHandle)
    {
        if (!_registered) return;
        NativeMethods.UnregisterHotKey(windowHandle, HotkeyId);
        _registered = false;
        _registeredHandle = IntPtr.Zero;
        _logger.LogInformation("Global hotkey unregistered: {Hotkey}", _currentHotkey.DisplayText);
    }

    public bool ChangeHotkey(IntPtr windowHandle, HotkeyDefinition newHotkey)
    {
        var previous = _currentHotkey;

        _logger.LogInformation(
            "Changing hotkey from '{Old}' to '{New}'",
            previous.DisplayText, newHotkey.DisplayText);

        // Unregister the current hotkey first
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(windowHandle, HotkeyId);
            _registered = false;
        }

        // Attempt to register the new hotkey
        bool success = NativeMethods.RegisterHotKey(
            windowHandle, HotkeyId,
            newHotkey.Win32Modifiers,
            newHotkey.Win32VirtualKey);

        if (success)
        {
            _currentHotkey = newHotkey;
            _registered = true;
            _registeredHandle = windowHandle;
            _settings.Hotkey = newHotkey;
            _settings.Save();
            _logger.LogInformation("Hotkey changed successfully to '{Hotkey}'", newHotkey.DisplayText);
            return true;
        }

        // Registration failed — restore the previous hotkey
        int err = Marshal.GetLastWin32Error();
        _logger.LogWarning(
            "Failed to register new hotkey '{New}' (Win32 error={Error}). Restoring '{Old}'.",
            newHotkey.DisplayText, err, previous.DisplayText);

        bool restored = NativeMethods.RegisterHotKey(
            windowHandle, HotkeyId,
            previous.Win32Modifiers,
            previous.Win32VirtualKey);

        if (restored)
        {
            _currentHotkey = previous;
            _registered = true;
            _logger.LogInformation("Previous hotkey '{Hotkey}' restored", previous.DisplayText);
        }
        else
        {
            _logger.LogError("Could not restore previous hotkey '{Hotkey}'", previous.DisplayText);
        }

        return false;
    }

    // ── Internal message-pump helpers ─────────────────────────────────────────

    /// <summary>Called by the message window when WM_HOTKEY arrives.</summary>
    internal void OnWmHotkey(int id)
    {
        if (id == HotkeyId)
            HotkeyPressed(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void ProcessWmHotkey(int id) => OnWmHotkey(id);

    internal static bool IsHotkeyMessage(Message m, out int id)
    {
        id = (int)m.WParam;
        return m.Msg == WM_HOTKEY;
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_registered && _registeredHandle != IntPtr.Zero)
            {
                NativeMethods.UnregisterHotKey(_registeredHandle, HotkeyId);
                _registered = false;
                _logger.LogInformation("HotkeyService disposed; hotkey unregistered");
            }
        }
    }
}

