namespace Flipit.Core;

/// <summary>
/// Orchestrates the full flip workflow: detect selection, copy, convert, paste.
/// </summary>
public interface IFlipOrchestrator
{
    /// <summary>
    /// Executes the flip operation on the currently focused application.
    /// </summary>
    /// <param name="ct">Optional token to cancel an in-flight flip (e.g. on shutdown).</param>
    Task FlipAsync(CancellationToken ct = default);
}

