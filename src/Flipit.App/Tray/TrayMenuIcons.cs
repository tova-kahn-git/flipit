using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Flipit.Tray;

/// <summary>
/// Generates the small icon bitmaps used in the tray context menu.
///
/// Strategy (highest quality first):
///   1. Segoe MDL2 Assets   — ships with Windows 10/11; crisp vector glyphs.
///   2. Segoe UI Symbol      — ships with Windows 7+; common Unicode symbols.
///   3. Hand-drawn GDI+      — always available; simple geometric fallbacks.
///
/// All bitmaps are 32-bpp ARGB with a transparent background so they render
/// cleanly against any menu background colour.
///
/// Callers are responsible for disposing every bitmap returned.
/// </summary>
internal static class TrayMenuIcons
{
    // ── Segoe MDL2 Assets PUA code points ────────────────────────────────────
    // These are the same glyphs used by Windows shell, PowerToys, Settings, etc.
    private const string GlyphCheckmark = "\uE73E"; // CheckMark
    private const string GlyphSettings  = "\uE713"; // Gear / Settings
    private const string GlyphInfo      = "\uE946"; // Information circle
    private const string GlyphClose     = "\uE711"; // Cancel / X

    // ── Segoe UI Symbol Unicode fallbacks ────────────────────────────────────
    private const string FbCheckmark    = "\u2713"; // ✓  CHECK MARK
    private const string FbSettings     = "\u2699"; // ⚙  GEAR
    private const string FbInfo         = "\u2139"; // ℹ  INFORMATION SOURCE
    private const string FbClose        = "\u2715"; // ✕  MULTIPLICATION X

    // ── Singleton font availability cache ────────────────────────────────────
    private static readonly bool _hasMdl2    = FontFamilyExists("Segoe MDL2 Assets");
    private static readonly bool _hasSymbol  = FontFamilyExists("Segoe UI Symbol");

    // ── Public factory methods ────────────────────────────────────────────────

    /// <summary>Checkmark icon — shown on the Enabled item only when the app is active.</summary>
    public static Bitmap EnabledOn(int size)  => GlyphBitmap(GlyphCheckmark, FbCheckmark, Color.FromArgb(72, 72, 72), size);

    /// <summary>Gear icon for "Settings".</summary>
    public static Bitmap Settings(int size)   => GlyphBitmap(GlyphSettings, FbSettings, Color.FromArgb(72, 72, 72), size);

    /// <summary>Info circle for "About".</summary>
    public static Bitmap About(int size)      => GlyphBitmap(GlyphInfo, FbInfo,         Color.FromArgb(72, 72, 72), size);

    /// <summary>X icon for "Exit".</summary>
    public static Bitmap Exit(int size)       => GlyphBitmap(GlyphClose, FbClose,       Color.FromArgb(72, 72, 72), size);

    // ── Private helpers ───────────────────────────────────────────────────────
    /// Tries Segoe MDL2 Assets first, then Segoe UI Symbol, then a hand-drawn shape.
    /// </summary>
    private static Bitmap GlyphBitmap(string mdl2Glyph, string symbolFallback,
                                       Color color, int size)
    {
        string glyph;
        string fontFamily;

        if (_hasMdl2)
        {
            glyph      = mdl2Glyph;
            fontFamily = "Segoe MDL2 Assets";
        }
        else if (_hasSymbol)
        {
            glyph      = symbolFallback;
            fontFamily = "Segoe UI Symbol";
        }
        else
        {
            // No suitable symbol font — fall back to a simple drawn shape
            return FallbackGlyph(symbolFallback, size, color);
        }

        var bmp = NewBitmap(size);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // Scale font so the glyph fills roughly 75 % of the bitmap height
        float em = size * 0.72f;
        using var font  = new Font(fontFamily, em, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);

        var textSz = g.MeasureString(glyph, font);
        float x = (size - textSz.Width)  / 2f;
        float y = (size - textSz.Height) / 2f;
        g.DrawString(glyph, font, brush, x, y);
        return bmp;
    }

    /// <summary>
    /// Plain GDI+ fallback when no symbol font is available.
    /// Renders the Unicode character using Segoe UI (always present on Vista+).
    /// </summary>
    private static Bitmap FallbackGlyph(string text, int size, Color color)
    {
        var bmp = NewBitmap(size);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        float em = size * 0.65f;
        using var font  = new Font("Segoe UI", em, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);

        var sz = g.MeasureString(text, font);
        g.DrawString(text, font, brush,
            (size - sz.Width)  / 2f,
            (size - sz.Height) / 2f);
        return bmp;
    }

    private static Bitmap NewBitmap(int size)
    {
        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        return bmp;
    }

    private static bool FontFamilyExists(string name)
    {
        try
        {
            using var ff = new FontFamily(name);
            return true;
        }
        catch
        {
            return false;
        }
    }
}





