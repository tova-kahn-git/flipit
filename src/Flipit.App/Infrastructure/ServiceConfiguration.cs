using Flipit.Clipboard;
using Flipit.Core;
using Flipit.Hooks;
using Flipit.KeyboardEngine;
using Flipit.Settings;
using Flipit.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flipit.Infrastructure;

/// <summary>
/// Composition root — wires up all services.
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Logging — always write to file so failures are visible without a debugger
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
#if DEBUG
            builder.AddDebug();
#endif
        });

        // Core services
        services.AddSingleton<IAppSettings, AppSettings>();
        services.AddSingleton<ITextConverter, TextConverter>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IKeyboardSimulator, KeyboardSimulator>();
        services.AddSingleton<ILineSelector, LineSelector>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IFlipOrchestrator, FlipOrchestrator>();

        // UI
        services.AddTransient<SettingsForm>();
        services.AddSingleton<Func<SettingsForm>>(sp => () => sp.GetRequiredService<SettingsForm>());
        services.AddSingleton<TrayApplicationContext>();

        return services.BuildServiceProvider();
    }
}



