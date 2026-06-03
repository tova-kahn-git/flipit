namespace Flipit.Core;

/// <summary>
/// Manages the clipboard for read/write operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Returns the current clipboard sequence number.
    /// This OS counter increments on every clipboard write by any process.
    /// Compare before and after a Ctrl+C to know with certainty whether
    /// new content was copied — regardless of what the text content is.
    /// </summary>
    uint GetSequenceNumber();

    /// <summary>
    /// Returns the current clipboard text, or null if unavailable.
    /// </summary>
    string? GetText();

    /// <summary>
    /// Sets the clipboard text. Returns false on failure.
    /// </summary>
    bool SetText(string text);

    /// <summary>
    /// Clears all clipboard content. Returns false on failure.
    /// </summary>
    bool Clear();
}
