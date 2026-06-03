namespace Flipit.Core;

/// <summary>
/// Immutable value type describing a global hotkey: a virtual key plus optional modifiers.
/// </summary>
/// <param name="Key">The main virtual key (e.g. <see cref="Keys.F1"/>).</param>
/// <param name="Modifiers">Bitwise combination of modifier keys (Ctrl, Alt, Shift). Use <see cref="Keys.None"/> for no modifiers.</param>
public record HotkeyDefinition(Keys Key, Keys Modifiers = Keys.None)
{
    /// <summary>Default hotkey: F1 (no modifiers).</summary>
    public static readonly HotkeyDefinition Default = new(Keys.F1);

    /// <summary>
    /// Returns a human-readable string, e.g. "Ctrl+F6" or "F1".
    /// </summary>
    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(Keys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(Keys.Alt))     parts.Add("Alt");
            if (Modifiers.HasFlag(Keys.Shift))   parts.Add("Shift");
            parts.Add(KeyDisplayName(Key));
            return string.Join("+", parts);
        }
    }

    /// <summary>
    /// Converts <see cref="Modifiers"/> to the bitmask expected by Win32 <c>RegisterHotKey</c>.
    /// </summary>
    public uint Win32Modifiers
    {
        get
        {
            uint m = 0;
            if (Modifiers.HasFlag(Keys.Alt))     m |= 0x0001; // MOD_ALT
            if (Modifiers.HasFlag(Keys.Control)) m |= 0x0002; // MOD_CONTROL
            if (Modifiers.HasFlag(Keys.Shift))   m |= 0x0004; // MOD_SHIFT
            return m;
        }
    }

    /// <summary>Returns the virtual key code expected by Win32.</summary>
    public uint Win32VirtualKey => (uint)Key;

    // ── Persistence helpers ──────────────────────────────────────────────────

    /// <summary>Serialise to a compact string for storage, e.g. "0x70:0x0".</summary>
    public string Serialize() => $"{(int)Key}:{(int)Modifiers}";

    /// <summary>
    /// Parse a string previously produced by <see cref="Serialize"/>.
    /// Returns <see cref="Default"/> on failure or when the parsed key is not
    /// in the allowed set (guards against tampered registry values).
    /// </summary>
    public static HotkeyDefinition Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Default;
        var parts = value.Split(':');
        if (parts.Length == 2
            && int.TryParse(parts[0], out int vk)
            && int.TryParse(parts[1], out int mod))
        {
            var key = (Keys)vk;
            // Validate key is in the allowed set — prevents a tampered HKCU value
            // from registering an arbitrary VK (e.g. VK_SPACE = 0x20).
            if (!IsAllowedKey(key)) return Default;
            var modifiers = (Keys)mod & (Keys.Control | Keys.Alt | Keys.Shift);
            return new HotkeyDefinition(key, modifiers);
        }
        return Default;
    }

    /// <summary>
    /// Returns true when <paramref name="key"/> is an acceptable hotkey main key.
    /// Mirrors <c>HotkeyValidator.AllowedKeys</c> — kept here to avoid a
    /// circular dependency between Core and Settings.
    /// </summary>
    private static bool IsAllowedKey(Keys key) =>
        key is Keys.F1  or Keys.F2  or Keys.F3  or Keys.F4
            or Keys.F5  or Keys.F6  or Keys.F7  or Keys.F8
            or Keys.F9  or Keys.F10 or Keys.F11 or Keys.F12
            or Keys.Insert or Keys.Pause or Keys.Scroll
            or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown;

    // ── Private helpers ──────────────────────────────────────────────────────

    private static string KeyDisplayName(Keys key) => key switch
    {
        Keys.F1         => "F1",
        Keys.F2         => "F2",
        Keys.F3         => "F3",
        Keys.F4         => "F4",
        Keys.F5         => "F5",
        Keys.F6         => "F6",
        Keys.F7         => "F7",
        Keys.F8         => "F8",
        Keys.F9         => "F9",
        Keys.F10        => "F10",
        Keys.F11        => "F11",
        Keys.F12        => "F12",
        Keys.Insert     => "Insert",
        Keys.Pause      => "Pause",
        Keys.Scroll     => "Scroll Lock",
        Keys.Home       => "Home",
        Keys.End        => "End",
        Keys.PageUp     => "Page Up",
        Keys.PageDown   => "Page Down",
        _               => key.ToString()
    };
}

