using Flipit.Core;

namespace Flipit.Settings;

/// <summary>
/// Validates whether a <see cref="Keys"/> value is an acceptable global hotkey.
/// Disallows letters, digits, and common typing keys to avoid interfering with
/// normal text input.
/// </summary>
public static class HotkeyValidator
{
    /// <summary>Keys explicitly permitted as the main key in a hotkey.</summary>
    public static readonly IReadOnlySet<Keys> AllowedKeys = new HashSet<Keys>
    {
        Keys.F1,  Keys.F2,  Keys.F3,  Keys.F4,
        Keys.F5,  Keys.F6,  Keys.F7,  Keys.F8,
        Keys.F9,  Keys.F10, Keys.F11, Keys.F12,
        Keys.Insert, Keys.Pause, Keys.Scroll,
        Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
    };

    /// <summary>Keys that act as modifiers (Ctrl, Alt, Shift).</summary>
    public static readonly IReadOnlySet<Keys> ModifierKeys = new HashSet<Keys>
    {
        Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
        Keys.Menu,       Keys.LMenu,       Keys.RMenu,
        Keys.ShiftKey,   Keys.LShiftKey,   Keys.RShiftKey,
        Keys.LWin,       Keys.RWin,
    };

    /// <summary>
    /// Returns <c>true</c> and sets <paramref name="definition"/> when
    /// <paramref name="key"/> is a valid hotkey main key.
    /// </summary>
    public static bool TryBuild(Keys key, Keys modifiers,
        out HotkeyDefinition definition, out string errorMessage)
    {
        // Strip modifier bits from the key value (e.g. Keys.Control | Keys.F6 → Keys.F6)
        var baseKey = key & Keys.KeyCode;

        if (!AllowedKeys.Contains(baseKey))
        {
            definition  = HotkeyDefinition.Default;
            errorMessage = $"'{baseKey}' is not a supported hotkey. "
                         + "Please use a function key (F1–F12) or a navigation key "
                         + "(Insert, Pause, Scroll Lock, Home, End, Page Up, Page Down).";
            return false;
        }

        // Strip modifier bits that are already captured in the modifiers parameter
        var cleanModifiers = modifiers & (Keys.Control | Keys.Alt | Keys.Shift);

        definition   = new HotkeyDefinition(baseKey, cleanModifiers);
        errorMessage = string.Empty;
        return true;
    }

    /// <summary>Returns <c>true</c> when the key is purely a modifier keystroke.</summary>
    public static bool IsModifierOnly(Keys key) => ModifierKeys.Contains(key & Keys.KeyCode);
}

