// IbkrConnectionDebugTests.cs — Comprehensive IBKR connection debugging with verbose logging

using AutoRevOption.Shared.Configuration;
using AutoRevOption.Shared.Ibkr;
using Xunit;
using Xunit.Abstractions;

namespace AutoRevOption.Tests;

public class IbkrConnectionDebugTests
{
    private readonly ITestOutputHelper _output;

    public IbkrConnectionDebugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DebugIbkrConnectionWithVerboseLogging()
    {
        _output.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
        _output.WriteLine("║                   IBKR CONNECTION DEBUG TEST                               ║");
        _output.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
        _output.WriteLine("");

        // Load secrets
        var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "secrets.json");
        _output.WriteLine($"📂 Loading secrets from: {secretsPath}");

        SecretConfig config;
        try
        {
            config = SecretConfig.LoadFromFile(secretsPath);
            _output.WriteLine("✅ Secrets loaded successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to load secrets: {ex.Message}");
            throw;
        }

        // Display connection parameters
        _output.WriteLine("");
        _output.WriteLine("📋 Connection Parameters:");
        _output.WriteLine($"   Host: {config.IBKRCredentials.Host}");
        _output.WriteLine($"   Port: {config.IBKRCredentials.Port}");
        _output.WriteLine($"   ClientId: {config.IBKRCredentials.ClientId}");
        _output.WriteLine($"   Paper Trading: {config.IBKRCredentials.IsPaperTrading}");
        _output.WriteLine("");

        // Redirect Console.WriteLine to test output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Create connection
            _output.WriteLine("🔌 Creating IbkrConnection instance...");
            var connection = new IbkrConnection(config.IBKRCredentials);
            _output.WriteLine("✅ Instance created");
            _output.WriteLine("");

            // Attempt connection with 30-second wait
            _output.WriteLine("⏳ Attempting connection (30-second timeout)...");
            _output.WriteLine("   Waiting for TWS API callbacks:");
            _output.WriteLine("   - connectAck() - Connection acknowledgment");
            _output.WriteLine("   - nextValidId() - Connection established with order ID");
            _output.WriteLine("   - error(2104) - Market data farm connected");
            _output.WriteLine("   - error(2106) - HMDS data farm connected");
            _output.WriteLine("   - error(2158) - Secure connection established");
            _output.WriteLine("");

            var startTime = DateTime.Now;
            var connected = await connection.ConnectAsync();
            var elapsed = DateTime.Now - startTime;

            // Capture console output
            Console.SetOut(originalOut);
            var consoleOutput = stringWriter.ToString();

            _output.WriteLine("📝 Console Output:");
            _output.WriteLine("─────────────────────────────────────────────────────────────────────────");
            foreach (var line in consoleOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                _output.WriteLine($"   {line}");
            }
            _output.WriteLine("─────────────────────────────────────────────────────────────────────────");
            _output.WriteLine("");

            // Display result
            _output.WriteLine($"⏱️  Connection attempt completed in {elapsed.TotalSeconds:F2} seconds");
            _output.WriteLine("");

            if (connected)
            {
                _output.WriteLine("✅ CONNECTION SUCCESSFUL");
                _output.WriteLine("");
                _output.WriteLine("🎯 Next Steps:");
                _output.WriteLine("   1. Request account summary");
                _output.WriteLine("   2. Test market data subscription");
                _output.WriteLine("   3. Verify option chain retrieval");

                // Connection successful - ready for further testing
                _output.WriteLine("");
                _output.WriteLine("📊 Connection established - ready to test account and market data APIs");
            }
            else
            {
                _output.WriteLine("❌ CONNECTION FAILED");
                _output.WriteLine("");
                _output.WriteLine("🔍 Diagnostic Checklist:");
                _output.WriteLine("");
                _output.WriteLine("   1. Check IB Gateway Status:");
                _output.WriteLine("      - Is IB Gateway running and logged in?");
                _output.WriteLine("      - Is the status showing as connected to IBKR servers?");
                _output.WriteLine("");
                _output.WriteLine("   2. Check IB Gateway API Settings:");
                _output.WriteLine($"      - Socket Port: Should be {config.IBKRCredentials.Port}");
                _output.WriteLine("      - Master API client ID: Try setting to blank/0 (not 10)");
                _output.WriteLine("      - Read-Only API: Should be UNCHECKED");
                _output.WriteLine("");
                _output.WriteLine("   3. Check Network:");
                _output.WriteLine("      - Run: netstat -an | findstr :4001");
                _output.WriteLine("      - Should show: TCP 0.0.0.0:4001 LISTENING");
                _output.WriteLine("");
                _output.WriteLine("   4. Check IB Gateway Logs:");
                _output.WriteLine("      - Location: C:\\Users\\{YourUsername}\\Jts\\");
                _output.WriteLine("      - Files: api.*.log, ibgateway.*.log");
                _output.WriteLine("      - Look for: Connection attempts, API client registrations, errors");
                _output.WriteLine("");
                _output.WriteLine("   5. Restart Sequence:");
                _output.WriteLine("      - Close IB Gateway completely");
                _output.WriteLine("      - Wait 10 seconds");
                _output.WriteLine("      - Restart IB Gateway and log in");
                _output.WriteLine("      - Check API settings again");
                _output.WriteLine("      - Set Master API client ID to blank or 0");
                _output.WriteLine("      - Try connection again");
                _output.WriteLine("");
                _output.WriteLine("   6. Check Console Output Above:");
                _output.WriteLine("      - Did you see '[IBKR] Connection acknowledged'?");
                _output.WriteLine("      - Did you see '[IBKR] Next valid order ID: X'?");
                _output.WriteLine("      - Did you see any error codes?");
                _output.WriteLine("");
                _output.WriteLine("📖 Common Error Codes:");
                _output.WriteLine("   502 - Couldn't connect to TWS (Gateway not running or port wrong)");
                _output.WriteLine("   504 - Not connected (Authentication failed)");
                _output.WriteLine("   2104 - Market data farm connected (GOOD)");
                _output.WriteLine("   2106 - HMDS data farm connected (GOOD)");
                _output.WriteLine("   2158 - Secure connection established (GOOD)");
            }

            // Always disconnect
            if (connection != null)
            {
                connection.Disconnect();
                _output.WriteLine("");
                _output.WriteLine("🔌 Disconnected");
            }
        }
        catch (Exception ex)
        {
            Console.SetOut(originalOut);
            _output.WriteLine("");
            _output.WriteLine("❌ EXCEPTION THROWN");
            _output.WriteLine($"   Type: {ex.GetType().Name}");
            _output.WriteLine($"   Message: {ex.Message}");
            _output.WriteLine($"   Stack: {ex.StackTrace}");
            throw;
        }
    }
}
