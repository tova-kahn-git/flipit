using System.Runtime.InteropServices;

namespace Flipit.Infrastructure;

/// <summary>
/// All Win32 P/Invoke declarations in one place.
/// </summary>
internal static class NativeMethods
{
    // ── Hotkey ───────────────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ── SendInput ────────────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        internal uint type;
        internal InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] internal MOUSEINPUT mi;
        [FieldOffset(0)] internal KEYBDINPUT ki;
        [FieldOffset(0)] internal HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        internal ushort wVk;
        internal ushort wScan;
        internal uint dwFlags;
        internal uint time;
        internal IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        internal int dx, dy, mouseData;
        internal uint dwFlags, time;
        internal IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        internal uint uMsg;
        internal ushort wParamL, wParamH;
    }

    internal const uint INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    // Virtual key codes
    internal const ushort VK_CONTROL = 0x11;
    internal const ushort VK_SHIFT   = 0x10;
    internal const ushort VK_HOME    = 0x24;
    internal const ushort VK_END     = 0x23;
    internal const ushort VK_C       = 0x43;
    internal const ushort VK_V       = 0x56;
    internal const ushort VK_A       = 0x41;

    // ── Clipboard ────────────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    /// <summary>Returns the registered window-class name for the given window handle.</summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    /// <summary>Returns the thread and process IDs that created the specified window.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Retrieves information about the active window or a specified GUI thread.
    /// Used to find <see cref="GUITHREADINFO.hwndFocus"/> — the child control that
    /// actually has keyboard focus inside the foreground window (e.g. the Scintilla
    /// control inside a Notepad++ top-level window).
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct GUITHREADINFO
    {
        internal uint   cbSize;
        internal uint   flags;
        internal IntPtr hwndActive;
        /// <summary>The focused control within the active window's thread.</summary>
        internal IntPtr hwndFocus;
        internal IntPtr hwndCapture;
        internal IntPtr hwndMenuOwner;
        internal IntPtr hwndMoveSize;
        internal IntPtr hwndCaret;
        internal RECT   rcCaret;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct RECT { internal int Left, Top, Right, Bottom; }

    /// <summary>
    /// Returns a serial number that is incremented each time the clipboard contents change.
    /// Use this to detect whether a Ctrl+C actually copied something new.
    /// Returns 0 if the calling thread does not have the clipboard open or
    /// another thread has changed the clipboard since the last call.
    /// </summary>
    [DllImport("user32.dll")]
    internal static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalFree(IntPtr hMem);

    internal const uint CF_UNICODETEXT = 13;
    internal const uint GMEM_MOVEABLE  = 0x0002;

    // ── SendMessage ───────────────────────────────────────────────────────────

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // ── Scintilla API ─────────────────────────────────────────────────────────
    // These message IDs are stable across all Scintilla versions.
    // Used to query the editor's selection state without modifying it.

    /// <summary>Returns the position of the start of the selection (== end means no selection).</summary>
    internal const uint SCI_GETSELECTIONSTART = 2143;

    /// <summary>Returns the position of the end of the selection.</summary>
    internal const uint SCI_GETSELECTIONEND   = 2145;
}




