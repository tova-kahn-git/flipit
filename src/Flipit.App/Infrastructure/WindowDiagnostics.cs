using System.Text;

namespace Flipit.Infrastructure;

/// <summary>
/// Captures the focused child control's class for the foreground window.
///
/// Used by <see cref="Flipit.Core.FlipOrchestrator"/> to detect Scintilla-based
/// editors (Notepad++, SciTE, Geany, …) that require special selection handling:
/// Scintilla copies the current line on Ctrl+C even when nothing is selected,
/// which would cause Phase 1 to falsely detect a selection.
///
/// GetGUIThreadInfo is used instead of GetClassName on the top-level HWND because
/// the Scintilla editor is a child control — the top-level window class is
/// "Notepad++" while the focused child's class is "Scintilla".
/// </summary>
public sealed record WindowInfo(IntPtr FocusedHandle, bool IsScintilla)
{
    public static readonly WindowInfo Unknown = new(IntPtr.Zero, false);
}

public static class WindowDiagnostics
{
    /// <summary>
    /// Captures the focused child control's handle and class for the given
    /// top-level window.  Returns <see cref="WindowInfo.Unknown"/> on any failure.
    /// </summary>
    public static WindowInfo Capture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return WindowInfo.Unknown;

        try
        {
            var threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);

            var gui = new NativeMethods.GUITHREADINFO
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GUITHREADINFO>()
            };

            var focusedHwnd = NativeMethods.GetGUIThreadInfo(threadId, ref gui) && gui.hwndFocus != IntPtr.Zero
                ? gui.hwndFocus
                : hwnd;

            var sb = new StringBuilder(256);
            NativeMethods.GetClassName(focusedHwnd, sb, sb.Capacity);
            var isScintilla = string.Equals(sb.ToString(), "Scintilla", StringComparison.OrdinalIgnoreCase);

            return new WindowInfo(focusedHwnd, isScintilla);
        }
        catch
        {
            return WindowInfo.Unknown;
        }
    }
}
