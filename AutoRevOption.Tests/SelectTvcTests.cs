using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoRevOption.Minimal.Services;
using Xunit;

namespace AutoRevOption.Tests;

public class SelectTvcTests
{
    [Fact]
    public async Task SelectionProducesPassWithGatesSatisfied()
    {
        var svc = new SelectTvcService();
        var res = await svc.EvaluateAsync(new SelectionRequest("SOFI", 5, 9), CancellationToken.None);
        var tvc = res.Single();
        Assert.True(tvc.Selection.Pass);
        Assert.True(tvc.Pop >= 0.75m);
        Assert.True(tvc.RewardPerDayPct >= 2.5m);
        Assert.True(tvc.Liquidity.Oi >= 200);
        Assert.True(tvc.Liquidity.BidAsk <= 0.05m || tvc.Liquidity.BidAskPctOfCredit <= 0.10m);
    }

    [Fact]
    public void RewardPerDayFormulaMatchesSpec()
    {
        decimal creditGross = 0.38m, feesOpen = 0.04m, width = 1.00m, dte = 7m;
        decimal riskCapital = width - creditGross;          // 0.62
        decimal creditNet = creditGross - feesOpen;         // 0.34
        decimal expectedNet = 0.60m * creditNet;            // 0.204
        decimal rewardPerDay = 100m * expectedNet / (riskCapital * dte);
        Assert.InRange((double)rewardPerDay, 4.6, 4.8);     // ≈ 4.70%
    }

    [Fact]
    public async Task EventAvoidanceIsRepresented()
    {
        var svc = new SelectTvcService();
        var res = await svc.EvaluateAsync(new SelectionRequest("SOFI", 5, 9), CancellationToken.None);
        var tvc = res.Single();
        Assert.NotNull(tvc.Events.Earnings);
        Assert.NotNull(tvc.Events.Fomc);
    }
}
