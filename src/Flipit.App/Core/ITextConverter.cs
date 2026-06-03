namespace Flipit.Core;

/// <summary>
/// Converts text between Hebrew and English keyboard layouts.
/// Stateless - safe to use as singleton.
/// </summary>
public interface ITextConverter
{
    /// <summary>
    /// Converts the given text by remapping physical keyboard keys
    /// between Hebrew and English layouts.
    /// </summary>
    string Convert(string text);
}

