// SelectionResult.cs — Selection outcome with reasons and score

namespace AutoRevOption.Shared.Tvc.SelectTVC;

/// <summary>
/// Selection result with pass/fail status, reasons, and optional score
/// </summary>
/// <param name="Pass">True if candidate passed all selection gates</param>
/// <param name="Reasons">List of reasons (gates passed or failed)</param>
/// <param name="Score">Optional normalized score [0,1] combining sub-scores</param>
public record SelectionResult(
    bool Pass,
    string[] Reasons,
    decimal Score
);
