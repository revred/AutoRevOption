using AutoRevOption.Shared.Portal;
using System;
using System.Threading.Tasks;

namespace AutoRevOption.Tests;

/// <summary>
/// Quick smoke test for Client Portal API connectivity
/// </summary>
public class AutoRevClientTests
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Client Portal API Connection Test ===\n");

        using var client = new AutoRevClient("https://localhost:5000/v1/api");

        try
        {
            // 1. Test authentication status
            Console.WriteLine("1. Checking authentication status...");
            var authStatus = await client.GetAuthStatusAsync();

            if (authStatus == null)
            {
                Console.WriteLine("❌ Failed to get auth status");
                return 1;
            }

            Console.WriteLine($"   Authenticated: {authStatus.Authenticated}");
            Console.WriteLine($"   Competing: {authStatus.Competing}");
            Console.WriteLine($"   Connected: {authStatus.Connected}");

            if (!authStatus.Authenticated)
            {
                Console.WriteLine("\n⚠️  Not authenticated - please:");
                Console.WriteLine("   1. Open https://localhost:5000 in browser");
                Console.WriteLine("   2. Log in with IBKR credentials");
                Console.WriteLine("   3. Re-run this test");
                return 2;
            }

            Console.WriteLine("✅ Authenticated\n");

            // 2. Test accounts
            Console.WriteLine("2. Fetching portfolio accounts...");
            var accounts = await client.GetAccountsAsync();

            if (accounts == null || accounts.Count == 0)
            {
                Console.WriteLine("❌ No accounts found");
                return 3;
            }

            Console.WriteLine($"✅ Found {accounts.Count} account(s):");
            foreach (var account in accounts)
            {
                Console.WriteLine($"   - {account.AccountId} ({account.AccountAlias})");
            }
            Console.WriteLine();

            // 3. Test positions for first account
            var firstAccount = accounts[0].AccountId;
            Console.WriteLine($"3. Fetching positions for account {firstAccount}...");

            var positions = await client.GetPositionsAsync(firstAccount);

            if (positions == null)
            {
                Console.WriteLine("❌ Failed to get positions");
                return 4;
            }

            Console.WriteLine($"✅ Found {positions.Count} position(s):");
            foreach (var pos in positions)
            {
                Console.WriteLine($"   - ConId: {pos.ConId}, Position: {pos.PositionSize}, MktValue: {pos.MarketValue:C}");
            }
            Console.WriteLine();

            // 4. Test account summary
            Console.WriteLine($"4. Fetching account summary...");
            var summary = await client.GetAccountSummaryAsync();

            if (summary == null)
            {
                Console.WriteLine("❌ Failed to get account summary");
                return 5;
            }

            Console.WriteLine("✅ Account summary:");
            Console.WriteLine($"   Account ID: {summary.AccountId}");
            Console.WriteLine($"   Account Title: {summary.AccountTitle}");
            Console.WriteLine($"   Type: {summary.Type}");
            Console.WriteLine($"   Currency: {summary.Currency}");
            Console.WriteLine();

            Console.WriteLine("=== All Tests Passed ===");
            Console.WriteLine("CPAPI client is working correctly!");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\n❌ HTTP Error: {ex.Message}");
            Console.WriteLine("\nMake sure Client Portal Gateway is running:");
            Console.WriteLine("  C:\\IBKR\\clientportal\\bin\\run.bat");
            return 6;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Unexpected error: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
            return 7;
        }
    }
}
