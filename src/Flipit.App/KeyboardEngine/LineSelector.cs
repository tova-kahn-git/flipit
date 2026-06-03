using Flipit.Core;
using Flipit.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Flipit.KeyboardEngine;

/// <summary>
/// Three-attempt line selection strategy verified with clipboard sequence-number
/// snapshots.  Never writes sentinel or marker values to the clipboard.
///
/// VERIFICATION (WasWrittenAfter — seq-only):
///   A snapshot is taken immediately before each Ctrl+C.  After Ctrl+C we check
///   whether the OS sequence number advanced AND the clipboard is non-empty.
///   Seq-only (no text comparison) is intentional: the line being selected may
///   contain the same text that was already in the clipboard.  Text-diff would
///   reject a genuine copy in that case.
///
/// ATTEMPTS:
///   1. LTR: Home → ShiftDown · End · ShiftUp
///   2. RTL: End  → ShiftDown · Home · ShiftUp  (Hebrew RTL paragraphs)
///   3. Bundled: Home + Shift+End in one atomic SendInput call
///
/// WHY SEPARATE SendInput CALLS PER MODIFIER:
///   Bundling [ShiftDown + End + ShiftUp] in one SendInput call is unreliable in
///   some apps (Chrome, IntelliJ).  Separate calls with delays let the OS update
///   the modifier key state before the navigation key arrives.
/// </summary>
public sealed class LineSelector : ILineSelector
{
    private readonly IKeyboardSimulator    _keyboard;
    private readonly IClipboardService     _clipboard;
    private readonly ILogger<LineSelector> _logger;

    private const int KeyDelayMs    = 40;
    private const int AfterSelectMs = 80;
    private const int AfterCopyMs   = 150;

    public LineSelector(
        IKeyboardSimulator    keyboard,
        IClipboardService     clipboard,
        ILogger<LineSelector> logger)
    {
        _keyboard  = keyboard;
        _clipboard = clipboard;
        _logger    = logger;
    }

    // ── ILineSelector ─────────────────────────────────────────────────────────

    public string? SelectAndCopy()
    {
        var r1 = TryWithSeparateKeys(goToStart: true);
        if (r1 is not null) return r1;

        var r2 = TryWithSeparateKeys(goToStart: false);
        if (r2 is not null) return r2;

        var r3 = TryBundled();
        if (r3 is not null) return r3;

        _logger.LogWarning("All 3 line-selection attempts failed");
        return null;
    }

    // ── Strategy A: separate SendInput per key with delays ────────────────────

    private string? TryWithSeparateKeys(bool goToStart)
    {
        if (goToStart) _keyboard.SendHome();
        else           _keyboard.SendEnd();
        Thread.Sleep(KeyDelayMs);

        _keyboard.SendShiftDown();
        Thread.Sleep(KeyDelayMs);

        if (goToStart) _keyboard.SendEnd();
        else           _keyboard.SendHome();
        Thread.Sleep(KeyDelayMs);

        _keyboard.SendShiftUp();
        Thread.Sleep(AfterSelectMs);

        return CopyAndVerify();
    }

    // ── Strategy B: everything in one SendInput call ──────────────────────────

    private string? TryBundled()
    {
        var inputs = new NativeMethods.INPUT[]
        {
            ExtKeyDown(NativeMethods.VK_HOME),
            ExtKeyUp(NativeMethods.VK_HOME),
            ModKeyDown(NativeMethods.VK_SHIFT),
            ExtKeyDown(NativeMethods.VK_END),
            ExtKeyUp(NativeMethods.VK_END),
            ModKeyUp(NativeMethods.VK_SHIFT),
        };

        uint sent = NativeMethods.SendInput(
            (uint)inputs.Length, inputs,
            System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());

        if (sent != (uint)inputs.Length)
            _logger.LogWarning("Bundled SendInput: requested {Total}, accepted {Sent}", inputs.Length, sent);

        Thread.Sleep(AfterSelectMs);

        return CopyAndVerify();
    }

    // ── Shared copy + verify ──────────────────────────────────────────────────

    /// <summary>
    /// Takes a snapshot immediately before Ctrl+C, then verifies the clipboard
    /// was updated with non-empty content.  Returns null if the clipboard did
    /// not change (selection gesture had no effect).
    /// </summary>
    private string? CopyAndVerify()
    {
        var before = new ClipboardSnapshot(_clipboard.GetSequenceNumber(), _clipboard.GetText());

        _keyboard.SendCopy();
        Thread.Sleep(AfterCopyMs);

        var seqNow  = _clipboard.GetSequenceNumber();
        var textNow = _clipboard.GetText();

        return before.WasWrittenAfter(seqNow, textNow) ? textNow : null;
    }

    // ── INPUT helpers ─────────────────────────────────────────────────────────

    private static NativeMethods.INPUT ExtKeyDown(ushort vk) =>
        MakeKey(vk, NativeMethods.KEYEVENTF_EXTENDEDKEY);

    private static NativeMethods.INPUT ExtKeyUp(ushort vk) =>
        MakeKey(vk, NativeMethods.KEYEVENTF_KEYUP | NativeMethods.KEYEVENTF_EXTENDEDKEY);

    private static NativeMethods.INPUT ModKeyDown(ushort vk) => MakeKey(vk, 0);
    private static NativeMethods.INPUT ModKeyUp(ushort vk)   => MakeKey(vk, NativeMethods.KEYEVENTF_KEYUP);

    private static NativeMethods.INPUT MakeKey(ushort vk, uint flags) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT { wVk = vk, dwFlags = flags }
        }
    };
}
