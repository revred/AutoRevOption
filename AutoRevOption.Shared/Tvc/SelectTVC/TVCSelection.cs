// TVCSelection.cs — Trade Vet Card selection artifact (per SelectTVC.md spec)

using AutoRevOption.Shared.Tvc.Common;

namespace AutoRevOption.Shared.Tvc.SelectTVC;

/// <summary>
/// Trade Vet Card - Selection artifact
/// Represents a vetted and scored trade candidate with all selection metrics
/// </summary>
/// <param name="TvcVersion">TVC schema version</param>
/// <param name="TimestampUtc">UTC timestamp of selection</param>
/// <param name="Symbol">Underlying ticker symbol</param>
/// <param name="Strategy">Strategy type (PUT_CREDIT_SPREAD, etc.)</param>
/// <param name="Legs">Option legs</param>
/// <param name="Spot">Current spot price of underlying</param>
/// <param name="DteCalendar">Calendar days to expiration</param>
/// <param name="DeltaShort">Delta of short leg</param>
/// <param name="Iv">Implied volatility</param>
/// <param name="Ivr">IV Rank (0-100)</param>
/// <param name="CreditGross">Gross credit received</param>
/// <param name="FeesOpen">Fees to open position</param>
/// <param name="CreditNetOpen">Net credit after open fees</param>
/// <param name="Width">Spread width (distance between strikes)</param>
/// <param name="RiskCapital">Capital at risk (width - creditGross)</param>
/// <param name="Pop">Probability of Profit at breakeven</param>
/// <param name="RewardPerDayPct">Daily reward percentage on risk capital</param>
/// <param name="Liquidity">Liquidity metrics</param>
/// <param name="Events">Event dates (earnings, FOMC, etc.)</param>
/// <param name="Selection">Selection result (pass/fail, reasons, score)</param>
/// <param name="HumanSummary">One-line human-readable summary</param>
public record TVCSelection(
    string TvcVersion,
    DateTime TimestampUtc,
    string Symbol,
    string Strategy,
    IReadOnlyList<Leg> Legs,
    decimal Spot,
    int DteCalendar,
    decimal DeltaShort,
    decimal Iv,
    int Ivr,
    decimal CreditGross,
    decimal FeesOpen,
    decimal CreditNetOpen,
    decimal Width,
    decimal RiskCapital,
    decimal Pop,
    decimal RewardPerDayPct,
    Liquidity Liquidity,
    Events Events,
    SelectionResult Selection,
    string HumanSummary
);
