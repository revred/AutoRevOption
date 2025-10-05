// ProgramMcp.cs — MCP Server entry point for Monitor

using System.Text.Json;
using AutoRevOption.Monitor;
using AutoRevOption.Monitor.Mcp;
using AutoRevOption.Shared.Portal;
using AutoRevOption.Shared.Context;
using AutoRevOption.Client;

namespace AutoRevOption.Monitor;

public static class ProgramMcp
{
    public static async Task MainMcp(string[] args)
    {
        // Check if running in MCP mode
        if (args.Length > 0 && args[0] == "--mcp")
        {
            await RunMcpServer();
        }
        else
        {
            Console.WriteLine("AutoRevOption.Monitor MCP Server");
            Console.WriteLine("Usage: dotnet run -- --mcp");
            Console.WriteLine("");
            Console.WriteLine("Or use the interactive monitor:");
            Console.WriteLine("  dotnet run");
        }
    }

    private static async Task RunMcpServer()
    {
        // Use ClientPortalLoginService to handle authentication and secrets
        Console.Error.WriteLine("[MCP] Initializing Client Portal login service...");
        var loginService = new ClientPortalLoginService();

        AutoRevClient? client = null;
        try
        {
            // LoginAsync will:
            // 1. Load secrets from AutoRevOption.Client folder (internally)
            // 2. Ensure gateway is running via GatewayProcessManager
            // 3. Perform browser login if needed (with 2FA support)
            client = await loginService.LoginAsync(headless: true, twoFactorTimeoutMinutes: 2);
            Console.Error.WriteLine("[MCP] ✅ Logged in to Client Portal API");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MCP] WARNING: Login failed: {ex.Message}");
            Console.Error.WriteLine("[MCP] MCP server will start, but IBKR operations will fail");
        }

        // Create IBKR connection wrapper using the authenticated client
        var ibkr = client != null
            ? new Connection(client)
            : new Connection(new AutoRevClient("https://localhost:5000/v1/api"));

        // Create MCP server
        var server = new ScreenContext(ibkr);

        Console.Error.WriteLine($"[MCP] {server.Name} v{server.Version} started");
        Console.Error.WriteLine("[MCP] Listening on stdio...");

        try
        {
            using var stdin = Console.OpenStandardInput();
            using var stdout = Console.OpenStandardOutput();
            using var reader = new StreamReader(stdin);
            using var writer = new StreamWriter(stdout) { AutoFlush = true };

            while (true)
            {
                try
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break; // EOF

                    Console.Error.WriteLine($"[MCP] Received: {line[..Math.Min(100, line.Length)]}...");

                    McpRequest? request;
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        request = JsonSerializer.Deserialize<McpRequest>(line, options);
                    }
                    catch (JsonException ex)
                    {
                        Console.Error.WriteLine($"[MCP] JSON parse error: {ex.Message}");
                        continue;
                    }

                    if (request == null)
                    {
                        Console.Error.WriteLine("[MCP] Null request");
                        continue;
                    }

                    var response = await server.HandleRequest(request);
                    var responseJson = JsonSerializer.Serialize(response);

                    await writer.WriteLineAsync(responseJson);
                    Console.Error.WriteLine($"[MCP] Sent: {responseJson[..Math.Min(100, responseJson.Length)]}...");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[MCP] Error: {ex.Message}");
                    Console.Error.WriteLine($"[MCP] Stack: {ex.StackTrace}");
                }
            }
        }
        finally
        {
            Console.Error.WriteLine("[MCP] Shutting down...");
            ibkr.Disconnect();
            ibkr.Dispose();
            Console.Error.WriteLine("[MCP] Server stopped");
        }
    }
}
