namespace Flipit.Core;

/// <summary>
/// Selects the current line in the focused application, copies it, and
/// verifies the copy succeeded using clipboard sequence-number snapshots.
/// Never writes sentinel or marker values to the clipboard.
/// </summary>
public interface ILineSelector
{
    /// <summary>
    /// Selects the current line, copies it, and returns the copied text.
    /// Returns null if the operation could not be verified (selection failed
    /// or clipboard sequence number did not advance after Ctrl+C).
    /// Callers MUST treat null as a hard abort — never fall back to any
    /// previous clipboard content.
    /// </summary>
    string? SelectAndCopy();
}
