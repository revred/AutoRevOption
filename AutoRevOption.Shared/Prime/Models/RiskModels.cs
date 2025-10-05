// RiskModels.cs — Risk management models (Prime execution model)

namespace AutoRevOption.Shared.Prime.Models;

/// <summary>
/// Risk guard parameters
/// </summary>
public record RiskGuards(
    decimal MaxDebit,
    decimal MaxML,
    int MaxOpenSpreads,
    decimal MaintPctMax,
    decimal DeltaMax,
    decimal ThetaMin
);

/// <summary>
/// Risk check request
/// </summary>
public record RiskCheckRequest(
    string AccountId,
    string CandidateId,
    RiskGuards Guards
);

/// <summary>
/// Validation response
/// </summary>
public record ValidateResponse(
    bool Ok,
    List<string> Issues
);

/// <summary>
/// Verification response
/// </summary>
public record VerifyResponse(
    bool Ok,
    int Score,
    string Reason,
    string Slippage
);

/// <summary>
/// Account snapshot
/// </summary>
public record AccountSnapshot(
    decimal NetLiq,
    decimal MaintPct,
    decimal AccountDelta,
    decimal AccountTheta
);
