namespace Flipit.Core;

/// <summary>
/// Manages registration of global hotkeys.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>Fired when the registered hotkey is pressed.</summary>
    event EventHandler HotkeyPressed;

    /// <summary>The currently active hotkey definition.</summary>
    HotkeyDefinition CurrentHotkey { get; }

    /// <summary>
    /// The window handle the hotkey is currently registered on.
    /// <see cref="IntPtr.Zero"/> when not registered.
    /// </summary>
    IntPtr RegisteredWindowHandle { get; }

    /// <summary>
    /// Registers the global hotkey using <see cref="CurrentHotkey"/>.
    /// Must be called on the UI thread.
    /// </summary>
    bool Register(IntPtr windowHandle);

    /// <summary>Unregisters the global hotkey.</summary>
    void Unregister(IntPtr windowHandle);

    /// <summary>
    /// Called by the message window when a WM_HOTKEY message is received.
    /// Fires <see cref="HotkeyPressed"/> if the id matches the registered hotkey.
    /// </summary>
    void ProcessWmHotkey(int id);

    /// <summary>Unregisters the old hotkey, applies <paramref name="newHotkey"/>, and
    /// re-registers on the given window handle.
    /// Returns <c>true</c> on success; on failure the previous hotkey is restored.
    /// </summary>
    bool ChangeHotkey(IntPtr windowHandle, HotkeyDefinition newHotkey);
}
