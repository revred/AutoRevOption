// Admissibility.cs — Admissibility check results

namespace AutoRevOption.Shared.Tvc.WriteTVC;

/// <summary>
/// Account-level admissibility check results
/// All gates must pass for EXECUTE mode
/// </summary>
/// <param name="MaintPctOk">Maintenance percentage within limits</param>
/// <param name="DefinedRiskOk">Total defined risk within portfolio cap</param>
/// <param name="SymbolExposureOk">Per-symbol exposure within cap</param>
/// <param name="FreshEnough">TVC age within max_age_minutes</param>
/// <param name="CreditDriftOk">Credit quote drift within tolerance</param>
/// <param name="Reasons">List of reasons for failures (empty if all pass)</param>
public record Admissibility(
    bool MaintPctOk,
    bool DefinedRiskOk,
    bool SymbolExposureOk,
    bool FreshEnough,
    bool CreditDriftOk,
    string[] Reasons
);
