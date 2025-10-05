// Candidate.cs — Trade candidate (Prime execution model)

namespace AutoRevOption.Shared.Prime.Models;

/// <summary>
/// Represents a candidate trade opportunity
/// </summary>
/// <param name="Id">Unique candidate identifier</param>
/// <param name="Ticker">Underlying ticker symbol</param>
/// <param name="Type">Strategy type (PCS, CCS, etc.)</param>
/// <param name="Legs">Option legs</param>
/// <param name="Width">Spread width (distance between strikes)</param>
/// <param name="Credit">Credit received (for credit spreads)</param>
/// <param name="Debit">Debit paid (for debit spreads)</param>
/// <param name="ShortDelta">Delta of short leg</param>
/// <param name="IvRank">IV Rank (0-100)</param>
/// <param name="Score">Selection score</param>
/// <param name="Playbook">Playbook identifier</param>
/// <param name="Notes">Selection notes/reasons</param>
public record Candidate(
    string Id,
    string Ticker,
    StrategyType Type,
    List<OptionLeg> Legs,
    decimal Width,
    decimal? Credit,
    decimal? Debit,
    decimal? ShortDelta,
    int IvRank,
    int Score,
    string Playbook,
    List<string> Notes
);
