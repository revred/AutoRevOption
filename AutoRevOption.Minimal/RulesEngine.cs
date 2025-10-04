// RulesEngine.cs — Parse OptionsRadar.yaml and enforce strategy policies (WP01)

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoRevOption;

// YAML configuration structure
public class OptionsRadarConfig
{
    public RiskConfig Risk { get; set; } = new();
    public IncomeConfig Income { get; set; } = new();
    public ConvexConfig Convex { get; set; } = new();
    public UniverseConfig Universe { get; set; } = new();
    public AlertsConfig Alerts { get; set; } = new();
    public ExchangesConfig Exchanges { get; set; } = new();
}

public class RiskConfig
{
    public decimal MaxDebit { get; set; }
    public int MaxSpreadsOpen { get; set; }
    public decimal MaintPctMax { get; set; }
    public decimal DeltaMax { get; set; }
    public decimal ThetaMin { get; set; }
}

public class IncomeConfig
{
    public List<int> Dte { get; set; } = new();
    public List<decimal> ShortDelta { get; set; } = new();
    public decimal MinCreditWidth { get; set; }
}

public class ConvexConfig
{
    public List<decimal> LowerDelta { get; set; } = new();
    public decimal TargetRR { get; set; }
}

public class UniverseConfig
{
    public List<string> Core { get; set; } = new();
    public List<string> Miners { get; set; } = new();
}

public class AlertsConfig
{
    public ThresholdsConfig Thresholds { get; set; } = new();
    public NotifyConfig Notify { get; set; } = new();
}

public class ThresholdsConfig
{
    public int ShipScore { get; set; }
    public int ReviewScore { get; set; }
}

public class NotifyConfig
{
    public decimal AtRiskDelta { get; set; }
    public List<decimal> TpCreditPct { get; set; } = new();
}

public class ExchangesConfig
{
    public string Route { get; set; } = "SMART";
    public string TifEntry { get; set; } = "DAY";
    public string TifExit { get; set; } = "GTC";
}

// Validation results
public record RuleViolation(string Rule, string Message, string Severity);
public record RuleValidationResult(bool IsValid, List<RuleViolation> Violations);

/// <summary>
/// Rules Engine - validates candidates against OptionsRadar.yaml policies
/// </summary>
public class RulesEngine
{
    private readonly OptionsRadarConfig _config;

    public RulesEngine(OptionsRadarConfig config)
    {
        _config = config;
    }

