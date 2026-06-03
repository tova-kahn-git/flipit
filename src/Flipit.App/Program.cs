using Flipit.Infrastructure;
using Flipit.Splash;
using Flipit.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flipit;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Prevent multiple instances
        using var mutex = new System.Threading.Mutex(true, "FlipitSingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Flipit is already running.", "Flipit",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // PerMonitorV2 gives per-monitor crisp rendering on mixed-DPI setups (Win10 1703+)
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        // Must be called BEFORE any control is created on this thread.
        // Creating the splash form below would otherwise make this call throw.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // ── Show splash immediately ───────────────────────────────────────────
        // Shown BEFORE BuildServiceProvider() so it is visible during the heavy
        // DI / JIT startup work (especially slow on first run in single-file mode).
        // Application.DoEvents() flushes WM_PAINT so the OS renders the window.
        using var splash = new StartupSplashForm();
        splash.Show();
        Application.DoEvents();

        var services = ServiceConfiguration.BuildServiceProvider();
        var logger   = services.GetRequiredService<ILogger<object>>();

        // ── Global exception handlers ─────────────────────────────────────────
        Application.ThreadException += (_, e) =>
        {
            logger.LogError(e.Exception, "Unhandled UI thread exception");
            ShowFatalError(e.Exception.Message);
            Application.Exit();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            logger.LogError(ex, "Unhandled background thread exception: {Message}",
                ex?.Message ?? e.ExceptionObject?.ToString());
            // Cannot reliably show UI here — the runtime may be terminating.
        };

        // Load settings on startup
        var settings = services.GetRequiredService<Flipit.Core.IAppSettings>();
        settings.Load();

        var context = services.GetRequiredService<TrayApplicationContext>();
        Application.Run(context);
    }

    private static void ShowFatalError(string message)
    {
        try
        {
            MessageBox.Show(
                $"Flipit encountered an unexpected error and must close.\n\n{message}",
                "Flipit — Unexpected Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { /* cannot show UI — ignore */ }
    }
}