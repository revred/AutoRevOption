// OrderBuilder.cs — Build combo orders with OCA exit brackets

using System.Text.Json;
using AutoRevOption.Shared.Prime.Models;

namespace AutoRevOption.Shared.Execution;

public record ExitPolicy(decimal TpMinPct, decimal TpMaxPct, decimal SlMultiplier);
public record OrderRequest(string Route, string TifEntry, string TifExit, List<OptionLeg> Legs, int Quantity);
public record OcaBracket(string OcaGroup, LimitOrder TakeProfit, StopOrder StopLoss);
public record LimitOrder(string Side, decimal LimitPrice, string Tif, string OcaGroup);
public record StopOrder(string Side, decimal StopPrice, string Trigger, string Tif, string OcaGroup);

/// <summary>
/// Order builder with OCA (One-Cancels-All) exit brackets
/// </summary>
public class OrderBuilder
{
    private readonly ExitPolicy _exitPolicy;
    private readonly string _defaultRoute;
    private readonly string _defaultTifEntry;
    private readonly string _defaultTifExit;

    public OrderBuilder(ExitPolicy exitPolicy, string route = "SMART", string tifEntry = "DAY", string tifExit = "GTC")
    {
        _exitPolicy = exitPolicy;
        _defaultRoute = route;
        _defaultTifEntry = tifEntry;
        _defaultTifExit = tifExit;
    }

    /// <summary>
    /// Build order plan from candidate with TP/SL exits
    /// </summary>
    public OrderPlan BuildOrderPlan(Candidate candidate, int quantity = 1)
    {
        // Generate OCA group ID
        var ocaGroup = $"OCA-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Scale legs by quantity
        var scaledLegs = candidate.Legs.Select(leg => leg with { Quantity = quantity }).ToList();

        // Build combo
        var combo = new Combo(_defaultRoute, _defaultTifEntry, scaledLegs);

        // Build exits based on strategy type
        var exits = candidate.Type switch
        {
            StrategyType.PCS or StrategyType.CCS => BuildCreditExits(candidate, quantity, ocaGroup),
            StrategyType.BPS or StrategyType.BCS => BuildDebitExits(candidate, quantity, ocaGroup),
            _ => BuildDefaultExits(ocaGroup)
        };

        var orderPlanId = $"OP-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        return new OrderPlan(candidate.Id, orderPlanId, combo, exits);
    }

    /// <summary>
    /// Build exits for credit spreads (PCS/CCS)
    /// TP: 50-60% of credit | SL: 2x credit or short strike touch
    /// </summary>
    private Exits BuildCreditExits(Candidate candidate, int quantity, string ocaGroup)
    {
        if (!candidate.Credit.HasValue)
            throw new ArgumentException("Credit spreads must have a Credit value");

        var creditReceived = candidate.Credit.Value * quantity * 100; // Convert to dollars per contract

        // Take profit: 50-60% of credit
        var tpMin = creditReceived * _exitPolicy.TpMinPct;
        var tpMax = creditReceived * _exitPolicy.TpMaxPct;
        var tpTarget = (tpMin + tpMax) / 2; // Use midpoint

        // Stop loss: 2x credit
        var slPrice = creditReceived * _exitPolicy.SlMultiplier;

        // Get short strike for touch monitoring
        var shortLeg = candidate.Legs.FirstOrDefault(l => l.Action == "SELL");
        var shortStrike = shortLeg?.Strike ?? 0;

        var tp = new TakeProfit(
            Type: "LIMIT",
            Side: "BUY_TO_CLOSE",
            CreditPct: $"{_exitPolicy.TpMinPct:P0}-{_exitPolicy.TpMaxPct:P0} (${tpTarget:F2})",
            Tif: _defaultTifExit,
            OcaGroup: ocaGroup
        );

        var sl = new StopLoss(
            Type: "STOP",
            Side: "BUY_TO_CLOSE",
            Trigger: $"Loss >= ${slPrice:F2} OR {shortLeg?.Right} {shortStrike} touched",
            Tif: _defaultTifExit,
            OcaGroup: ocaGroup
        );

        return new Exits(tp, sl);
    }

