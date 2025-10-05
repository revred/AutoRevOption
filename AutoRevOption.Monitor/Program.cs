// AutoRevOption.Monitor — Read-only IBKR connection monitor
// Establishes connection and displays account/position data

using AutoRevOption.Monitor;
using AutoRevOption.Shared.Portal;
using AutoRevOption.Client;

// Check if running in MCP mode
if (args.Length > 0 && args[0] == "--mcp")
{
    await ProgramMcp.MainMcp(args);
    return;
}

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("AutoRevOption.Monitor — IBKR Read-Only Connection\n");

// Use ClientPortalLoginService (handles secrets internally)
Console.WriteLine("Logging in to Client Portal...");
var loginService = new ClientPortalLoginService();

Connection? ibkr = null;
var cts = new CancellationTokenSource();

try
{
    var client = await loginService.LoginAsync(headless: true, twoFactorTimeoutMinutes: 2);
    ibkr = new Connection(client);
    Console.WriteLine("\n=== IBKR Connection Established ===\n");

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
    ibkr?.Disconnect();
    ibkr?.Dispose();
}

static async Task ShowAccountSummary(Connection ibkr)
{
    Console.WriteLine("\n--- Account Summary ---");
    var account = await ibkr.GetAccountInfoAsync();

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

static async Task ShowPositions(Connection ibkr)
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

static async Task MonitorLoop(Connection ibkr)
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
