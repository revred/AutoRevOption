using AutoRevOption.Shared.Configuration;
using AutoRevOption.Shared.Portal;

namespace AutoRevOption.Client.Test;

/// <summary>
/// Test automated Client Portal login with 2FA
/// </summary>
internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Client Portal Automated Login Test ===\n");

        // Load credentials from secrets.json (in AutoRevOption.Client folder)
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var secretsPath = Path.Combine(projectRoot, "secrets.json");
        if (!File.Exists(secretsPath))
        {
            Console.WriteLine($"❌ secrets.json not found at: {secretsPath}");
            Console.WriteLine("Please create secrets.json with your IBKR credentials.");
            return 1;
        }

        var config = SecretConfig.LoadFromFile(secretsPath);

        if (string.IsNullOrEmpty(config.IBKRCredentials.Username) ||
            string.IsNullOrEmpty(config.IBKRCredentials.Password))
        {
            Console.WriteLine("❌ Username or Password not configured in secrets.json");
            Console.WriteLine("Please add your IBKR credentials to secrets.json:");
            Console.WriteLine("  \"Username\": \"your_username\",");
            Console.WriteLine("  \"Password\": \"your_password\"");
            return 1;
        }

        using var client = new AutoRevClient("https://localhost:5000/v1/api");

        try
        {
            // 1. Check if already authenticated
            Console.WriteLine("1. Checking current authentication status...");
            var authStatus = await client.GetAuthStatusAsync();

            if (authStatus?.Authenticated == true && authStatus.Connected)
            {
                Console.WriteLine("✅ Already authenticated!\n");
            }
            else
            {
                // 2. Perform automated browser login
                Console.WriteLine($"2. Starting automated browser login for: {config.IBKRCredentials.Username}");
                Console.WriteLine("   Running headless Chrome browser...\n");

                var browserLogin = new ClientPortalBrowserLogin();
                var loginSuccess = await browserLogin.LoginAsync(
                    config.IBKRCredentials.Username,
                    config.IBKRCredentials.Password,
                    headless: true,  // Run in headless mode
                    twoFactorTimeoutMinutes: 2,
                    keepSessionAlive: true  // Keep browser running
                );

                if (!loginSuccess)
                {
                    Console.WriteLine("❌ Browser login failed");
                    browserLogin.Dispose();
                    return 2;
                }

                Console.WriteLine("✅ Browser login successful\n");

                // Give session time to propagate to API
                Console.WriteLine("   Waiting for session to propagate...");
                await Task.Delay(5000);

                // Note: Browser session is kept alive - don't dispose here
            }

            // 4. Test connection - get accounts
            Console.WriteLine("\n4. Fetching portfolio accounts...");
            var accounts = await client.GetAccountsAsync();

            if (accounts == null || accounts.Count == 0)
            {
                Console.WriteLine("❌ No accounts found");
                return 4;
            }

            Console.WriteLine($"✅ Found {accounts.Count} account(s):");
            foreach (var account in accounts)
            {
                Console.WriteLine($"   - {account.AccountId} ({account.DisplayName ?? account.AccountAlias})");
            }

            // 5. Get positions for first account
            var firstAccount = accounts[0].AccountId;
            Console.WriteLine($"\n5. Fetching positions for account {firstAccount}...");

            var positions = await client.GetPositionsAsync(firstAccount);

            if (positions != null)
            {
                Console.WriteLine($"✅ Found {positions.Count} position(s)");
                foreach (var pos in positions.Take(5))
                {
                    Console.WriteLine($"   - {pos.ContractDesc}: {pos.PositionSize} @ {pos.MarketPrice:C}");
                }
            }

            Console.WriteLine("\n=== SUCCESS ===");
            Console.WriteLine("Automated login completed successfully!");
            Console.WriteLine("Your application can now use the Client Portal API without browser interaction.");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\n❌ HTTP Error: {ex.Message}");
            Console.WriteLine("\nMake sure Client Portal Gateway is running.");
            return 5;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            return 6;
        }
    }
}
