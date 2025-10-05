using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoRevOption.Minimal.Services;

public sealed class SelectTvcService : ISelectTvcService
{
    public async Task<IReadOnlyList<TVCSelection>> EvaluateAsync(SelectionRequest req, CancellationToken ct)
    {
        // TODO: load OptionsRadar.yaml and query MCP endpoints for chains and events.
        await Task.Yield();
        var legs = new List<Leg> {
            new("SELL","PUT", 6.5m, "2025-10-10"),
            new("BUY", "PUT", 5.5m, "2025-10-10")
        };
        var liq = new Liquidity(2100, 0.03m, 0.079m);
        var ev  = new Events("2025-10-28", new[]{ "2025-10-28","2025-10-29" }, null, "2025-10-31");
        var reasons = new List<string>
        {
            "POP 78% ≥ 75%",
            "Reward/Day 4.7% ≥ 2.5%",
            "Δshort 0.22 within [0.20,0.25]",
            "Liquidity OK",
            "IVR 33 ≥ 30"
        };
        var sel = new SelectionResult(true, reasons, 0.82m);
        return new[] {
            new TVCSelection(
                Symbol: "SOFI",
                Strategy: "PUT_CREDIT_SPREAD",
                Legs: legs,
                Spot: 6.98m,
                DteCalendar: 7,
                DeltaShort: 0.22m,
                Iv: 0.46m,
                Ivr: 33,
                CreditGross: 0.38m,
                FeesOpen: 0.04m,
                CreditNetOpen: 0.34m,
                Width: 1.0m,
                RiskCapital: 0.62m,
                Pop: 0.78m,
                RewardPerDayPct: 4.7m,
                Liquidity: liq,
                Events: ev,
                Selection: sel,
                HumanSummary: "SOFI PCS 6.5/5.5 (7DTE) — POP 78%, Reward/Day 4.7%, OI 2.1k, $0.03 spread; ER 10/28, FOMC 10/28–29, PCE 10/31 — PASS"
            )
        };
    }
}
