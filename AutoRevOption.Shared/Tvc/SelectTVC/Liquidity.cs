// Liquidity.cs — Liquidity quality metrics

namespace AutoRevOption.Shared.Tvc.SelectTVC;

/// <summary>
/// Liquidity quality metrics for an option spread
/// </summary>
/// <param name="Oi">Open Interest</param>
/// <param name="BidAsk">Bid-ask spread (absolute)</param>
/// <param name="BidAskPctOfCredit">Bid-ask as percentage of credit</param>
public record Liquidity(
    int Oi,
    decimal BidAsk,
    decimal BidAskPctOfCredit
);
