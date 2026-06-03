using Flipit.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Flipit.Settings;

/// <summary>
/// Persists app settings to the Windows registry under HKCU.
/// </summary>
public sealed class AppSettings : IAppSettings
{
    private const string RegistryKey   = @"Software\Flipit";
    private const string StartupKey    = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValue  = "Flipit";
    private const string EnabledValue  = "Enabled";
    private const string HotkeyValue   = "Hotkey";

    private readonly ILogger<AppSettings> _logger;

    public bool IsEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public HotkeyDefinition Hotkey { get; set; } = HotkeyDefinition.Default;

    public AppSettings(ILogger<AppSettings> logger)
    {
        _logger = logger;
    }

    public void Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key is null) return;

            IsEnabled        = (int)(key.GetValue(EnabledValue, 1)!) == 1;
            Hotkey           = HotkeyDefinition.Deserialize(key.GetValue(HotkeyValue) as string);

            // Check startup
            using var startupKey = Registry.CurrentUser.OpenSubKey(StartupKey);
            StartWithWindows = startupKey?.GetValue(StartupValue) is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
        }
    }

    public void Save()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(EnabledValue, IsEnabled ? 1 : 0,  RegistryValueKind.DWord);
            key.SetValue(HotkeyValue,  Hotkey.Serialize(), RegistryValueKind.String);

            // Startup
            using var startupKey = Registry.CurrentUser.OpenSubKey(StartupKey, writable: true);
            if (startupKey is null) return;

            if (StartWithWindows)
            {
                var exePath = System.Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                startupKey.SetValue(StartupValue, $"\"{exePath}\"");
            }
            else
            {
                startupKey.DeleteValue(StartupValue, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings");
        }
    }
}
