using System.Runtime.InteropServices;
using Flipit.Core;
using Flipit.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Flipit.Clipboard;

/// <summary>
/// Clipboard access using Win32 APIs directly for reliability.
/// Uses retry logic because the clipboard can be temporarily locked.
///
/// SECURITY NOTES:
/// - Clipboard content is NEVER logged.
/// - No sentinel or marker values are written to the clipboard.
/// - GlobalLock failures are logged as warnings (without the content).
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private readonly ILogger<ClipboardService> _logger;
    private const int MaxRetries   = 5;
    private const int RetryDelayMs = 20;

    public ClipboardService(ILogger<ClipboardService> logger)
    {
        _logger = logger;
    }

    public uint GetSequenceNumber() => NativeMethods.GetClipboardSequenceNumber();

    public string? GetText()
    {
        var result = RetryClipboard<string?>("read", () =>
        {
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
                return ClipboardResult<string?>.Locked();

            try
            {
                var hData = NativeMethods.GetClipboardData(NativeMethods.CF_UNICODETEXT);
                if (hData == IntPtr.Zero)
                    return ClipboardResult<string?>.Success(null); // no text on clipboard

                var ptr = NativeMethods.GlobalLock(hData);
                if (ptr == IntPtr.Zero)
                {
                    _logger.LogWarning(
                        "GlobalLock failed for clipboard handle — Win32 error {Error}",
                        Marshal.GetLastWin32Error());
                    return ClipboardResult<string?>.Success(null);
                }

                try   { return ClipboardResult<string?>.Success(Marshal.PtrToStringUni(ptr)); }
                finally { NativeMethods.GlobalUnlock(hData); }
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
        });

        return result.Value;
    }

    public bool SetText(string text)
    {
        var result = RetryClipboard<bool>("write", () =>
        {
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
                return ClipboardResult<bool>.Locked();

            try
            {
                NativeMethods.EmptyClipboard();

                // Allocate global memory for the Unicode string (including null terminator)
                var bytes = System.Text.Encoding.Unicode.GetByteCount(text) + 2;
                var hMem  = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)bytes);
                if (hMem == IntPtr.Zero)
                    return ClipboardResult<bool>.Success(false);

                var ptr = NativeMethods.GlobalLock(hMem);
                if (ptr == IntPtr.Zero)
                {
                    NativeMethods.GlobalFree(hMem);
                    return ClipboardResult<bool>.Success(false);
                }

                try
                {
                    Marshal.Copy(System.Text.Encoding.Unicode.GetBytes(text + "\0"), 0, ptr, bytes);
                }
                finally
                {
                    NativeMethods.GlobalUnlock(hMem);
                }

                var handle = NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, hMem);
                if (handle == IntPtr.Zero)
                {
                    // SetClipboardData failed — we still own hMem, so free it
                    NativeMethods.GlobalFree(hMem);
                    return ClipboardResult<bool>.Success(false);
                }
                // On success the clipboard owns hMem; do NOT free it
                return ClipboardResult<bool>.Success(true);
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
        });

        return result.Value;
    }

    public bool Clear()
    {
        var result = RetryClipboard<bool>("clear", () =>
        {
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
                return ClipboardResult<bool>.Locked();

            try   { return ClipboardResult<bool>.Success(NativeMethods.EmptyClipboard()); }
            finally { NativeMethods.CloseClipboard(); }
        });

        return result.Value;
    }

    // ── Retry helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Retries a clipboard operation up to <see cref="MaxRetries"/> times.
    /// The operation returns <see cref="ClipboardResult{T}.Locked"/> to signal
    /// that <c>OpenClipboard</c> failed and the attempt should be retried.
    /// Any other result (including null values) is returned immediately.
    /// </summary>
    private ClipboardResult<T> RetryClipboard<T>(string operationName,
        Func<ClipboardResult<T>> operation)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var result = operation();
                if (!result.IsLocked) return result;

                // Clipboard was locked by another app — wait and retry
                Thread.Sleep(RetryDelayMs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Clipboard {Operation} attempt {Attempt} failed",
                    operationName, attempt);
                Thread.Sleep(RetryDelayMs);
            }
        }

        _logger.LogError("Failed to {Operation} clipboard after {MaxRetries} attempts",
            operationName, MaxRetries);
        return ClipboardResult<T>.Success(default!);
    }

    // ── Result wrapper ────────────────────────────────────────────────────────

    private readonly record struct ClipboardResult<T>(bool IsLocked, T Value)
    {
        public static ClipboardResult<T> Locked()          => new(true,  default!);
        public static ClipboardResult<T> Success(T value)  => new(false, value);
    }
}
