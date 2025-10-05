// ActionResult.cs — Action result (order placement outcome)

namespace AutoRevOption.Shared.Tvc.WriteTVC;

/// <summary>
/// Result of action taken on execution card
/// </summary>
/// <param name="Status">Status (PLACED, QUEUED, REJECTED, STAGED, DRY_RUN)</param>
/// <param name="OrderId">Broker order ID (if placed)</param>
/// <param name="Notes">Additional notes about the action</param>
public record ActionResult(
    string Status,
    string? OrderId,
    string Notes
);
