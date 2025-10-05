// ExecutionCard.cs — Execution Card (per WriteTVC.md spec)

using AutoRevOption.Shared.Tvc.Common;

namespace AutoRevOption.Shared.Tvc.WriteTVC;

/// <summary>
/// Execution Card - Immutable record of write/act decision
/// Produced by WriteTVC layer after admissibility checks
/// </summary>
/// <param name="EcVersion">Execution Card schema version</param>
/// <param name="TimestampUtc">UTC timestamp of execution decision</param>
/// <param name="Mode">Execution mode (DRY_RUN, STAGE, EXECUTE, QUEUE, REJECT)</param>
/// <param name="TvcRef">Reference to source TVC selection artifact</param>
/// <param name="Symbol">Underlying ticker symbol</param>
/// <param name="Strategy">Strategy type (PUT_CREDIT_SPREAD, etc.)</param>
/// <param name="Legs">Option legs</param>
/// <param name="IntendedCreditLimit">Intended limit price for credit</param>
/// <param name="Brackets">Exit bracket configuration (TP/SL)</param>
/// <param name="Admissibility">Admissibility check results</param>
/// <param name="BrokerPreview">Optional broker preview data (fees, margin, etc.)</param>
/// <param name="ActionResult">Result of action taken (order ID, status, notes)</param>
public record ExecutionCard(
    string EcVersion,
    DateTime TimestampUtc,
    string Mode,
    string TvcRef,
    string Symbol,
    string Strategy,
    Leg[] Legs,
    decimal IntendedCreditLimit,
    Brackets Brackets,
    Admissibility Admissibility,
    BrokerPreview? BrokerPreview,
    ActionResult ActionResult
);
