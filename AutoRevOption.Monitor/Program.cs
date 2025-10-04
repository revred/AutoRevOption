// AutoRevOption.Monitor — Read-only IBKR connection monitor
// Establishes connection and displays account/position data

using AutoRevOption.Monitor;

// Check if running in MCP mode
if (args.Length > 0 && args[0] == "--mcp")
{
    await ProgramMcp.MainMcp(args);
    return;
}

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("AutoRevOption.Monitor — IBKR Read-Only Connection\n");

// Load secrets
var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "secrets.json");
if (!File.Exists(secretsPath))
{
    Console.WriteLine($"❌ secrets.json not found at {secretsPath}");
    Console.WriteLine("\nPlease create secrets.json with:");
    Console.WriteLine(@"{
  ""IBKRCredentials"": {
    ""Host"": ""127.0.0.1"",
    ""Port"": 7497,
    ""ClientId"": 1
  }
}");
    return;
}

SecretConfig config;
try
{
    config = SecretConfig.LoadFromFile(secretsPath);
    Console.WriteLine($"✅ Loaded secrets from {secretsPath}");
    Console.WriteLine($"   Host: {config.IBKRCredentials.Host}:{config.IBKRCredentials.Port}");
    Console.WriteLine($"   ClientId: {config.IBKRCredentials.ClientId}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to load secrets: {ex.Message}");
    return;
}

// Check and launch Gateway if needed
var gatewayManager = new GatewayManager(config.IBKRCredentials);

Console.WriteLine($"\n{gatewayManager.GetStatus()}\n");

var gatewayReady = await gatewayManager.EnsureGatewayRunningAsync();
if (!gatewayReady)
{
    Console.WriteLine("\n❌ IB Gateway is not running");
    Console.WriteLine("\nOptions:");
    Console.WriteLine("1. Start IB Gateway manually and log in");
    Console.WriteLine($"2. Set AutoLaunch: true in secrets.json and configure GatewayPath");
    Console.WriteLine("3. Enable API: Configure → Settings → API → Settings");
    Console.WriteLine($"4. Verify port {config.IBKRCredentials.Port} (7497=paper, 7496=live)");
    Console.WriteLine("\nPress Enter to retry or Ctrl+C to exit...");
    Console.ReadLine();

    gatewayReady = await gatewayManager.EnsureGatewayRunningAsync();
    if (!gatewayReady)
    {
        Console.WriteLine("\n❌ Still cannot connect to IB Gateway. Exiting.");
        return;
    }
}

// Create IBKR connection
var ibkr = new IbkrConnection(config.IBKRCredentials);

// Start Gateway monitoring in background
var cts = new CancellationTokenSource();
var monitorTask = Task.Run(() => gatewayManager.MonitorGatewayAsync(cts.Token));

try
{
    // Connect
    var connected = await ibkr.ConnectAsync();
    if (!connected)
    {
        Console.WriteLine("\n❌ Failed to connect to IBKR API");
        Console.WriteLine($"\nGateway Status: {gatewayManager.GetStatus()}");
        Console.WriteLine("\nTroubleshooting:");
        Console.WriteLine("1. Ensure you've logged into IB Gateway");
        Console.WriteLine("2. Check API settings: Configure → Settings → API → Settings");
        Console.WriteLine("3. Enable 'Enable ActiveX and Socket Clients'");
        Console.WriteLine($"4. Verify port {config.IBKRCredentials.Port} matches (7497=paper, 7496=live)");
        Console.WriteLine("5. Add trusted IP: 127.0.0.1");
        cts.Cancel();
        return;
    }

    Console.WriteLine("\n=== IBKR Connection Established ===\n");
    Console.WriteLine($"Gateway Status: {gatewayManager.GetStatus()}\n");

    // Interactive menu
    while (true)
    {
        Console.WriteLine("\nOptions:");
        Console.WriteLine("1. Get Account Summary");
        Console.WriteLine("2. Get Positions");
        Console.WriteLine("3. Monitor Loop (account + positions every 30s)");
        Console.WriteLine("q. Quit\n");

        Console.Write("Select> ");
        var input = Console.ReadLine()?.Trim().ToLower();

        switch (input)
        {
            case "1":
                await ShowAccountSummary(ibkr);
                break;

            case "2":
                await ShowPositions(ibkr);
                break;

            case "3":
                await MonitorLoop(ibkr);
                break;

            case "q":
            case "quit":
            case "exit":
                Console.WriteLine("\nDisconnecting...");
                ibkr.Disconnect();
                return;

            default:
                Console.WriteLine("Invalid option");
                break;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
    ibkr.Disconnect();
    gatewayManager.Dispose();

    try
    {
        await monitorTask;
    }
    catch (TaskCanceledException)
    {
        // Expected
    }
}

static async Task ShowAccountSummary(IbkrConnection ibkr)
{
    Console.WriteLine("\n--- Account Summary ---");
    var account = await ibkr.GetAccountInfoAsync("All");

    if (account == null)
    {
        Console.WriteLine("Failed to retrieve account data");
        return;
    }

    Console.WriteLine($"\nAccount: {account.AccountId}");
    Console.WriteLine($"Net Liquidation: ${account.NetLiquidation:N2}");
    Console.WriteLine($"Cash:            ${account.Cash:N2}");
    Console.WriteLine($"Buying Power:    ${account.BuyingPower:N2}");
    Console.WriteLine($"Maint Margin:    ${account.MaintenanceMargin:N2}");
    Console.WriteLine($"Maint %:         {account.MaintenancePct:P2}");

    if (account.AllValues.Any())
    {
        Console.WriteLine("\nAll Account Values:");
        foreach (var kvp in account.AllValues.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }
}

static async Task ShowPositions(IbkrConnection ibkr)
{
    Console.WriteLine("\n--- Positions ---");
    var positions = await ibkr.GetPositionsAsync();

    if (!positions.Any())
    {
        Console.WriteLine("No open positions");
        return;
    }

    Console.WriteLine($"\nFound {positions.Count} position(s):\n");
    Console.WriteLine("{0,-10} {1,-8} {2,8} {3,10} {4,8} {5,10} {6,12}",
        "Symbol", "Type", "Right", "Strike", "Expiry", "Qty", "Avg Cost");
    Console.WriteLine(new string('-', 80));

    foreach (var pos in positions.OrderBy(p => p.Symbol))
    {
        if (pos.SecType == "OPT")
        {
            Console.WriteLine("{0,-10} {1,-8} {2,8} {3,10:F2} {4,8} {5,10:F0} {6,12:F2}",
                pos.Symbol, pos.SecType, pos.Right, pos.Strike, pos.Expiry,
                pos.Position, pos.AvgCost);
        }
        else
        {
            Console.WriteLine("{0,-10} {1,-8} {2,8} {3,10} {4,8} {5,10:F0} {6,12:F2}",
                pos.Symbol, pos.SecType, "-", "-", "-", pos.Position, pos.AvgCost);
        }
    }
}

static async Task MonitorLoop(IbkrConnection ibkr)
{
    Console.WriteLine("\n--- Monitor Loop (Ctrl+C to stop) ---");
    Console.WriteLine("Refreshing every 30 seconds...\n");

    var iteration = 0;
    while (true)
    {
        try
        {
            iteration++;
            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Refresh #{iteration}");

            await ShowAccountSummary(ibkr);
            await ShowPositions(ibkr);

            Console.WriteLine($"\nNext refresh in 30s... (press Ctrl+C to stop)");
            await Task.Delay(30000);
        }
        catch (TaskCanceledException)
        {
            break;
        }
    }
}