    public static RulesEngine FromYamlFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<OptionsRadarConfig>(yaml);
        return new RulesEngine(config);
    }

    public OptionsRadarConfig GetConfig() => _config;

    /// <summary>
    /// Validate an income strategy (PCS/CCS)
    /// </summary>
    public RuleValidationResult ValidateIncome(Candidate candidate)
    {
        var violations = new List<RuleViolation>();

        // Check strategy type
        if (candidate.Type is not StrategyType.PCS and not StrategyType.CCS)
        {
            violations.Add(new RuleViolation("StrategyType", "Not an income strategy (PCS/CCS)", "Error"));
            return new RuleValidationResult(false, violations);
        }

        // Check credit exists
        if (candidate.Credit is null || candidate.Credit <= 0)
        {
            violations.Add(new RuleViolation("Credit", "Credit must be > 0 for income trades", "Error"));
        }

        // Check DTE range
        var dte = CalculateDte(candidate);
        if (dte < _config.Income.Dte[0] || dte > _config.Income.Dte[1])
        {
            violations.Add(new RuleViolation("DTE",
                $"DTE {dte} outside range [{_config.Income.Dte[0]}, {_config.Income.Dte[1]}]", "Warning"));
        }

        // Check short delta
        if (candidate.ShortDelta.HasValue)
        {
            var delta = candidate.ShortDelta.Value;
            if (delta < _config.Income.ShortDelta[0] || delta > _config.Income.ShortDelta[1])
            {
                violations.Add(new RuleViolation("ShortDelta",
                    $"Short delta {delta:F2} outside range [{_config.Income.ShortDelta[0]}, {_config.Income.ShortDelta[1]}]", "Warning"));
            }
        }

        // Check credit/width ratio
        if (candidate.Credit.HasValue && candidate.Width > 0)
        {
            var ratio = candidate.Credit.Value / candidate.Width;
            if (ratio < _config.Income.MinCreditWidth)
            {
                violations.Add(new RuleViolation("CreditWidth",
                    $"Credit/Width ratio {ratio:F2} below minimum {_config.Income.MinCreditWidth:F2}", "Warning"));
            }
        }

        return new RuleValidationResult(violations.All(v => v.Severity != "Error"), violations);
    }

    /// <summary>
    /// Validate a convex strategy (BPS/BCS)
    /// </summary>
    public RuleValidationResult ValidateConvex(Candidate candidate)
    {
        var violations = new List<RuleViolation>();

        // Check strategy type
        if (candidate.Type is not StrategyType.BPS and not StrategyType.BCS)
        {
            violations.Add(new RuleViolation("StrategyType", "Not a convex strategy (BPS/BCS)", "Error"));
            return new RuleValidationResult(false, violations);
        }

        // Check debit exists
        if (candidate.Debit is null || candidate.Debit <= 0)
        {
            violations.Add(new RuleViolation("Debit", "Debit must be > 0 for convex trades", "Error"));
        }

        // Check lower delta (long leg)
        if (candidate.ShortDelta.HasValue)
        {
            var delta = candidate.ShortDelta.Value;
            if (delta < _config.Convex.LowerDelta[0] || delta > _config.Convex.LowerDelta[1])
            {
                violations.Add(new RuleViolation("LowerDelta",
                    $"Delta {delta:F2} outside range [{_config.Convex.LowerDelta[0]}, {_config.Convex.LowerDelta[1]}]", "Warning"));
            }
        }

        // Check Risk:Reward ratio
        if (candidate.Debit.HasValue && candidate.Width > 0)
        {
            var maxProfit = candidate.Width - candidate.Debit.Value;
            var rr = maxProfit / candidate.Debit.Value;
            if (rr < _config.Convex.TargetRR)
            {
                violations.Add(new RuleViolation("RiskReward",
                    $"R:R ratio {rr:F2} below target {_config.Convex.TargetRR:F2}", "Warning"));
            }
        }

        return new RuleValidationResult(violations.All(v => v.Severity != "Error"), violations);
    }

    /// <summary>
    /// Validate risk gates (applies to all strategies)
    /// </summary>
    public RuleValidationResult ValidateRiskGates(Candidate candidate, AccountSnapshot account, int currentOpenSpreads)
    {
        var violations = new List<RuleViolation>();

        // Max debit check
        if (candidate.Debit.HasValue && candidate.Debit.Value > _config.Risk.MaxDebit)
        {
            violations.Add(new RuleViolation("MaxDebit",
                $"Debit ${candidate.Debit.Value} exceeds max ${_config.Risk.MaxDebit}", "Error"));
        }

        // Max spreads open
        if (currentOpenSpreads >= _config.Risk.MaxSpreadsOpen)
        {
            violations.Add(new RuleViolation("MaxSpreads",
                $"Already at max spreads ({currentOpenSpreads}/{_config.Risk.MaxSpreadsOpen})", "Error"));
        }

        // Maintenance margin %
        if (account.MaintPct > _config.Risk.MaintPctMax)
        {
            violations.Add(new RuleViolation("MaintMargin",
                $"Maintenance margin {account.MaintPct:P1} exceeds max {_config.Risk.MaintPctMax:P1}", "Error"));
        }

        // Account delta
        if (Math.Abs(account.AccountDelta) > _config.Risk.DeltaMax)
        {
            violations.Add(new RuleViolation("AccountDelta",
                $"Account delta {account.AccountDelta} exceeds max ±{_config.Risk.DeltaMax}", "Warning"));
        }

        // Account theta
        if (account.AccountTheta < _config.Risk.ThetaMin)
        {
            violations.Add(new RuleViolation("AccountTheta",
                $"Account theta {account.AccountTheta} below minimum {_config.Risk.ThetaMin}", "Warning"));
        }

        return new RuleValidationResult(violations.All(v => v.Severity != "Error"), violations);
    }

    /// <summary>
    /// Check if candidate meets actionable score threshold
    /// </summary>
    public bool IsShippable(int score) => score >= _config.Alerts.Thresholds.ShipScore;
    public bool NeedsReview(int score) => score >= _config.Alerts.Thresholds.ReviewScore && score < _config.Alerts.Thresholds.ShipScore;

    /// <summary>
    /// Get full universe (core + miners)
    /// </summary>
    public string[] GetUniverse()
    {
        return _config.Universe.Core.Concat(_config.Universe.Miners).ToArray();
    }

    /// <summary>
    /// Get take-profit % range
    /// </summary>
    public (decimal Min, decimal Max) GetTakeProfitRange()
    {
        return (_config.Alerts.Notify.TpCreditPct[0], _config.Alerts.Notify.TpCreditPct[1]);
    }

    /// <summary>
    /// Get exchange routing config
    /// </summary>
    public (string Route, string TifEntry, string TifExit) GetExchangeConfig()
    {
        return (_config.Exchanges.Route, _config.Exchanges.TifEntry, _config.Exchanges.TifExit);
    }

    private int CalculateDte(Candidate candidate)
    {
        if (!candidate.Legs.Any()) return 0;
        var expiry = candidate.Legs.First().Exp;
        return expiry.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
    }
}
