namespace Flipit.Core;

/// <summary>
/// Immutable snapshot of the clipboard state captured at a point in time.
/// Used to verify whether a Ctrl+C actually wrote new content — without ever
/// mutating the clipboard with sentinel or marker values.
///
/// TWO verification modes:
///
/// IsGenuineSelectionCopy  — for Phase 1 (did the user have text selected?)
///   Requires BOTH seq advanced AND text differs.
///   Rationale: Win11 Notepad and Chromium re-write the existing clipboard
///   value on every Ctrl+C (even with no selection), incrementing the sequence
///   number but leaving the text unchanged.  Checking text equality filters
///   out that false-positive.
///
/// WasWrittenAfter  — for Phase 2 (did our line-selection Ctrl+C copy anything?)
///   Requires only seq advanced (plus non-empty text).
///   Rationale: in Phase 2 we have explicitly performed a selection gesture
///   before the Ctrl+C.  The only way the seq advances without producing
///   useful text is if the line was empty (caught by the non-empty check).
///   Using seq-only here correctly handles the edge case where the line
///   content equals the previous clipboard text — text-diff would wrongly
///   reject a genuine copy in that case.
/// </summary>
public sealed record ClipboardSnapshot(uint SequenceNumber, string? Text)
{
    // ── Phase 1: selection detection ─────────────────────────────────────────

    /// <summary>
    /// Returns true when the clipboard contains a genuinely NEW selection:
    ///   • seq advanced  (something was written)
    ///   • text is non-empty
    ///   • text differs from this snapshot  (not a Win11/Chromium re-write)
    /// </summary>
    public bool IsGenuineSelectionCopy(uint currentSeq, string? currentText)
    {
        if (currentSeq == SequenceNumber)      return false; // nothing written
        if (string.IsNullOrEmpty(currentText)) return false; // wrote empty
        // Win11 Notepad / Chromium re-write: seq advanced but same text
        if (string.Equals(currentText, Text, StringComparison.Ordinal)) return false;
        return true;
    }

    // ── Phase 2: line-copy verification ──────────────────────────────────────

    /// <summary>
    /// Returns true when the clipboard was written since this snapshot was
    /// taken AND the new text is non-empty.
    /// Seq-only check — see class doc for rationale.
    /// </summary>
    public bool WasWrittenAfter(uint currentSeq, string? currentText)
    {
        if (currentSeq == SequenceNumber)      return false; // nothing written
        if (string.IsNullOrEmpty(currentText)) return false; // empty line
        return true;
    }
}
