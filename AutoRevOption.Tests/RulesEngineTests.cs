// RulesEngineTests.cs — Unit tests for RulesEngine (WP01)
//
// ✅ SAFETY: This test file is 100% SAFE
// - NO IBKR connection
// - NO order placement
// - NO account modifications
// - Pure logic testing only
//
// Safe to run anytime with: dotnet test

using AutoRevOption;
using AutoRevOption.Shared.Prime.Models;
using AutoRevOption.Shared.Rules;
using Xunit;

namespace AutoRevOption.Tests;

public class RulesEngineTests
{
    private readonly RulesEngine _rulesEngine;
    private readonly OptionsRadarConfig _config;

    public RulesEngineTests()
    {
        // Setup test config matching OptionsRadar.yaml
        _config = new OptionsRadarConfig
        {
            Risk = new RiskConfig
            {
                MaxDebit = 800,
                MaxSpreadsOpen = 5,
                MaintPctMax = 0.35m,
                DeltaMax = 50,
                ThetaMin = 0
            },
            Income = new IncomeConfig
            {
                Dte = new List<int> { 5, 9 },
                ShortDelta = new List<decimal> { 0.20m, 0.25m },
                MinCreditWidth = 0.30m
            },
            Convex = new ConvexConfig
            {
                LowerDelta = new List<decimal> { 0.55m, 0.65m },
                TargetRR = 2.0m
            },
            Universe = new UniverseConfig
            {
                Core = new List<string> { "APP", "SOFI", "META", "GOOGL", "AMD" },
                Miners = new List<string> { "GDX", "GDXJ", "CCJ" }
            },
            Alerts = new AlertsConfig
            {
                Thresholds = new ThresholdsConfig
                {
                    ShipScore = 75,
                    ReviewScore = 60
                },
                Notify = new NotifyConfig
                {
                    AtRiskDelta = 0.35m,
                    TpCreditPct = new List<decimal> { 0.50m, 0.60m }
                }
            },
            Exchanges = new ExchangesConfig
            {
                Route = "SMART",
                TifEntry = "DAY",
                TifExit = "GTC"
            }
        };

        _rulesEngine = new RulesEngine(_config);
    }

