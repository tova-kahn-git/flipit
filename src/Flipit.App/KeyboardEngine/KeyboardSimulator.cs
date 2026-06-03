using Flipit.Core;
using Flipit.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Flipit.KeyboardEngine;

/// <summary>
/// Simulates keyboard events using Win32 SendInput.
/// Every public method issues exactly ONE SendInput call.
/// Callers are responsible for placing Thread.Sleep between calls so that
/// the target application has time to process each event before the next arrives.
/// Navigation keys (Home, End) always use KEYEVENTF_EXTENDEDKEY so Windows routes
/// them to the dedicated key rather than the NumPad equivalent.
/// </summary>
public sealed class KeyboardSimulator : IKeyboardSimulator
{
    private readonly ILogger<KeyboardSimulator> _logger;

    public KeyboardSimulator(ILogger<KeyboardSimulator> logger)
    {
        _logger = logger;
    }

    // ── Clipboard ─────────────────────────────────────────────────────────────

    public void SendCopy()  => SendWithCtrl(NativeMethods.VK_C);
    public void SendPaste() => SendWithCtrl(NativeMethods.VK_V);

    // ── Navigation ────────────────────────────────────────────────────────────

    public void SendHome() => SendExtKey(NativeMethods.VK_HOME);
    public void SendEnd()  => SendExtKey(NativeMethods.VK_END);

    // ── Modifier primitives ───────────────────────────────────────────────────

    public void SendShiftDown() => SendOne(KeyDown(NativeMethods.VK_SHIFT));
    public void SendShiftUp()   => SendOne(KeyUp(NativeMethods.VK_SHIFT));

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SendWithCtrl(ushort vk)
    {
        Send(new[]
        {
            KeyDown(NativeMethods.VK_CONTROL),
            KeyDown(vk),
            KeyUp(vk),
            KeyUp(NativeMethods.VK_CONTROL),
        });
    }

    private void SendExtKey(ushort vk)
    {
        Send(new[] { ExtKeyDown(vk), ExtKeyUp(vk) });
    }

    private void SendOne(NativeMethods.INPUT input) => Send(new[] { input });

    private void Send(NativeMethods.INPUT[] inputs)
    {
        uint sent = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());

        if (sent != inputs.Length)
            _logger.LogWarning("SendInput: requested {Total}, accepted {Sent}", inputs.Length, sent);
    }

    // ── INPUT constructors ────────────────────────────────────────────────────

    private static NativeMethods.INPUT KeyDown(ushort vk) => MakeKey(vk, 0);
    private static NativeMethods.INPUT KeyUp(ushort vk)   => MakeKey(vk, NativeMethods.KEYEVENTF_KEYUP);

    private static NativeMethods.INPUT ExtKeyDown(ushort vk) =>
        MakeKey(vk, NativeMethods.KEYEVENTF_EXTENDEDKEY);

    private static NativeMethods.INPUT ExtKeyUp(ushort vk) =>
        MakeKey(vk, NativeMethods.KEYEVENTF_KEYUP | NativeMethods.KEYEVENTF_EXTENDEDKEY);

    private static NativeMethods.INPUT MakeKey(ushort vk, uint flags) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT { wVk = vk, dwFlags = flags }
        }
    };
}
