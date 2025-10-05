// Brackets.cs — Exit bracket configuration

namespace AutoRevOption.Shared.Tvc.WriteTVC;

/// <summary>
/// Exit bracket configuration (Take Profit and Stop Loss)
/// </summary>
/// <param name="TpPct">Take profit percentage of credit (e.g., 50 = close at 50% of credit)</param>
/// <param name="SlMultipleCredit">Stop loss multiple of credit (e.g., 2.0 = stop at 2x credit loss)</param>
/// <param name="TimeInForce">Time in force for brackets (GTC, DAY, etc.)</param>
public record Brackets(
    decimal TpPct,
    decimal SlMultipleCredit,
    string TimeInForce
);
