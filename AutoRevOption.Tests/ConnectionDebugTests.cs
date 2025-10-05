// ConnectionDebugTests.cs — Comprehensive IBKR connection debugging with verbose logging

using AutoRevOption.Shared.Configuration;
using AutoRevOption.Shared.Portal;
using Xunit;
using Xunit.Abstractions;

namespace AutoRevOption.Tests;

public class ConnectionDebugTests
{
    private readonly ITestOutputHelper _output;

    public ConnectionDebugTests(ITestOutputHelper output)
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
        _output.WriteLine($"   Username: {config.IBKRCredentials.Username}");
        _output.WriteLine($"   API URL: https://localhost:5000/v1/api");
        _output.WriteLine("");

        // Redirect Console.WriteLine to test output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Create connection
            _output.WriteLine("🔌 Creating Connection instance...");
            var connection = new Connection(config.IBKRCredentials);
            _output.WriteLine("✅ Instance created");
            _output.WriteLine("");

            // Attempt connection with Client Portal API
            _output.WriteLine("⏳ Attempting connection to Client Portal Gateway...");
            _output.WriteLine("   This will check authentication and start browser login if needed");
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
                _output.WriteLine("   2. Check Client Portal Gateway:");
                _output.WriteLine("      - Ensure gateway is running on port 5000");
                _output.WriteLine("      - Check browser authentication at https://localhost:5000");
                _output.WriteLine("");
                _output.WriteLine("   3. Check Network:");
                _output.WriteLine("      - Run: netstat -an | findstr :5000");
                _output.WriteLine("      - Should show: TCP [::]:5000 LISTENING");
                _output.WriteLine("");
                _output.WriteLine("   4. Check Credentials:");
                _output.WriteLine("      - Verify username/password in secrets.json");
                _output.WriteLine("      - Ensure 2FA is approved on mobile app");
                _output.WriteLine("");
                _output.WriteLine("   5. Restart Sequence:");
                _output.WriteLine("      - Stop Client Portal Gateway");
                _output.WriteLine("      - Wait 10 seconds");
                _output.WriteLine("      - Restart gateway and test connection");
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
