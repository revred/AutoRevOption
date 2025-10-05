// TVCEnums.cs — Enumerations for TVC

namespace AutoRevOption.Shared.Tvc.Common;

/// <summary>
/// TVC execution mode
/// </summary>
public enum ExecutionMode
{
    /// <summary>Dry run - persist intent only, no broker calls</summary>
    DRY_RUN,

    /// <summary>Create execution ticket, await approval</summary>
    STAGE,

    /// <summary>Place orders with OCO brackets</summary>
    EXECUTE,

    /// <summary>Queued - waiting for funds/capacity</summary>
    QUEUE,

    /// <summary>Rejected due to admissibility failure</summary>
    REJECT
}

/// <summary>
/// Option strategy type (TVC naming)</summary>
public enum TVCStrategy
{
    /// <summary>Put Credit Spread</summary>
    PUT_CREDIT_SPREAD,

    /// <summary>Call Credit Spread</summary>
    CALL_CREDIT_SPREAD,

    /// <summary>Bull Call Spread</summary>
    BULL_CALL_SPREAD,

    /// <summary>Bear Put Spread</summary>
    BEAR_PUT_SPREAD,

    /// <summary>Iron Condor</summary>
    IRON_CONDOR,

    /// <summary>Butterfly</summary>
    BUTTERFLY,

    /// <summary>Broken Wing Butterfly</summary>
    BROKEN_WING_BUTTERFLY
}
