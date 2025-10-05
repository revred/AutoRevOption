// Events.cs — Event proximity tracking

namespace AutoRevOption.Shared.Tvc.SelectTVC;

/// <summary>
/// Tracks proximity to market-moving events
/// </summary>
/// <param name="Earnings">Earnings date (if within window)</param>
/// <param name="Fomc">FOMC meeting dates (if within window)</param>
/// <param name="Cpi">CPI release date (if within window)</param>
/// <param name="Pce">PCE release date (if within window)</param>
public record Events(
    string? Earnings,
    string[]? Fomc,
    string? Cpi,
    string? Pce
);