    /// <summary>
    /// Build exits for debit spreads (BPS/BCS)
    /// TP: Target R:R (e.g., 2:1) | SL: Full debit loss
    /// </summary>
    private Exits BuildDebitExits(Candidate candidate, int quantity, string ocaGroup)
    {
        if (!candidate.Debit.HasValue)
            throw new ArgumentException("Debit spreads must have a Debit value");

        var debitPaid = candidate.Debit.Value * quantity * 100;
        var maxProfit = (candidate.Width - candidate.Debit.Value) * quantity * 100;

        // Take profit: Max profit or partial (80% of max)
        var tpTarget = maxProfit * 0.80m;

        // Stop loss: Full debit (let it go to near zero)
        var slPrice = debitPaid * 0.90m; // 90% loss

        var tp = new TakeProfit(
            Type: "LIMIT",
            Side: "SELL_TO_CLOSE",
            CreditPct: $"80% max profit (${tpTarget:F2})",
            Tif: _defaultTifExit,
            OcaGroup: ocaGroup
        );

        var sl = new StopLoss(
            Type: "STOP",
            Side: "SELL_TO_CLOSE",
            Trigger: $"Loss >= ${slPrice:F2} (90% of debit)",
            Tif: _defaultTifExit,
            OcaGroup: ocaGroup
        );

        return new Exits(tp, sl);
    }

    /// <summary>
    /// Default exits for unsupported strategies
    /// </summary>
    private Exits BuildDefaultExits(string ocaGroup)
    {
        var tp = new TakeProfit("MANUAL", "CLOSE", "Manual management", "GTC", ocaGroup);
        var sl = new StopLoss("MANUAL", "CLOSE", "Manual management", "GTC", ocaGroup);
        return new Exits(tp, sl);
    }

    /// <summary>
    /// Calculate actual OCA bracket orders (for IBKR submission)
    /// </summary>
    public OcaBracket CalculateOcaBracket(Candidate candidate, int quantity, decimal currentBidAsk)
    {
        var ocaGroup = $"OCA-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        if (candidate.Type is StrategyType.PCS or StrategyType.CCS)
        {
            if (!candidate.Credit.HasValue)
                throw new ArgumentException("Credit required for PCS/CCS");

            var creditReceived = candidate.Credit.Value * quantity;
            var tpPrice = creditReceived * 0.55m; // 55% of credit (midpoint of 50-60%)
            var slPrice = creditReceived * _exitPolicy.SlMultiplier;

            var tp = new LimitOrder("BUY_TO_CLOSE", tpPrice, _defaultTifExit, ocaGroup);
            var sl = new StopOrder("BUY_TO_CLOSE", slPrice, "LOSS_LIMIT", _defaultTifExit, ocaGroup);

            return new OcaBracket(ocaGroup, tp, sl);
        }
        else if (candidate.Type is StrategyType.BPS or StrategyType.BCS)
        {
            if (!candidate.Debit.HasValue)
                throw new ArgumentException("Debit required for BPS/BCS");

            var maxProfit = (candidate.Width - candidate.Debit.Value) * quantity;
            var tpPrice = maxProfit * 0.80m;
            var slPrice = candidate.Debit.Value * quantity * 0.10m; // Exit at 90% loss

            var tp = new LimitOrder("SELL_TO_CLOSE", tpPrice, _defaultTifExit, ocaGroup);
            var sl = new StopOrder("SELL_TO_CLOSE", slPrice, "LOSS_LIMIT", _defaultTifExit, ocaGroup);

            return new OcaBracket(ocaGroup, tp, sl);
        }

        throw new NotSupportedException($"Strategy type {candidate.Type} not supported for OCA brackets");
    }

    /// <summary>
    /// Export order plan as JSON
    /// </summary>
    public string ToJson(OrderPlan plan)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(plan, options);
    }
}
