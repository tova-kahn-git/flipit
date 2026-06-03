using Flipit.About;
using Flipit.Core;
using Flipit.Infrastructure;
using Flipit.Settings;
using Microsoft.Extensions.Logging;

namespace Flipit.Tray;

/// <summary>
/// Invisible background window.  Hosts:
///   • System tray icon with a polished ContextMenuStrip
///   • Global hotkey message pump
///
/// Menu appearance is driven by <see cref="TrayMenuRenderer"/> (custom
/// ProfessionalColorTable + targeted overrides) and <see cref="TrayMenuIcons"/>
/// (glyph-based icon bitmaps).  No third-party libraries or heavy owner-draw.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IHotkeyService                  _hotkey;
    private readonly IFlipOrchestrator               _orchestrator;
    private readonly IAppSettings                    _settings;
    private readonly ILogger<TrayApplicationContext> _logger;
    private readonly Func<SettingsForm>              _settingsFormFactory;

    // ── Tray infrastructure ───────────────────────────────────────────────────
    private NotifyIcon          _trayIcon      = null!;
    private HiddenMessageWindow _messageWindow = null!;
    private Icon?               _appIcon;

    // ── Menu GDI resources ────────────────────────────────────────────────────
    private TrayMenuRenderer?  _renderer;
    private Bitmap?            _iconEnabledOn;   // checkmark — shown only when enabled
    private Bitmap?            _iconSettings;
    private Bitmap?            _iconAbout;
    private Bitmap?            _iconExit;

    // ── Menu item ref ─────────────────────────────────────────────────────────
    private ToolStripMenuItem? _enableItem;

    // ── Single-instance About window ─────────────────────────────────────────
    private AboutForm? _aboutForm;

    // ── Constructor ───────────────────────────────────────────────────────────

    public TrayApplicationContext(
        IHotkeyService                  hotkey,
        IFlipOrchestrator               orchestrator,
        IAppSettings                    settings,
        ILogger<TrayApplicationContext> logger,
        Func<SettingsForm>              settingsFormFactory)
    {
        _hotkey              = hotkey;
        _orchestrator        = orchestrator;
        _settings            = settings;
        _logger              = logger;
        _settingsFormFactory = settingsFormFactory;

        Initialize();
    }

    // ── Initialization ────────────────────────────────────────────────────────

    private void Initialize()
    {
        _messageWindow = new HiddenMessageWindow(_hotkey);
        _appIcon       = AppIcons.LoadAppIcon();

        _trayIcon = new NotifyIcon
        {
            Text             = TrayTooltip(),
            Icon             = _appIcon,
            ContextMenuStrip = BuildMenu(),
            Visible          = true,
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        bool registered = _hotkey.Register(_messageWindow.Handle);
        _hotkey.HotkeyPressed += OnHotkeyFired;

        if (!registered)
        {
            _trayIcon.ShowBalloonTip(
                5000,
                "Flipit \u2014 Hotkey Not Registered",
                $"Could not register {_settings.Hotkey.DisplayText}. " +
                "Another application may be using it. " +
                "Open Settings to choose a different hotkey.",
                ToolTipIcon.Warning);
        }

        _logger.LogInformation("Flipit started");
    }

    // ── Menu construction ─────────────────────────────────────────────────────

    private ContextMenuStrip BuildMenu()
    {
        int iconSz      = SystemInformation.SmallIconSize.Width;
        _iconEnabledOn  = TrayMenuIcons.EnabledOn(iconSz);   // checkmark bitmap
        _iconSettings   = TrayMenuIcons.Settings(iconSz);
        _iconAbout      = TrayMenuIcons.About(iconSz);
        _iconExit       = TrayMenuIcons.Exit(iconSz);

        _renderer = new TrayMenuRenderer();

        var menu = new ContextMenuStrip
        {
            Renderer         = _renderer,
            Font             = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
            ImageScalingSize = new Size(iconSz, iconSz),
            Padding          = new Padding(0, 3, 0, 3),
            ShowImageMargin  = true,
            ShowCheckMargin  = false,
        };

        // ── Enabled toggle ────────────────────────────────────────────────────
        // Checkmark icon appears only when enabled; null = no image when disabled.
        _enableItem = new ToolStripMenuItem
        {
            Name         = "enableItem",
            Text         = "Enabled",
            Image        = _settings.IsEnabled ? _iconEnabledOn : null,
            Checked      = _settings.IsEnabled,
            CheckOnClick = true,
        };
        _enableItem.CheckedChanged += OnEnabledChanged;
        menu.Items.Add(_enableItem);

        // ── Settings ──────────────────────────────────────────────────────────
        menu.Items.Add(new ToolStripMenuItem("Settings", _iconSettings,
            (_, _) => ShowSettings()));

        // ── About ─────────────────────────────────────────────────────────────
        menu.Items.Add(new ToolStripMenuItem("About Flipit", _iconAbout,
            (_, _) => ShowAbout()));

        // ── Single separator before Exit ──────────────────────────────────────
        menu.Items.Add(new ToolStripSeparator());

        // ── Exit ──────────────────────────────────────────────────────────────
        menu.Items.Add(new ToolStripMenuItem("Exit", _iconExit,
            (_, _) => ExitApp()));

        return menu;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnHotkeyFired(object? sender, EventArgs e)
    {
        _ = _orchestrator.FlipAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Flip task faulted");
        }, TaskScheduler.Default);
    }

    private void OnEnabledChanged(object? sender, EventArgs e)
    {
        if (_enableItem is null) return;

        _settings.IsEnabled = _enableItem.Checked;
        _settings.Save();

        // Show checkmark only when enabled; no image when disabled
        _enableItem.Image  = _settings.IsEnabled ? _iconEnabledOn : null;
        _trayIcon.Text     = TrayTooltip();
    }

    // ── Settings / About windows ──────────────────────────────────────────────

    private void ShowSettings()
    {
        using var form = _settingsFormFactory();
        form.FormClosed += (_, _) =>
        {
            _trayIcon.Text = TrayTooltip();
            SyncEnabledItem();
        };
        form.ShowDialog();
    }

    private void ShowAbout()
    {
        if (_aboutForm is not null)
        {
            if (_aboutForm.WindowState == FormWindowState.Minimized)
                _aboutForm.WindowState = FormWindowState.Normal;
            _aboutForm.Activate();
            return;
        }

        _aboutForm = new AboutForm();
        _aboutForm.FormClosed += (_, _) =>
        {
            _aboutForm?.Dispose();
            _aboutForm = null;
        };
        _aboutForm.Show();
    }

    // ── Shutdown ──────────────────────────────────────────────────────────────

    private void ExitApp()
    {
        _hotkey.Unregister(_messageWindow.Handle);
        _trayIcon.Visible = false;
        _messageWindow.DestroyHandle();
        _messageWindow.Dispose();
        _aboutForm?.Close();
        Application.Exit();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string TrayTooltip() =>
        _settings.IsEnabled ? "Flipit" : "Flipit (Disabled)";

    private void SyncEnabledItem()
    {
        if (_enableItem is null) return;
        _enableItem.Checked = _settings.IsEnabled;
        _enableItem.Image   = _settings.IsEnabled ? _iconEnabledOn : null;
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkey.Dispose();
            _trayIcon?.Dispose();
            _messageWindow?.Dispose();
            _aboutForm?.Dispose();
            _renderer?.Dispose();

            _iconEnabledOn?.Dispose();
            _iconSettings?.Dispose();
            _iconAbout?.Dispose();
            _iconExit?.Dispose();

            if (_appIcon is not null && !ReferenceEquals(_appIcon, SystemIcons.Application))
                _appIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}

