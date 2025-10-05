using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoRevOption.Minimal.Services;

public sealed class WriteTvcService : IWriteTvcService
{
    public async Task<ExecutionCard> ActAsync(ExecutionRequest req, CancellationToken ct)
    {
        // TODO: load TVC JSON, perform admissibility checks, and (optionally) call broker preview/place.
        await Task.Yield();
        var legs = new List<Leg> {
            new("SELL","PUT", 6.5m, "2025-10-10"),
            new("BUY", "PUT", 5.5m, "2025-10-10")
        };
        var admissibility = new {
            maint_pct_ok = true,
            defined_risk_ok = true,
            symbol_exposure_ok = true,
            fresh_enough = true,
            credit_drift_ok = true,
            reasons = new string[0]
        };
        var actionResult = new {
            status = "STAGED",
            order_id = (string?)null,
            notes = "Dry wiring; broker not called"
        };
        return new ExecutionCard(
            Mode: req.Mode,
            TvcRef: req.TvcPath,
            Symbol: "SOFI",
            Strategy: "PUT_CREDIT_SPREAD",
            Legs: legs,
            IntendedCreditLimit: 0.38m,
            Brackets: new { tp_pct = 50, sl_multiple_credit = 2.0m, time_in_force = "GTC" },
            Admissibility: admissibility,
            BrokerPreview: null,
            ActionResult: actionResult
        );
    }
}
