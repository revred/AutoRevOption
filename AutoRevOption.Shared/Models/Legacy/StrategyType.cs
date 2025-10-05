// StrategyType.cs — Option strategy types (Legacy)

namespace AutoRevOption.Shared.Models.Legacy;

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
