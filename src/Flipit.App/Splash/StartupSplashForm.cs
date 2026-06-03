using Flipit.Infrastructure;

namespace Flipit.Splash;

/// <summary>
/// Lightweight startup splash shown once on launch.
///
/// Behaviour:
///   • Fades in over <see cref="FadeMs"/> ms
///   • Stays fully visible for <see cref="HoldMs"/> ms
///   • Fades out over <see cref="FadeMs"/> ms, then closes itself
///
/// Focus policy:
///   • <see cref="ShowWithoutActivation"/> returns true → window never steals focus
///   • <see cref="TopMost"/> = true for the duration so it stays visible
///
/// Lifetime:
///   • Call <see cref="ShowSplash"/> from the UI thread immediately after
///     <see cref="Application.Run(ApplicationContext)"/> starts the message loop
///     (or just before it, since the message loop begins processing on the
///     first <see cref="Application.DoEvents"/> / WM pump tick).
///   • The form disposes itself via a <see cref="Timer"/>; the caller does NOT
///     need to keep a reference after calling Show().
/// </summary>
internal sealed class StartupSplashForm : Form
{
    // ── Timing ────────────────────────────────────────────────────────────────
    private const int FadeMs    = 120;   // fade-in / fade-out duration
    private const int HoldMs    = 1200;  // fully-visible duration
    private const int TickMs    = 16;    // ~60 fps timer tick

    private readonly double _fadeStep = (double)TickMs / FadeMs;

    // ── State machine ─────────────────────────────────────────────────────────
    private enum Phase { FadingIn, Holding, FadingOut }

    private Phase   _phase      = Phase.FadingIn;
    private int     _holdTicks  = 0;
    private readonly int _maxHoldTicks = (int)Math.Ceiling((double)HoldMs / TickMs);

    // ── Resources ─────────────────────────────────────────────────────────────
    private Image?  _logo;
    private readonly System.Windows.Forms.Timer _timer;

    // ── Constructor ───────────────────────────────────────────────────────────

    public StartupSplashForm()
    {
        // ── Form chrome ───────────────────────────────────────────────────────
        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.CenterScreen;
        ShowInTaskbar   = false;
        TopMost         = true;
        Opacity         = 0;            // start invisible; timer fades us in
        BackColor       = Color.White;
        ClientSize      = new Size(320, 200);

        // ── Window icon (title-bar / Alt+Tab) ─────────────────────────────────
        using var appIcon = AppIcons.LoadAppIcon();
        if (!ReferenceEquals(appIcon, SystemIcons.Application))
            Icon = (Icon)appIcon.Clone();

        // ── Rounded-corner appearance (Windows 11) ────────────────────────────
        // DWM_WINDOW_CORNER_PREFERENCE = 2 (DWMWCP_ROUND) — silently ignored on Win10
        try
        {
            const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
            int round = 2;
            NativeSplashMethods.DwmSetWindowAttribute(
                Handle,
                DWMWA_WINDOW_CORNER_PREFERENCE,
                ref round,
                sizeof(int));
        }
        catch { /* non-fatal; just skip on OS that doesn't support it */ }

        BuildContent();

        // ── Animation timer ───────────────────────────────────────────────────
        _timer = new System.Windows.Forms.Timer { Interval = TickMs };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    // ── Focus policy — never steal keyboard focus ─────────────────────────────

    protected override bool ShowWithoutActivation => true;

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildContent()
    {
        // Outer panel — adds a subtle light-gray border feel via BackColor contrast
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.White,
            Padding   = new Padding(0),
        };

        // ── Logo ──────────────────────────────────────────────────────────────
        _logo = AppIcons.LoadBrandingImage();
        var logoBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size     = new Size(80, 80),
            Location = new Point((ClientSize.Width - 80) / 2, 26),
            BackColor= Color.White,
        };
        if (_logo is not null)
            logoBox.Image = _logo;

        // ── App name ──────────────────────────────────────────────────────────
        var lblName = new Label
        {
            Text      = "Flipit",
            Font      = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.White,
            AutoSize  = true,
        };
        lblName.Location = new Point(
            (ClientSize.Width - lblName.PreferredWidth) / 2,
            logoBox.Bottom + 10);

        // ── Subtitle ──────────────────────────────────────────────────────────
        var lblSub = new Label
        {
            Text      = "Keyboard layout fixer is running",
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(110, 110, 110),
            BackColor = Color.White,
            AutoSize  = true,
        };
        lblSub.Location = new Point(
            (ClientSize.Width - lblSub.PreferredWidth) / 2,
            lblName.Location.Y + lblName.PreferredHeight + 4);

        // ── Thin accent bar at the bottom ─────────────────────────────────────
        var accentBar = new Panel
        {
            BackColor = Color.FromArgb(0, 120, 212),   // Windows blue
            Location  = new Point(0, ClientSize.Height - 3),
            Size      = new Size(ClientSize.Width, 3),
        };

        panel.Controls.Add(logoBox);
        panel.Controls.Add(lblName);
        panel.Controls.Add(lblSub);
        panel.Controls.Add(accentBar);
        Controls.Add(panel);

        // ── Drop-shadow border (1 px dark outline) ────────────────────────────
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(200, 200, 200), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
    }

    // ── Animation tick ────────────────────────────────────────────────────────

    private void OnTick(object? sender, EventArgs e)
    {
        switch (_phase)
        {
            case Phase.FadingIn:
                Opacity = Math.Min(1.0, Opacity + _fadeStep);
                if (Opacity >= 1.0)
                    _phase = Phase.Holding;
                break;

            case Phase.Holding:
                _holdTicks++;
                if (_holdTicks >= _maxHoldTicks)
                    _phase = Phase.FadingOut;
                break;

            case Phase.FadingOut:
                Opacity = Math.Max(0.0, Opacity - _fadeStep);
                if (Opacity <= 0.0)
                    CloseSplash();
                break;
        }
    }

    private void CloseSplash()
    {
        _timer.Stop();
        Close();
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _logo?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>Minimal DWM P/Invoke for splash window rounding (Win 11).</summary>
internal static class NativeSplashMethods
{
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}


