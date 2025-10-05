// StrategyType.cs — Option strategy types (Prime execution model)

namespace AutoRevOption.Shared.Prime.Models;

/// <summary>
/// Supported option strategy types
/// </summary>
public enum StrategyType
{
    /// <summary>Put Credit Spread</summary>
    PCS,

    /// <summary>Call Credit Spread</summary>
    CCS,

    /// <summary>Bull Call Spread</summary>
    BCS,

    /// <summary>Bear Put Spread</summary>
    BPS,

    /// <summary>Diagonal Spread</summary>
    DIAGONAL,

    /// <summary>Poor Man's Covered Call</summary>
    PMCC,

    /// <summary>Reverse (unspecified)</summary>
    RV
}
