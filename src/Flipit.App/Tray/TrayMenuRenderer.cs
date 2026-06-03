using System.Drawing.Drawing2D;

namespace Flipit.Tray;

// ── Color table ───────────────────────────────────────────────────────────────

/// <summary>
/// ProfessionalColorTable that replaces the default Windows XP–era gradients
/// with a clean, flat palette inspired by Windows 11 context menus.
///
/// Key choices:
///   • Pure-white background removes the vintage gray stripe.
///   • Single flat hover colour (no gradient) keeps the look modern.
///   • Image-margin background matches the menu background so the icon column
///     blends in seamlessly — just like PowerToys / EarTrumpet.
///   • Border is a very soft gray so the popup looks floated, not boxed.
/// </summary>
internal sealed class FlipitColorTable : ProfessionalColorTable
{
    // ── Named palette entries ─────────────────────────────────────────────────

    internal static readonly Color MenuBg         = Color.White;
    internal static readonly Color HoverBg        = Color.FromArgb(229, 229, 229);
    internal static readonly Color PressedBg      = Color.FromArgb(210, 210, 210);
    internal static readonly Color BorderColor     = Color.FromArgb(200, 200, 200);
    internal static readonly Color SepColor        = Color.FromArgb(218, 218, 218);
    internal static readonly Color HeaderBg        = Color.FromArgb(246, 246, 246);
    internal static readonly Color TextPrimary     = Color.FromArgb(28,  28,  28);
    internal static readonly Color TextSecondary   = Color.FromArgb(120, 120, 120);

    // ── ProfessionalColorTable overrides ──────────────────────────────────────

    // Menu surface
    public override Color ToolStripDropDownBackground        => MenuBg;
    public override Color MenuBorder                         => BorderColor;
    public override Color MenuItemBorder                     => Color.Transparent;   // no per-item border ring

    // Hover / pressed states — flat (begin == end == middle)
    public override Color MenuItemSelected                   => HoverBg;
    public override Color MenuItemSelectedGradientBegin      => HoverBg;
    public override Color MenuItemSelectedGradientEnd        => HoverBg;
    public override Color MenuItemPressedGradientBegin       => PressedBg;
    public override Color MenuItemPressedGradientEnd         => PressedBg;
    public override Color MenuItemPressedGradientMiddle      => PressedBg;

    // Image / check margin — match the menu background so no gray stripe appears
    public override Color ImageMarginGradientBegin           => MenuBg;
    public override Color ImageMarginGradientEnd             => MenuBg;
    public override Color ImageMarginGradientMiddle          => MenuBg;
    public override Color ImageMarginRevealedGradientBegin   => MenuBg;
    public override Color ImageMarginRevealedGradientEnd     => MenuBg;
    public override Color ImageMarginRevealedGradientMiddle  => MenuBg;

    // Check-mark background
    public override Color CheckBackground                    => HoverBg;
    public override Color CheckSelectedBackground            => HoverBg;
    public override Color CheckPressedBackground             => PressedBg;

    // Toolbar / menu-bar (not used in a tray app, but keeps everything consistent)
    public override Color MenuStripGradientBegin             => MenuBg;
    public override Color MenuStripGradientEnd               => MenuBg;
    public override Color ToolStripGradientBegin             => MenuBg;
    public override Color ToolStripGradientEnd               => MenuBg;
    public override Color ToolStripGradientMiddle            => MenuBg;
    public override Color ToolStripBorder                    => BorderColor;
    public override Color SeparatorDark                      => SepColor;
    public override Color SeparatorLight                     => MenuBg;
}

// ── Renderer ─────────────────────────────────────────────────────────────────

/// <summary>
/// Lightweight custom renderer for the Flipit tray context menu.
///
/// Builds on <see cref="ToolStripProfessionalRenderer"/> (no full owner-draw)
/// and adds only the targeted overrides needed to achieve a clean, modern look:
///
///   • Flat white background — no gradients, no XP chrome.
///   • Rounded-rectangle hover highlight (4 px corner radius).
///   • Hairline separator rendered with consistent horizontal padding.
///   • Image-margin background suppressed so icons sit against pure white.
///   • Check mark replaced by a simple tick character to stay consistent with
///     the icon style (no Windows 95-era groove square).
///
/// DPI notes:
///   All pixel values that affect visual weight (padding, radius, separator
///   thickness) are intentionally kept in logical units.  WinForms scales
///   the ContextMenuStrip via <see cref="ToolStrip.ImageScalingSize"/> and
///   the system DPI; we piggyback on that instead of doing our own scaling.
/// </summary>
internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer, IDisposable
{
    public TrayMenuRenderer() : base(new FlipitColorTable()) { }
    // ── Background ────────────────────────────────────────────────────────────

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        // Flat white — no gradient, no etched edge.
        e.Graphics.Clear(FlipitColorTable.MenuBg);
    }

    // ── Image margin ──────────────────────────────────────────────────────────

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Paint the image column the same colour as the menu background so it
        // is invisible — icons appear to float against a pure-white surface.
        using var brush = new SolidBrush(FlipitColorTable.MenuBg);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    // ── Hover / pressed item background ──────────────────────────────────────

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;

        // Non-interactive items (header, disabled) receive no highlight.
        if (!item.Enabled || !item.Selected && !item.Pressed)
            return;

        // Rounded rectangle — 4 px radius gives a Windows-11 feel without
        // looking overdone.  We deflate by (2, 1) to leave a 2 px left/right
        // gap and 1 px top/bottom gap from the item edge.
        var bounds = Rectangle.Inflate(new Rectangle(Point.Empty, item.Size), -2, -1);
        var color  = item.Pressed ? FlipitColorTable.PressedBg : FlipitColorTable.HoverBg;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        using var path  = RoundedRect(bounds, 4);
        e.Graphics.FillPath(brush, path);
        e.Graphics.SmoothingMode = SmoothingMode.Default;
    }

    // ── Separator ─────────────────────────────────────────────────────────────

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        // Hairline centered in the separator item height, with horizontal inset.
        var r  = e.Item.ContentRectangle;
        int y  = r.Top + r.Height / 2;
        int x1 = r.Left  + 2;
        int x2 = r.Right - 2;

        using var pen = new Pen(FlipitColorTable.SepColor);
        e.Graphics.DrawLine(pen, x1, y, x2, y);
    }

    // ── Check mark ────────────────────────────────────────────────────────────

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // The Enabled menu item uses an explicit Image that already reflects
        // the checked/unchecked state (green dot / gray dot).  We suppress
        // the default "check box" so it does not double-render.
        // For any other item that happens to have CheckState set, we fall back
        // to the professional renderer's built-in behaviour.
        if (e.Item.Image is not null)
            return;

        base.OnRenderItemCheck(e);
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        // ToolStripRenderer / ToolStripProfessionalRenderer do not implement
        // IDisposable, so there is nothing to forward to.
        GC.SuppressFinalize(this);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d    = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.Left,          r.Top,           d, d, 180, 90);
        path.AddArc(r.Right - d,     r.Top,           d, d, 270, 90);
        path.AddArc(r.Right - d,     r.Bottom - d,    d, d,   0, 90);
        path.AddArc(r.Left,          r.Bottom - d,    d, d,  90, 90);
        path.CloseFigure();
        return path;
    }
}





