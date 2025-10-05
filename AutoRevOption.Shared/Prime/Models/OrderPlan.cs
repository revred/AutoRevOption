// OrderPlan.cs — Order execution plan with exits (Prime execution model)

namespace AutoRevOption.Shared.Prime.Models;

/// <summary>
/// Order plan with entry and exit brackets
/// </summary>
public record OrderPlan(
    string CandidateId,
    string OrderPlanId,
    Combo Combination,
    Exits Exits
);

/// <summary>
/// Entry combo order
/// </summary>
public record Combo(
    string Route,
    string TimeInForce,
    List<OptionLeg> Legs
);

/// <summary>
/// Exit brackets (Take Profit and Stop Loss)
/// </summary>
public record Exits(
    TakeProfit Tp,
    StopLoss Sl
);

/// <summary>
/// Take Profit exit order
/// </summary>
public record TakeProfit(
    string Type,
    string Side,
    string CreditPct,
    string Tif,
    string OcaGroup
);

/// <summary>
/// Stop Loss exit order
/// </summary>
public record StopLoss(
    string Type,
    string Side,
    string Trigger,
    string Tif,
    string OcaGroup
);
