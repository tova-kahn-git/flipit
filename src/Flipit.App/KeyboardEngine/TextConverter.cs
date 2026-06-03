using Flipit.Core;

namespace Flipit.KeyboardEngine;

/// <summary>
/// Converts text between Hebrew and English based on physical keyboard layout mapping.
/// Stateless and thread-safe.
/// </summary>
public sealed class TextConverter : ITextConverter
{

    // Physical key position -> Hebrew character (Standard Hebrew keyboard layout).
    // Lowercase-only keys: input is normalized via char.ToLower() before lookup
    // so there is no need to store both 'q' and 'Q'.
    private static readonly Dictionary<char, char> EnglishToHebrew = new()
    {
        // Row 2 - QWERTY
        { 'q', '/' },  { 'w', '\'' }, { 'e', 'ק' }, { 'r', 'ר' }, { 't', 'א' },
        { 'y', 'ט' },  { 'u', 'ו' },  { 'i', 'ן' }, { 'o', 'ם' }, { 'p', 'פ' },
        // Row 3 - ASDF
        { 'a', 'ש' },  { 's', 'ד' },  { 'd', 'ג' }, { 'f', 'כ' }, { 'g', 'ע' },
        { 'h', 'י' },  { 'j', 'ח' },  { 'k', 'ל' }, { 'l', 'ך' },
        // Row 4 - ZXCV
        { 'z', 'ז' },  { 'x', 'ס' },  { 'c', 'ב' }, { 'v', 'ה' }, { 'b', 'נ' },
        { 'n', 'מ' },  { 'm', 'צ' },
        // Punctuation
        { ',', 'ת' }, { '.', 'ץ' }, { ';', 'ף' },
    };

    // Build reverse map: Hebrew -> English
    private static readonly Dictionary<char, char> HebrewToEnglish;

    // Chars that are uppercase Latin (used to detect shift for Hebrew->EN conversion)
    static TextConverter()
    {
        HebrewToEnglish = new Dictionary<char, char>();
        foreach (var kv in EnglishToHebrew)
        {
            // Only add lowercase english->hebrew mappings to build reverse
            // Hebrew chars map back to lowercase English
            if (!HebrewToEnglish.ContainsKey(kv.Value))
            {
                // Map to lowercase always since Hebrew has no case
                HebrewToEnglish[kv.Value] = char.ToLower(kv.Key);
            }
        }
    }

    public TextConverter() { }

    public string Convert(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var direction = DetectDirection(text);

        return direction == ConversionDirection.HebrewToEnglish
            ? ConvertHebrewToEnglish(text)
            : ConvertEnglishToHebrew(text);
    }

    private static ConversionDirection DetectDirection(string text)
    {
        int hebrew = 0, english = 0;
        foreach (var c in text)
        {
            if (IsHebrew(c)) hebrew++;
            else if (char.IsLetter(c)) english++;
        }

        return hebrew >= english
            ? ConversionDirection.HebrewToEnglish
            : ConversionDirection.EnglishToHebrew;
    }

    private static string ConvertHebrewToEnglish(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (HebrewToEnglish.TryGetValue(c, out var mapped))
                sb.Append(mapped);
            else
                sb.Append(c); // preserve numbers, spaces, symbols
        }
        return sb.ToString();
    }

    private static string ConvertEnglishToHebrew(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            // Normalize to lowercase — the dictionary contains only lowercase keys.
            // Both 'a' and 'A' map to the same Hebrew letter (Hebrew has no case).
            if (EnglishToHebrew.TryGetValue(char.ToLowerInvariant(c), out var mapped))
                sb.Append(mapped);
            else
                sb.Append(c); // preserve numbers, spaces, symbols
        }
        return sb.ToString();
    }

    private static bool IsHebrew(char c) => c >= '\u05D0' && c <= '\u05EA';
}

internal enum ConversionDirection { HebrewToEnglish, EnglishToHebrew }

