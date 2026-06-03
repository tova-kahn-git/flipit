namespace Flipit.Core;

public interface IAppSettings
{
    bool IsEnabled { get; set; }
    bool StartWithWindows { get; set; }

    /// <summary>The persisted hotkey definition. Defaults to <see cref="HotkeyDefinition.Default"/>.</summary>
    HotkeyDefinition Hotkey { get; set; }

    void Save();
    void Load();
}
