// AutoRevOption — MCP Interface for OptionsRadar
// .NET 8 single-file console. Drop-in starter.
// Matches hooks: scan → validate → verify → act

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoRevOption;

#pragma warning disable CS8618

public enum StrategyType { PCS, CCS, BCS, BPS, DIAGONAL, PMCC, RV }

public record OptionLeg(string Action, string Right, decimal Strike, DateOnly Exp, int Quantity = 1);

public record Candidate(
    string Id, string Ticker, StrategyType Type, List<OptionLeg> Legs,
    decimal Width, decimal? Credit, decimal? Debit, decimal? ShortDelta,
    int IvRank, int Score, string Playbook, List<string> Notes
);

public record OrderPlan(string CandidateId, string OrderPlanId, Combo Combination, Exits Exits);
public record Combo(string Route, string TimeInForce, List<OptionLeg> Legs);
public record Exits(TakeProfit Tp, StopLoss Sl);
public record TakeProfit(string Type, string Side, string CreditPct, string Tif, string OcaGroup);
public record StopLoss(string Type, string Side, string Trigger, string Tif, string OcaGroup);

public record RiskGuards(decimal MaxDebit, decimal MaxML, int MaxOpenSpreads, decimal MaintPctMax, decimal DeltaMax, decimal ThetaMin);
public record RiskCheckRequest(string AccountId, string CandidateId, RiskGuards Guards);
public record ValidateResponse(bool Ok, List<string> Issues);
public record VerifyResponse(bool Ok, int Score, string Reason, string Slippage);
public record AccountSnapshot(decimal NetLiq, decimal MaintPct, decimal AccountDelta, decimal AccountTheta);

// ---------- MCP Hook Interface ----------
public interface IAutoRevOption
{
    Task<List<Candidate>> ScanAsync(string[] universe, CancellationToken ct = default);
    Task<ValidateResponse> ValidateAsync(Candidate candidate, CancellationToken ct = default);
    Task<VerifyResponse> VerifyAsync(string accountId, Candidate candidate, CancellationToken ct = default);
    Task<(bool ok, string message)> ActAsync(OrderPlan orderPlan, string confirmationCode, bool paper = true, CancellationToken ct = default);
    Task<OrderPlan> BuildOrderPlanAsync(Candidate c, int quantity = 1, CancellationToken ct = default);
    Task<AccountSnapshot> GetAccountAsync(string accountId, CancellationToken ct = default);
}

// ---------- Mock Implementation (replace with IBKR/ThetaData agents) ----------
public sealed class MockAutoRevOption : IAutoRevOption
{
    private readonly ConcurrentDictionary<string, Candidate> _lastScan = new();
    private readonly Random _rng = new();

    public Task<List<Candidate>> ScanAsync(string[] universe, CancellationToken ct = default)
    {
        var list = new List<Candidate>();
        foreach (var t in universe)
        {
            if (_rng.NextDouble() < 0.35)
            {
                var exp = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
                var width = 1.00m;
                var credit = Math.Round(0.32m + (decimal)_rng.NextDouble() * 0.20m, 2);
                var score = 60 + _rng.Next(35);
                var id = $"PCS:{t}:{exp:yyyy-MM-dd}:22-21:{Guid.NewGuid().ToString()[..4]}";
                var c = new Candidate(
                    Id: id, Ticker: t, Type: StrategyType.PCS,
                    Legs: new() { new OptionLeg("SELL","PUT",22, exp), new OptionLeg("BUY","PUT",21, exp) },
                    Width: width, Credit: credit, Debit: null, ShortDelta: 0.22m,
                    IvRank: _rng.Next(20, 80), Score: score,
                    Playbook: "Income-PCS/Weekly/TP50-60/Stop2x",
                    Notes: new(){ "Meets ≥30% width rule" }
                );
                _lastScan[c.Id] = c;
                list.Add(c);
            }
        }
        return Task.FromResult(list.OrderByDescending(x => x.Score).ToList());
    }

    public Task<ValidateResponse> ValidateAsync(Candidate candidate, CancellationToken ct = default)
    {
        var issues = new List<string>();
        if (candidate.Legs.Count < 2) issues.Add("Must have at least 2 legs");
        if (candidate.Type is StrategyType.PCS or StrategyType.CCS)
        {
            if (candidate.Credit is null || candidate.Credit <= 0) issues.Add("Credit missing or ≤ 0");
            if (candidate.Width <= 0) issues.Add("Width must be > 0");
        }
        return Task.FromResult(new ValidateResponse(issues.Count == 0, issues));
    }

