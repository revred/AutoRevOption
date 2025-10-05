using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoRevOption.Minimal.Services;

public record Leg(string Side, string Right, decimal Strike, string Expiry);
public record Liquidity(int Oi, decimal BidAsk, decimal BidAskPctOfCredit);
public record Events(string? Earnings, string[]? Fomc, string? Cpi, string? Pce);
public record SelectionResult(bool Pass, IReadOnlyList<string> Reasons, decimal Score);

public record TVCSelection(
    string Symbol, string Strategy, IReadOnlyList<Leg> Legs,
    decimal Spot, int DteCalendar, decimal DeltaShort, decimal Iv, int Ivr,
    decimal CreditGross, decimal FeesOpen, decimal CreditNetOpen,
    decimal Width, decimal RiskCapital, decimal Pop, decimal RewardPerDayPct,
    Liquidity Liquidity, Events Events, SelectionResult Selection, string HumanSummary);

public record SelectionRequest(string Symbol, int? DteMin, int? DteMax, string Strategy = "PCS");

public interface ISelectTvcService
{
    Task<IReadOnlyList<TVCSelection>> EvaluateAsync(SelectionRequest req, CancellationToken ct);
}
