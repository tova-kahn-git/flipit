using Flipit.Infrastructure;

namespace Flipit.About;

/// <summary>
/// Lightweight standalone About window.
/// Opened from the system tray menu.
/// </summary>
public sealed class AboutForm : Form
{
    private Image? _logo;

    public AboutForm()
    {
        BuildUi();
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUi()
    {
        Text            = "About Flipit";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;
        ShowInTaskbar   = false;
        AutoScroll      = true;
        ClientSize      = new Size(400, 520);

        // Window icon
        using var appIcon = AppIcons.LoadAppIcon();
        if (!ReferenceEquals(appIcon, SystemIcons.Application))
            Icon = (Icon)appIcon.Clone();

        // ── Outer scroll panel ────────────────────────────────────────────────
        var scroll = new Panel
        {
            Dock       = DockStyle.Fill,
            AutoScroll = true,
        };

        // ── Flow panel ────────────────────────────────────────────────────────
        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            Dock          = DockStyle.Top,
            Padding       = new Padding(24, 20, 24, 20),
        };

        const int contentWidth = 352;

        // ── Logo ─────────────────────────────────────────────────────────────
        _logo = AppIcons.LoadBrandingImage();
        if (_logo is not null)
        {
            var logoBox = new PictureBox
            {
                Image    = _logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size     = new Size(80, 80),
            };
            flow.Controls.Add(Centered(logoBox, contentWidth, 90));
            AddGap(flow, 6);
        }

        // ── Title ─────────────────────────────────────────────────────────────
        var lblTitle = new Label
        {
            Text      = "About Flipit",
            Font      = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize  = true,
            ForeColor = SystemColors.ControlText,
        };
        flow.Controls.Add(Centered(lblTitle, contentWidth));
        AddGap(flow, 12);

        // ── Description ───────────────────────────────────────────────────────
        flow.Controls.Add(MakeWrappedLabel(
            "Flipit is a lightweight Windows utility that instantly fixes text typed " +
            "with the wrong keyboard layout.\r\n\r\n" +
            "Instead of deleting and retyping text, simply press your configured hotkey " +
            "and Flipit will automatically convert the text between Hebrew and English layouts.",
            contentWidth));
        AddGap(flow, 14);

        // ── Designed to be ────────────────────────────────────────────────────
        flow.Controls.Add(MakeSectionHeader("Designed to be:"));
        flow.Controls.Add(MakeBullets(contentWidth,
            "Fast",
            "Minimal",
            "Reliable",
            "Invisible during everyday work"));
        AddGap(flow, 10);

        // ── Features ─────────────────────────────────────────────────────────
        flow.Controls.Add(MakeSectionHeader("Features:"));
        flow.Controls.Add(MakeBullets(contentWidth,
            "Global hotkey support",
            "Current line auto-detection",
            "Hebrew ↔ English conversion",
            "Tray application support",
            "Lightweight native Windows experience"));
        AddGap(flow, 10);

        // ── Built with ────────────────────────────────────────────────────────
        flow.Controls.Add(MakeSectionHeader("Built with:"));
        flow.Controls.Add(MakeBullets(contentWidth,
            "C#",
            ".NET 8",
            "WinForms"));
        AddGap(flow, 14);

        // ── Privacy statement ─────────────────────────────────────────────────
        var privacyPanel = new Panel
        {
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            Padding       = new Padding(8),
            MinimumSize   = new Size(contentWidth, 1),
            BackColor     = Color.FromArgb(235, 245, 255),
        };
        var privacyLabel = new Label
        {
            Text        =
                "🔒  Privacy: Flipit does not collect, store, transmit, or analyze " +
                "user text or keyboard activity. All processing is performed locally " +
                "on this device.",
            AutoSize    = true,
            MaximumSize = new Size(contentWidth - 16, 0),
            Font        = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor   = Color.FromArgb(0, 70, 127),
        };
        privacyPanel.Controls.Add(privacyLabel);
        flow.Controls.Add(privacyPanel);
        AddGap(flow, 10);

        // ── Separator ─────────────────────────────────────────────────────────
        flow.Controls.Add(new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Size        = new Size(contentWidth, 2),
            AutoSize    = false,
        });
        AddGap(flow, 10);

        // ── Version ───────────────────────────────────────────────────────────
        flow.Controls.Add(new Label
        {
            Text     = "Version: 1.0.0",
            AutoSize = true,
            Font     = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
        });
        AddGap(flow, 2);

        // ── Copyright ─────────────────────────────────────────────────────────
        flow.Controls.Add(new Label
        {
            Text      = "© Flipit",
            AutoSize  = true,
            ForeColor = SystemColors.GrayText,
            Font      = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
        });
        AddGap(flow, 16);

        // ── Close button ─────────────────────────────────────────────────────
        var btnClose = new Button
        {
            Text         = "Close",
            DialogResult = DialogResult.Cancel,
            Size         = new Size(90, 28),
        };
        btnClose.Click += (_, _) => Close();
        flow.Controls.Add(Centered(btnClose, contentWidth));

        scroll.Controls.Add(flow);
        Controls.Add(scroll);
        CancelButton = btnClose;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Label MakeSectionHeader(string text) => new Label
    {
        Text      = text,
        AutoSize  = true,
        Font      = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point),
        ForeColor = SystemColors.GrayText,
    };

    private static Label MakeWrappedLabel(string text, int maxWidth) => new Label
    {
        Text        = text,
        AutoSize    = true,
        MaximumSize = new Size(maxWidth, 0),
        Font        = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
    };

    private static Label MakeBullets(int maxWidth, params string[] items) => new Label
    {
        Text        = string.Join(Environment.NewLine, items.Select(i => $"  • {i}")),
        AutoSize    = true,
        MaximumSize = new Size(maxWidth, 0),
        Font        = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
        Padding     = new Padding(4, 0, 0, 0),
    };

    /// <summary>Centers a control inside a fixed-width transparent wrapper panel.</summary>
    private static Panel Centered(Control ctrl, int containerWidth, int? fixedHeight = null)
    {
        var wrapper = new Panel
        {
            AutoSize    = fixedHeight is null,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(containerWidth, fixedHeight ?? 1),
            Size        = new Size(containerWidth, fixedHeight ?? 1),
        };
        // We rely on VisibleChanged so PreferredSize is computed after layout.
        ctrl.Location = new Point(Math.Max(0, (containerWidth - ctrl.PreferredSize.Width) / 2),
                                  fixedHeight.HasValue ? (fixedHeight.Value - ctrl.Height) / 2 : 0);
        wrapper.Controls.Add(ctrl);
        // Re-center after the control's font/size is finalised.
        wrapper.Layout += (_, _) =>
        {
            int cx = Math.Max(0, (wrapper.Width - ctrl.Width) / 2);
            int cy = fixedHeight.HasValue ? Math.Max(0, (wrapper.Height - ctrl.Height) / 2) : 0;
            if (ctrl.Location != new Point(cx, cy))
                ctrl.Location = new Point(cx, cy);
        };
        return wrapper;
    }

    private static void AddGap(FlowLayoutPanel flow, int height) =>
        flow.Controls.Add(new Panel { Size = new Size(1, height), AutoSize = false });

    // ── Disposal ──────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing) _logo?.Dispose();
        base.Dispose(disposing);
    }
}