    public Task<VerifyResponse> VerifyAsync(string accountId, Candidate candidate, CancellationToken ct = default)
    {
        var ok = candidate.Score >= 60;
        var slip = "$4 est";
        return Task.FromResult(new VerifyResponse(ok, candidate.Score, ok ? "Meets demo guards" : "Below threshold", slip));
    }

    public Task<OrderPlan> BuildOrderPlanAsync(Candidate c, int quantity = 1, CancellationToken ct = default)
    {
        var group = $"OP-{Guid.NewGuid().ToString()[..4]}";
        var combo = new Combo("SMART","DAY", c.Legs.Select(l => l with { Quantity = quantity }).ToList());
        var exits = new Exits(
            Tp: new("LIMIT","BUY_TO_CLOSE","50-60","GTC", group),
            Sl: new("STOP","BUY_TO_CLOSE","2x_credit OR short_strike_touched","GTC", group)
        );
        return Task.FromResult(new OrderPlan(c.Id, group, combo, exits));
    }

    public Task<(bool ok, string message)> ActAsync(OrderPlan orderPlan, string confirmationCode, bool paper = true, CancellationToken ct = default)
    {
        if (!Regex.IsMatch(confirmationCode, "^CONFIRM-OP-"))
            return Task.FromResult((false, "Bad confirmation code"));
        var mode = paper ? "IBKR Demo" : "Live";
        return Task.FromResult((true, $"Submitted {orderPlan.OrderPlanId} to {mode}. TP/SL queued (OCA)."));
    }

    public Task<AccountSnapshot> GetAccountAsync(string accountId, CancellationToken ct = default)
        => Task.FromResult(new AccountSnapshot(NetLiq: 31000m, MaintPct: 0.23m, AccountDelta: 38m, AccountTheta: 2.1m));
}

// ---------- Console App ----------
public static class Program
{
    private static readonly string[] Universe = new[] { "APP","SOFI","META","GOOGL","AMD","AAL","SHOP","MRVL","PLTR","TSLA","MSFT","ZETA" };
    private static readonly IAutoRevOption Radar = new MockAutoRevOption();

    public static async Task Main(string[] args)
    {
        // Check if running in MCP mode
        if (args.Length > 0 && args[0] == "--mcp")
        {
            await ProgramMcp.MainMcp(args);
            return;
        }

        // Regular interactive console mode
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("AutoRevOption — Console (demo)\n");

        // Check IB Gateway status on startup
        GatewayChecker.ShowGatewayStatus("127.0.0.1", 7497);

        while (true)
        {
            Menu();
            Console.Write("Select> ");
            var key = Console.ReadLine();
            Console.WriteLine();
            switch (key)
            {
                case "0": await WeeklyAsync(); break;
                case "1": await ProspectsAsync(); break;
                case "2": await StatusAsync(); break;
                case "3": await NowAsync(); break;
                case "4": await LossLeadsAsync(); break;
                case "5": await ProfitLeadsAsync(); break;
                case "6": await SummaryAsync(); break;
                case "x": case "q": return;
                default: Console.WriteLine("Unknown option. Press Enter..."); Console.ReadLine(); break;
            }
            Console.WriteLine();
        }
    }

    private static void Menu()
    {
        Console.WriteLine("0. Weekly (events/IV map — placeholder) ");
        Console.WriteLine("1. Prospects (ranked Candidates)");
        Console.WriteLine("2. Status (account greeks/margin)");
        Console.WriteLine("3. Now (top 3 OrderPlans with CONFIRM codes)");
        Console.WriteLine("4. Loss Leads (Δ breaches → propose roll) — placeholder");
        Console.WriteLine("5. Profit Leads (at TP/harvest) — placeholder");
        Console.WriteLine("6. Summary (KPIs) — placeholder");
        Console.WriteLine("q. Quit\n");
    }

    private static Task WeeklyAsync()
    {
        Console.WriteLine("[Weekly] — events + IV map (stub)\n");
        return Task.CompletedTask;
    }

    private static async Task ProspectsAsync()
    {
        Console.WriteLine("Scanning universe for Candidates...\n");
        var scan = await Radar.ScanAsync(Universe);
        if (!scan.Any()) { Console.WriteLine("No candidates found.\n"); return; }
        DumpCandidates(scan);
    }

    private static async Task StatusAsync()
    {
        var acct = await Radar.GetAccountAsync("ibkr:primary");
        Console.WriteLine($"NetLiq: £{acct.NetLiq:N0}  Maint%: {acct.MaintPct:P0}  Δ: {acct.AccountDelta}  Θ: {acct.AccountTheta}\n");
        Console.WriteLine("Open positions / working orders: (integrate IBKR feed)\n");
    }