    [Fact]
    public void ValidateIncome_ValidPCS_ReturnsTrue()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "PCS:SOFI:2025-10-11:22-21",
            Ticker: "SOFI",
            Type: StrategyType.PCS,
            Legs: new List<OptionLeg>
            {
                new("SELL", "PUT", 22m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))),
                new("BUY", "PUT", 21m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            },
            Width: 1.00m,
            Credit: 0.35m,
            Debit: null,
            ShortDelta: 0.22m,
            IvRank: 65,
            Score: 80,
            Playbook: "Income-PCS/Weekly",
            Notes: new List<string> { "Meets rules" }
        );

        // Act
        var result = _rulesEngine.ValidateIncome(candidate);

        // Assert
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Violations, v => v.Severity == "Error");
    }

    [Fact]
    public void ValidateIncome_CreditTooLow_ReturnsWarning()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "PCS:SOFI:2025-10-11:22-21",
            Ticker: "SOFI",
            Type: StrategyType.PCS,
            Legs: new List<OptionLeg>
            {
                new("SELL", "PUT", 22m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))),
                new("BUY", "PUT", 21m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            },
            Width: 1.00m,
            Credit: 0.20m, // 20% credit/width ratio - below 30% minimum
            Debit: null,
            ShortDelta: 0.22m,
            IvRank: 65,
            Score: 80,
            Playbook: "Income-PCS/Weekly",
            Notes: new List<string>()
        );

        // Act
        var result = _rulesEngine.ValidateIncome(candidate);

        // Assert
        Assert.True(result.IsValid); // Still valid (just warning)
        Assert.Contains(result.Violations, v => v.Rule == "CreditWidth" && v.Severity == "Warning");
    }

    [Fact]
    public void ValidateIncome_MissingCredit_ReturnsError()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "PCS:SOFI:2025-10-11:22-21",
            Ticker: "SOFI",
            Type: StrategyType.PCS,
            Legs: new List<OptionLeg>
            {
                new("SELL", "PUT", 22m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))),
                new("BUY", "PUT", 21m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            },
            Width: 1.00m,
            Credit: null, // Missing!
            Debit: null,
            ShortDelta: 0.22m,
            IvRank: 65,
            Score: 80,
            Playbook: "Income-PCS/Weekly",
            Notes: new List<string>()
        );

        // Act
        var result = _rulesEngine.ValidateIncome(candidate);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Rule == "Credit" && v.Severity == "Error");
    }

    [Fact]
    public void ValidateConvex_ValidBPS_ReturnsTrue()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "BPS:AMD:2025-10-18:140-145",
            Ticker: "AMD",
            Type: StrategyType.BPS,
            Legs: new List<OptionLeg>
            {
                new("BUY", "PUT", 145m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))),
                new("SELL", "PUT", 140m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)))
            },
            Width: 5.00m,
            Credit: null,
            Debit: 1.50m, // R:R = (5 - 1.5) / 1.5 = 2.33
            ShortDelta: 0.60m,
            IvRank: 55,
            Score: 72,
            Playbook: "Convex-BPS",
            Notes: new List<string>()
        );

        // Act
        var result = _rulesEngine.ValidateConvex(candidate);

        // Assert
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Violations, v => v.Severity == "Error");
    }

    [Fact]
    public void ValidateConvex_LowRiskReward_ReturnsWarning()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "BPS:AMD:2025-10-18:140-145",
            Ticker: "AMD",
            Type: StrategyType.BPS,
            Legs: new List<OptionLeg>
            {
                new("BUY", "PUT", 145m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))),
                new("SELL", "PUT", 140m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)))
            },
            Width: 5.00m,
            Credit: null,
            Debit: 3.00m, // R:R = (5 - 3) / 3 = 0.67 - below 2.0 target
            ShortDelta: 0.60m,
            IvRank: 55,
            Score: 72,
            Playbook: "Convex-BPS",
            Notes: new List<string>()
        );

        // Act
        var result = _rulesEngine.ValidateConvex(candidate);

        // Assert
        Assert.True(result.IsValid); // Still valid (just warning)
        Assert.Contains(result.Violations, v => v.Rule == "RiskReward" && v.Severity == "Warning");
    }

    [Fact]
    public void ValidateRiskGates_AllPass_ReturnsTrue()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "BPS:AMD:2025-10-18:140-145",
            Ticker: "AMD",
            Type: StrategyType.BPS,
            Legs: new List<OptionLeg>(),
            Width: 5.00m,
            Credit: null,
            Debit: 500m, // Below 800 max
            ShortDelta: null,
            IvRank: 55,
            Score: 72,
            Playbook: "Convex-BPS",
            Notes: new List<string>()
        );

        var account = new AccountSnapshot(
            NetLiq: 31000m,
            MaintPct: 0.25m, // Below 35% max
            AccountDelta: 30m, // Below 50 max
            AccountTheta: 2.0m // Above 0 min
        );

        // Act
        var result = _rulesEngine.ValidateRiskGates(candidate, account, currentOpenSpreads: 3);

        // Assert
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Violations, v => v.Severity == "Error");
    }

    [Fact]
    public void ValidateRiskGates_ExceedsMaxDebit_ReturnsError()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "BPS:AMD:2025-10-18:140-150",
            Ticker: "AMD",
            Type: StrategyType.BPS,
            Legs: new List<OptionLeg>(),
            Width: 10.00m,
            Credit: null,
            Debit: 900m, // Exceeds 800 max!
            ShortDelta: null,
            IvRank: 55,
            Score: 72,
            Playbook: "Convex-BPS",
            Notes: new List<string>()
        );

        var account = new AccountSnapshot(31000m, 0.25m, 30m, 2.0m);

        // Act
        var result = _rulesEngine.ValidateRiskGates(candidate, account, currentOpenSpreads: 2);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Rule == "MaxDebit" && v.Severity == "Error");
    }

    [Fact]
    public void ValidateRiskGates_MaxSpreadsReached_ReturnsError()
    {
        // Arrange
        var candidate = new Candidate(
            Id: "PCS:SOFI:2025-10-11:22-21",
            Ticker: "SOFI",
            Type: StrategyType.PCS,
            Legs: new List<OptionLeg>(),
            Width: 1.00m,
            Credit: 0.35m,
            Debit: null,
            ShortDelta: 0.22m,
            IvRank: 65,
            Score: 80,
            Playbook: "Income-PCS/Weekly",
            Notes: new List<string>()
        );

        var account = new AccountSnapshot(31000m, 0.25m, 30m, 2.0m);

        // Act
        var result = _rulesEngine.ValidateRiskGates(candidate, account, currentOpenSpreads: 5); // At max!

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Rule == "MaxSpreads" && v.Severity == "Error");
    }

    [Fact]
    public void IsShippable_ScoreAboveThreshold_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(_rulesEngine.IsShippable(75));
        Assert.True(_rulesEngine.IsShippable(80));
        Assert.False(_rulesEngine.IsShippable(74));
    }

    [Fact]
    public void NeedsReview_ScoreInRange_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(_rulesEngine.NeedsReview(60));
        Assert.True(_rulesEngine.NeedsReview(70));
        Assert.False(_rulesEngine.NeedsReview(75)); // Shippable
        Assert.False(_rulesEngine.NeedsReview(55)); // Below threshold
    }

    [Fact]
    public void GetUniverse_ReturnsAllTickers()
    {
        // Act
        var universe = _rulesEngine.GetUniverse();

        // Assert
        Assert.Equal(8, universe.Length); // 5 core + 3 miners
        Assert.Contains("APP", universe);
        Assert.Contains("GDX", universe);
    }

    [Fact]
    public void GetTakeProfitRange_ReturnsCorrectRange()
    {
        // Act
        var (min, max) = _rulesEngine.GetTakeProfitRange();

        // Assert
        Assert.Equal(0.50m, min);
        Assert.Equal(0.60m, max);
    }
}
