using Flipit.Core;

namespace Flipit.Tray;

/// <summary>
/// Invisible Win32 window whose sole purpose is receiving WM_HOTKEY messages.
/// </summary>
internal sealed class HiddenMessageWindow : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    /// <summary>
    /// Pseudo parent handle that creates a message-only window (not visible,
    /// not enumerable via EnumWindows). See MSDN "Message-Only Windows".
    /// </summary>
    private static readonly IntPtr HWND_MESSAGE = new(-3);
    private readonly IHotkeyService _hotkeyService;

    public event EventHandler? HotkeyFired;

    public HiddenMessageWindow(IHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;

        // Create an invisible window to receive messages
        var cp = new CreateParams
        {
            Caption = "FlipitMessageWindow",
            Style = 0,
            ExStyle = 0,
            Parent = HWND_MESSAGE
        };
        CreateHandle(cp);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            _hotkeyService.ProcessWmHotkey((int)m.WParam);
            HotkeyFired?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }
}

