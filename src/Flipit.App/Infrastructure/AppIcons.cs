namespace Flipit.Infrastructure;

/// <summary>
/// Centralized access to all application icons and images.
///
/// flipit.ico      — embedded resource → tray icon and process icon.
/// flipit-256.png  — embedded resource (primary) + loose file (fallback)
///                   → splash, about window, settings branding header.
/// flipit-512.png  — same dual strategy as flipit-256.png.
///
/// Embedding the PNGs ensures they are always available inside the single-file
/// EXE produced by:
///   dotnet publish -c Release -r win-x64 --self-contained true
///                  /p:PublishSingleFile=true
/// Without embedding, <Content> files are published as loose files alongside
/// the EXE; if only the EXE is distributed the images cannot be found.
///
/// All public methods return a new object per call; callers are responsible
/// for disposal where required (Icon, Image).
/// </summary>
public static class AppIcons
{
    // Embedded resource names (MSBuild format: AssemblyName.FolderDots.FileName)
    private const string IcoResourceName  = "Flipit.Assets.Icons.flipit.ico";
    private const string Png256ResName    = "Flipit.Assets.Icons.flipit-256.png";
    private const string Png512ResName    = "Flipit.Assets.Icons.flipit-512.png";

    // ── Tray / process icon (embedded .ico) ──────────────────────────────────

    /// <summary>
    /// Loads the application icon from the embedded resource.
    /// Returns <see cref="SystemIcons.Application"/> as a safe fallback.
    /// The caller owns the returned <see cref="Icon"/> and must dispose it
    /// when it is no longer needed (except for the SystemIcons fallback,
    /// which must NOT be disposed).
    /// </summary>
    public static Icon LoadAppIcon()
    {
        try
        {
            var stream = typeof(AppIcons).Assembly
                .GetManifestResourceStream(IcoResourceName);

            if (stream is not null)
                return new Icon(stream);
        }
        catch { /* fall through to system icon */ }

        return SystemIcons.Application;
    }

    /// <summary>
    /// Returns true when the full application icon is available (i.e. the
    /// embedded resource was found).  Useful for diagnostics.
    /// </summary>
    public static bool IsAppIconAvailable =>
        typeof(AppIcons).Assembly
            .GetManifestResourceNames()
            .Contains(IcoResourceName);

    // ── Settings / branding image ─────────────────────────────────────────────

    /// <summary>
    /// Loads <c>flipit-256.png</c>.
    /// Tries the embedded assembly resource first (always present in a
    /// single-file publish), then falls back to the loose file next to the
    /// EXE for Debug / non-single-file layouts.
    /// Returns null if neither source is available.
    /// The caller owns the returned <see cref="Image"/> and must dispose it.
    /// </summary>
    public static Image? LoadBrandingImage()  => LoadPng(Png256ResName, "flipit-256.png");

    /// <summary>
    /// Loads <c>flipit-512.png</c>. Same fallback strategy as
    /// <see cref="LoadBrandingImage"/>.
    /// </summary>
    public static Image? LoadLargeBrandingImage() => LoadPng(Png512ResName, "flipit-512.png");

    // ── Paths ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Absolute path to the Icons asset folder next to the running .exe.
    /// </summary>
    public static string IconsFolder =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");

    // ── Private ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads an image bitmap.
    /// 1. Tries the embedded assembly resource (<paramref name="resourceName"/>).
    /// 2. Falls back to <paramref name="fileName"/> next to the EXE.
    /// Returns null when both strategies fail (graceful degradation).
    /// </summary>
    private static Image? LoadPng(string resourceName, string fileName)
    {
        // ── Strategy 1: embedded resource (single-file safe) ──────────────────
        try
        {
            var stream = typeof(AppIcons).Assembly
                .GetManifestResourceStream(resourceName);
            if (stream is not null)
                return new Bitmap(stream);
        }
        catch { /* fall through */ }

        // ── Strategy 2: loose file next to the EXE (Debug / non-single-file) ──
        try
        {
            var path = Path.Combine(IconsFolder, fileName);
            if (File.Exists(path))
                return new Bitmap(path);
        }
        catch { /* graceful fallback */ }

        return null;
    }
}