    private static async Task NowAsync()
    {
        Console.WriteLine("=== WP01 Demo: Top 3 Candidates with OrderPlan JSON ===\n");

        // Initialize components
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "OptionsRadar.yaml");
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"⚠️  OptionsRadar.yaml not found at {configPath}");
            Console.WriteLine("Using hardcoded defaults...\n");
        }

        RulesEngine? rulesEngine = null;
        try
        {
            if (File.Exists(configPath))
            {
                rulesEngine = RulesEngine.FromYamlFile(configPath);
                Console.WriteLine("✅ Loaded OptionsRadar.yaml config\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Failed to load YAML: {ex.Message}");
        }

        var exitPolicy = new ExitPolicy(TpMinPct: 0.50m, TpMaxPct: 0.60m, SlMultiplier: 2.0m);
        var orderBuilder = new OrderBuilder(exitPolicy, "SMART", "DAY", "GTC");

        // Scan for candidates
        var scan = await Radar.ScanAsync(Universe);
        var top = scan.OrderByDescending(x => x.Score).Take(3).ToList();
        if (!top.Any()) { Console.WriteLine("No actionable candidates.\n"); return; }

        var plans = new List<OrderPlan>();
        for (int i = 0; i < top.Count; i++)
        {
            var c = top[i];

            // Validate with mock
            var val = await Radar.ValidateAsync(c);
            var ver = await Radar.VerifyAsync("ibkr:primary", c);

            // Validate with RulesEngine if available
            if (rulesEngine != null)
            {
                var incomeValidation = rulesEngine.ValidateIncome(c);
                var account = await Radar.GetAccountAsync("ibkr:primary");
                var riskValidation = rulesEngine.ValidateRiskGates(c, account, currentOpenSpreads: 2);

                Console.WriteLine($"[{i}] {c.Ticker} {c.Type} score {c.Score}");
                Console.WriteLine($"    RulesEngine: Income={incomeValidation.IsValid} Risk={riskValidation.IsValid}");
                if (incomeValidation.Violations.Any())
                    Console.WriteLine($"    Issues: {string.Join(", ", incomeValidation.Violations.Select(v => v.Message))}");
            }
            else
            {
                Console.WriteLine($"[{i}] {c.Ticker} {c.Type} score {c.Score}");
                Console.WriteLine($"    Validate: {(val.Ok ? "OK" : string.Join(",", val.Issues))} | Verify: {(ver.Ok ? "OK" : ver.Reason)}");
            }

            // Build OrderPlan
            var plan = orderBuilder.BuildOrderPlan(c, quantity: 2);
            plans.Add(plan);

            // Export as JSON
            var json = orderBuilder.ToJson(plan);
            Console.WriteLine($"\nOrderPlan JSON for {c.Ticker}:");
            Console.WriteLine(json);
            Console.WriteLine($"    CONFIRM code: CONFIRM-{plan.OrderPlanId}\n");
        }

        Console.Write("Enter index to ACT (or blank to skip): ");
        var sel = Console.ReadLine();
        if (int.TryParse(sel, out var idx) && idx >= 0 && idx < plans.Count)
        {
            var plan = plans[idx];
            var code = $"CONFIRM-{plan.OrderPlanId}";
            var (ok, msg) = await Radar.ActAsync(plan, code, paper: true);
            Console.WriteLine(ok ? $"✅ {msg}" : $"❌ {msg}");
        }
    }

    private static Task LossLeadsAsync()
    {
        Console.WriteLine("[Loss Leads] — roll proposals (stub)\n");
        return Task.CompletedTask;
    }

    private static Task ProfitLeadsAsync()
    {
        Console.WriteLine("[Profit Leads] — harvest/roll (stub)\n");
        return Task.CompletedTask;
    }

    private static Task SummaryAsync()
    {
        Console.WriteLine("[Summary] — KPIs (stub)\n");
        return Task.CompletedTask;
    }

    private static void DumpCandidates(List<Candidate> scan)
    {
        Console.WriteLine("#  Ticker  Type  Expiry     Width  Credit  Δshort  IVR  Score  Notes");
        var i = 0;
        foreach (var c in scan)
        {
            var exp = c.Legs.First().Exp.ToString("yyyy-MM-dd");
            Console.WriteLine($"{i,0}. {c.Ticker,-6} {c.Type,-4} {exp,-10} {c.Width,5:N2}  {c.Credit,6}  {c.ShortDelta,5}  {c.IvRank,3}  {c.Score,5}  {string.Join("; ", c.Notes)}");
            i++;
        }
        Console.WriteLine();
    }
}