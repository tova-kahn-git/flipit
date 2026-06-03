using Flipit.Core;
using Flipit.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Flipit.Core;

/// <summary>
/// Orchestrates the full flip workflow using clipboard snapshots for
/// copy verification.  Never writes sentinel or marker values to the clipboard.
///
/// DETECTION STRATEGY:
///   Phase 1 — selection check (IsGenuineSelectionCopy)
///   Scintilla fast-path (Notepad++, SciTE, …)
///   Phase 2 — line copy (WasWrittenAfter in LineSelector)
///
/// SAFETY:
///   - The original clipboard content (text) is restored after the paste so the
///     user's clipboard is not permanently overwritten.
///   - HWND validity is re-checked after the initial settle delay to guard against
///     the TOCTOU race where a window is destroyed and its handle recycled.
///
/// RE-ENTRANCY:
///   SemaphoreSlim(1,1) drops a second hotkey press while a flip is in progress.
/// </summary>
public sealed class FlipOrchestrator : IFlipOrchestrator
{
    private readonly IKeyboardSimulator        _keyboard;
    private readonly IClipboardService         _clipboard;
    private readonly ITextConverter            _converter;
    private readonly ILineSelector             _lineSelector;
    private readonly IAppSettings              _settings;
    private readonly ILogger<FlipOrchestrator> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);

    private const int InitialSettleMs  = 50;
    private const int AfterCopyMs      = 150;
    private const int BeforePasteMs    = 50;
    private const int AfterPasteMs     = 150;
    private const int ClipboardRestoreDelayMs = 80;

    public FlipOrchestrator(
        IKeyboardSimulator keyboard,
        IClipboardService  clipboard,
        ITextConverter     converter,
        ILineSelector      lineSelector,
        IAppSettings       settings,
        ILogger<FlipOrchestrator> logger)
    {
        _keyboard     = keyboard;
        _clipboard    = clipboard;
        _converter    = converter;
        _lineSelector = lineSelector;
        _settings     = settings;
        _logger       = logger;
    }

    public async Task FlipAsync(CancellationToken ct = default)
    {
        if (!_settings.IsEnabled) return;

        if (!_gate.Wait(0))
        {
            _logger.LogDebug("Flip already in progress — dropping re-entrant request");
            return;
        }

        try
        {
            // TaskCreationOptions.LongRunning allocates a dedicated OS thread.
            // The flip worker holds the thread for up to ~600 ms (multiple sleeps);
            // using a pool thread for that long would starve the thread pool.
            await Task.Factory.StartNew(
                () => ExecuteFlip(ct),
                ct,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Flip cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in flip operation");
        }
        finally
        {
            _gate.Release();
        }
    }

    // ── Workflow ──────────────────────────────────────────────────────────────

    private void ExecuteFlip(CancellationToken ct)
    {
        // Capture the foreground window and its process ID BEFORE the settle delay.
        var fgHwnd = NativeMethods.GetForegroundWindow();
        NativeMethods.GetWindowThreadProcessId(fgHwnd, out uint originalPid);

        Thread.Sleep(InitialSettleMs);
        ct.ThrowIfCancellationRequested();

        // ── HWND validity check ───────────────────────────────────────────────
        // After the settle delay, verify the foreground window is still the same
        // process. The handle could be recycled to a different window/process if
        // the original window was destroyed during the delay.
        var fgHwndNow = NativeMethods.GetForegroundWindow();
        NativeMethods.GetWindowThreadProcessId(fgHwndNow, out uint currentPid);

        if (fgHwndNow == IntPtr.Zero || currentPid != originalPid)
        {
            _logger.LogWarning("Foreground window changed during settle delay — aborting flip");
            return;
        }

        // Re-capture window info after validity check so Scintilla detection
        // uses a fresh snapshot with the confirmed HWND.
        var winInfo = WindowDiagnostics.Capture(fgHwndNow);

        // ── Scintilla fast-path ───────────────────────────────────────────────
        // Notepad++ (Scintilla) copies the whole current line on Ctrl+C even
        // with no selection, making Phase 1 see a false SELECTION_FOUND.
        bool goDirectToLineSelect =
            winInfo.IsScintilla
            && winInfo.FocusedHandle != IntPtr.Zero
            && !ScintillaHasSelection(winInfo.FocusedHandle);

        // Save the original clipboard text so we can restore it after the paste.
        // This prevents the converted text from permanently replacing the
        // user's clipboard content.
        string? originalClipboard = _clipboard.GetText();

        string? textToConvert;

        if (!goDirectToLineSelect)
        {
            // ── Phase 1: detect existing text selection ───────────────────────
            var snapshot = new ClipboardSnapshot(
                _clipboard.GetSequenceNumber(),
                _clipboard.GetText());

            _keyboard.SendCopy();
            Thread.Sleep(AfterCopyMs);
            ct.ThrowIfCancellationRequested();

            var seqAfter  = _clipboard.GetSequenceNumber();
            var textAfter = _clipboard.GetText();

            if (snapshot.IsGenuineSelectionCopy(seqAfter, textAfter))
            {
                textToConvert = textAfter;
            }
            else
            {
                textToConvert = RunLineSelect(ct);
                if (textToConvert is null) return;
            }
        }
        else
        {
            // ── Phase 2 direct: Scintilla with no selection ───────────────────
            textToConvert = RunLineSelect(ct);
            if (textToConvert is null) return;
        }

        // ── Phase 3: convert ──────────────────────────────────────────────────
        var converted = _converter.Convert(textToConvert!);
        if (string.Equals(converted, textToConvert, StringComparison.Ordinal))
            return; // already the correct layout — nothing to do

        // ── Phase 4: paste ────────────────────────────────────────────────────
        if (!_clipboard.SetText(converted))
        {
            _logger.LogError("Failed to write converted text to clipboard — aborting paste");
            return;
        }

        Thread.Sleep(BeforePasteMs);
        _keyboard.SendPaste();
        Thread.Sleep(AfterPasteMs);
        ct.ThrowIfCancellationRequested();

        // ── Phase 5: restore original clipboard ──────────────────────────────
        // Restore pre-flip clipboard text so the user's clipboard is not
        // permanently replaced with the converted text.
        Thread.Sleep(ClipboardRestoreDelayMs);
        if (originalClipboard is not null)
        {
            if (!_clipboard.SetText(originalClipboard))
                _logger.LogWarning("Could not restore original clipboard content after flip");
        }
        else
        {
            // Original clipboard was empty or non-text — clear the converted text
            _clipboard.Clear();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? RunLineSelect(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var text = _lineSelector.SelectAndCopy();

        if (text is null)
        {
            _logger.LogWarning("Line selection failed — aborting");
            return null;
        }

        if (string.IsNullOrEmpty(text))
            return null; // empty line — nothing to convert

        return text;
    }

    /// <summary>
    /// Returns true when the Scintilla control has a non-empty text selection.
    /// Queries SCI_GETSELECTIONSTART and SCI_GETSELECTIONEND via SendMessage.
    /// </summary>
    private static bool ScintillaHasSelection(IntPtr scintillaHwnd)
    {
        var start = NativeMethods.SendMessage(scintillaHwnd, NativeMethods.SCI_GETSELECTIONSTART, IntPtr.Zero, IntPtr.Zero).ToInt64();
        var end   = NativeMethods.SendMessage(scintillaHwnd, NativeMethods.SCI_GETSELECTIONEND,   IntPtr.Zero, IntPtr.Zero).ToInt64();
        return end > start;
    }
}
