using Microsoft.Extensions.Logging;

namespace Flipit.Infrastructure;

/// <summary>
/// Writes structured log lines to %LOCALAPPDATA%\Flipit\flipit.log.
/// Each run appends; the file is human-readable and always up-to-date.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logPath;
    private readonly object _writeLock = new();

    private const long MaxLogBytes = 2 * 1024 * 1024; // 2 MB

    public FileLoggerProvider()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Flipit");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "flipit.log");

        // Rotate if the log is too large before writing a new session header
        try
        {
            if (File.Exists(_logPath) && new FileInfo(_logPath).Length > MaxLogBytes)
            {
                var oldPath = Path.ChangeExtension(_logPath, ".old.log");
                File.Delete(oldPath);
                File.Move(_logPath, oldPath);
            }
        }
        catch { /* rotation failure is non-fatal */ }

        // Write a session header so runs are clearly separated
        var header = $"{Environment.NewLine}═══ Flipit session started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ═══{Environment.NewLine}";
        lock (_writeLock) File.AppendAllText(_logPath, header);
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(_logPath, categoryName, _writeLock);

    public void Dispose() { }
}

internal sealed class FileLogger : ILogger
{
    private readonly string _path;
    private readonly string _tag;      // short class name
    private readonly object _lock;

    public FileLogger(string path, string categoryName, object writeLock)
    {
        _path = path;
        _tag  = categoryName.Split('.').Last();
        _lock = writeLock;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => level >= LogLevel.Information;

    public void Log<TState>(
        LogLevel level, EventId _, TState state,
        Exception? ex, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(level)) return;

        var lvl  = level switch
        {
            LogLevel.Debug       => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning     => "WRN",
            LogLevel.Error       => "ERR",
            _                    => "???",
        };

        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {lvl} [{_tag}] {formatter(state, ex)}";
        if (ex is not null)
            line += $"\n  EX: {ex.Message}";

        lock (_lock)
        {
            try { File.AppendAllText(_path, line + Environment.NewLine); }
            catch { /* never crash on logging failure */ }
        }
    }
}

