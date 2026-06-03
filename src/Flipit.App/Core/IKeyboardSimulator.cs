namespace Flipit.Core;

/// <summary>
/// Simulates keyboard input at the OS level.
/// Each method issues exactly ONE SendInput call so callers can place explicit
/// delays between individual events (modifier down → navigation key → modifier up).
/// Bundling modifier + key into a single call is unreliable in some apps.
/// </summary>
public interface IKeyboardSimulator
{
    // ── Clipboard ─────────────────────────────────────────────────────────────

    /// <summary>Sends Ctrl+C.</summary>
    void SendCopy();

    /// <summary>Sends Ctrl+V.</summary>
    void SendPaste();

    // ── Navigation (extended keys) ────────────────────────────────────────────

    /// <summary>Sends the Home key (moves caret to start of line).</summary>
    void SendHome();

    /// <summary>Sends the End key (moves caret to end of line).</summary>
    void SendEnd();

    // ── Modifier primitives ───────────────────────────────────────────────────
    // These are intentionally separate so callers can hold Shift across a
    // navigation key with explicit timing between each event.

    /// <summary>Presses Shift down. Must be paired with <see cref="SendShiftUp"/>.</summary>
    void SendShiftDown();

    /// <summary>Releases Shift. Pair with <see cref="SendShiftDown"/>.</summary>
    void SendShiftUp();
}
