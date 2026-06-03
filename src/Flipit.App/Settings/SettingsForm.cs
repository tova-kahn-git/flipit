using Flipit.Core;
using Flipit.Infrastructure;

namespace Flipit.Settings;

/// <summary>
/// Simple settings dialog.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly IAppSettings   _settings;
    private readonly IHotkeyService _hotkeyService;

    // General controls
    private CheckBox _chkEnabled = null!;
    private CheckBox _chkStartup = null!;

    // Hotkey controls
    private Label  _lblCurrentHotkey = null!;
    private Button _btnChangeHotkey  = null!;
    private Button _btnResetHotkey   = null!;
    private Label  _lblHotkeyStatus  = null!;

    // Action buttons
    private Button _btnSave   = null!;
    private Button _btnCancel = null!;

    // Branding
    private Image? _brandingImage;

    public SettingsForm(IAppSettings settings, IHotkeyService hotkeyService)
    {
        _settings      = settings;
        _hotkeyService = hotkeyService;
        BuildUi();
        LoadValues();
    }

    // â”€â”€ UI construction â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void BuildUi()
    {
        Text            = "Flipit Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;
        ShowInTaskbar   = false;

        // Window icon
        using var appIcon = AppIcons.LoadAppIcon();
        if (!ReferenceEquals(appIcon, SystemIcons.Application))
            Icon = (Icon)appIcon.Clone();

        // Branding header
        _brandingImage = AppIcons.LoadBrandingImage();
        int headerH = _brandingImage is not null ? 56 : 0;

        ClientSize = new Size(340, 300 + headerH);

        if (_brandingImage is not null)
        {
            const int imgSize = 40;
            var imgBox = new PictureBox
            {
                Image    = _brandingImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(12, 8),
                Size     = new Size(imgSize, imgSize),
            };
            var lblTitle = new Label
            {
                Text     = "Flipit",
                Font     = new Font("Segoe UI", 14f, FontStyle.Regular, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(imgSize + 20, 16),
            };
            var separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location    = new Point(0, headerH - 2),
                Size        = new Size(340, 2),
            };
            Controls.AddRange(new Control[] { imgBox, lblTitle, separator });
        }

        int y = headerH + 14;

        // â”€â”€ General section â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        AddSectionHeader("General", 12, y);
        y += 22;

        _chkEnabled = new CheckBox
        {
            Text     = "Enable Flipit",
            Location = new Point(20, y),
            AutoSize = true,
        };
        y += 26;

        _chkStartup = new CheckBox
        {
            Text     = "Start with Windows",
            Location = new Point(20, y),
            AutoSize = true,
        };
        y += 36;

        // â”€â”€ Hotkey section â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        AddSectionHeader("Global Hotkey", 12, y);
        y += 22;

        var lblHotkeyCaption = new Label
        {
            Text     = "Current hotkey:",
            Location = new Point(20, y + 4),
            AutoSize = true,
        };

        _lblCurrentHotkey = new Label
        {
            Text      = "â€”",
            Location  = new Point(130, y),
            Size      = new Size(100, 22),
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        y += 30;

        _btnChangeHotkey = new Button
        {
            Text     = "Change Hotkey",
            Location = new Point(20, y),
            Size     = new Size(130, 26),
        };
        _btnChangeHotkey.Click += OnChangeHotkeyClick;

        _btnResetHotkey = new Button
        {
            Text     = "Reset to Default",
            Location = new Point(158, y),
            Size     = new Size(120, 26),
        };
        _btnResetHotkey.Click += OnResetHotkeyClick;
        y += 30;

        _lblHotkeyStatus = new Label
        {
            Text      = string.Empty,
            Location  = new Point(20, y),
            Size      = new Size(300, 18),
            ForeColor = Color.Firebrick,
            Font      = new Font("Segoe UI", 8.5f),
        };
        y += 28;

        // Separator before buttons
        var btnSep = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location    = new Point(0, y),
            Size        = new Size(340, 2),
        };
        y += 10;

        // â”€â”€ Dialog buttons â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        _btnSave = new Button
        {
            Text         = "Save",
            DialogResult = DialogResult.OK,
            Location     = new Point(140, y),
            Size         = new Size(80, 28),
        };
        _btnSave.Click += (_, _) => SaveGeneral();

        _btnCancel = new Button
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(234, y),
            Size         = new Size(80, 28),
        };

        Controls.AddRange(new Control[]
        {
            _chkEnabled, _chkStartup,
            lblHotkeyCaption, _lblCurrentHotkey,
            _btnChangeHotkey, _btnResetHotkey, _lblHotkeyStatus,
            btnSep, _btnSave, _btnCancel,
        });

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    private void AddSectionHeader(string text, int x, int y)
    {
        var lbl = new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            AutoSize  = true,
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = SystemColors.GrayText,
        };
        Controls.Add(lbl);
    }

    // â”€â”€ Data binding â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void LoadValues()
    {
        _chkEnabled.Checked = _settings.IsEnabled;
        _chkStartup.Checked = _settings.StartWithWindows;
        RefreshHotkeyDisplay();
    }

    private void RefreshHotkeyDisplay()
    {
        _lblCurrentHotkey.Text = _hotkeyService.CurrentHotkey.DisplayText;
        _lblHotkeyStatus.Text  = string.Empty;
    }

    // â”€â”€ Hotkey change â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void OnChangeHotkeyClick(object? sender, EventArgs e)
    {
        _lblHotkeyStatus.Text = string.Empty;

        using var dlg = new HotkeyCaptureDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.CapturedHotkey is null)
            return;

        var newHotkey = dlg.CapturedHotkey;

        bool success = _hotkeyService.ChangeHotkey(
            GetMessageWindowHandle(), newHotkey);

        if (success)
        {
            _lblHotkeyStatus.ForeColor = Color.DarkGreen;
            _lblHotkeyStatus.Text      = $"Hotkey changed to {newHotkey.DisplayText}";
            RefreshHotkeyDisplay();
        }
        else
        {
            _lblHotkeyStatus.ForeColor = Color.Firebrick;
            _lblHotkeyStatus.Text      =
                $"'{newHotkey.DisplayText}' is already in use by another application. "
                + $"Previous hotkey ({_hotkeyService.CurrentHotkey.DisplayText}) restored.";
            RefreshHotkeyDisplay();
        }
    }

    private void OnResetHotkeyClick(object? sender, EventArgs e)
    {
        _lblHotkeyStatus.Text = string.Empty;

        if (_hotkeyService.CurrentHotkey == HotkeyDefinition.Default)
        {
            _lblHotkeyStatus.ForeColor = SystemColors.GrayText;
            _lblHotkeyStatus.Text      = "Hotkey is already set to the default (F1).";
            return;
        }

        bool success = _hotkeyService.ChangeHotkey(
            GetMessageWindowHandle(), HotkeyDefinition.Default);

        if (success)
        {
            _lblHotkeyStatus.ForeColor = Color.DarkGreen;
            _lblHotkeyStatus.Text      = "Hotkey reset to F1.";
            RefreshHotkeyDisplay();
        }
        else
        {
            _lblHotkeyStatus.ForeColor = Color.Firebrick;
            _lblHotkeyStatus.Text      =
                "Could not register F1 â€” it may be in use by another application.";
        }
    }

    private IntPtr GetMessageWindowHandle()
    {
        var h = _hotkeyService.RegisteredWindowHandle;
        return h != IntPtr.Zero ? h : Handle;
    }

    // â”€â”€ General save â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SaveGeneral()
    {
        _settings.IsEnabled          = _chkEnabled.Checked;
        _settings.StartWithWindows   = _chkStartup.Checked;
        _settings.Save();
    }

    // â”€â”€ Disposal â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _brandingImage?.Dispose();
        base.Dispose(disposing);
    }
}
