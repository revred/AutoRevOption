// BrokerPreview.cs — Broker preview data (optional)

namespace AutoRevOption.Shared.Tvc.WriteTVC;

/// <summary>
/// Broker preview data from POST /orders/preview
/// Optional - may not be available if preview endpoint not called
/// </summary>
/// <param name="MaxLoss">Maximum loss estimate</param>
/// <param name="FeesTotal">Total fees (open + close)</param>
/// <param name="MarginEffect">Margin requirement impact</param>
/// <param name="EstFillProb">Estimated fill probability at limit price</param>
public record BrokerPreview(
    decimal MaxLoss,
    decimal FeesTotal,
    decimal MarginEffect,
    decimal EstFillProb
);
