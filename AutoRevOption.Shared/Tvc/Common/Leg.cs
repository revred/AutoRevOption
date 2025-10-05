// Leg.cs — Option leg for TVC (common between Select and Write)

namespace AutoRevOption.Shared.Tvc.Common;

/// <summary>
/// Represents a single option leg in a TVC structure
/// </summary>
/// <param name="Side">SELL or BUY</param>
/// <param name="Right">PUT or CALL</param>
/// <param name="Strike">Strike price</param>
/// <param name="Expiry">Expiration date (YYYY-MM-DD format)</param>
public record Leg(
    string Side,
    string Right,
    decimal Strike,
    string Expiry
);
