// OptionLeg.cs — Option leg definition (Legacy)

namespace AutoRevOption.Shared.Models.Legacy;

/// <summary>
/// Represents a single option leg in a multi-leg strategy
/// </summary>
/// <param name="Action">BUY or SELL</param>
/// <param name="Right">CALL or PUT</param>
/// <param name="Strike">Strike price</param>
/// <param name="Exp">Expiration date</param>
/// <param name="Quantity">Number of contracts (default: 1)</param>
public record OptionLeg(
    string Action,
    string Right,
    decimal Strike,
    DateOnly Exp,
    int Quantity = 1
);
