// ProgramMcp.cs — MCP Server entry point for Monitor

using System.Text.Json;
using AutoRevOption.Monitor;
using AutoRevOption.Monitor.Mcp;

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
        // Load secrets
        var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "secrets.json");
        if (!File.Exists(secretsPath))
        {
            Console.Error.WriteLine($"[MCP] ERROR: secrets.json not found at {secretsPath}");
            Environment.Exit(1);
            return;
        }

        SecretConfig config;
        try
        {
            config = SecretConfig.LoadFromFile(secretsPath);
            Console.Error.WriteLine($"[MCP] Loaded secrets from {secretsPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MCP] ERROR: Failed to load secrets: {ex.Message}");
            Environment.Exit(1);
            return;
        }

        // Initialize Gateway Manager
        var gatewayManager = new GatewayManager(config.IBKRCredentials);
        Console.Error.WriteLine($"[MCP] Gateway Status: {gatewayManager.GetStatus()}");

        // Ensure Gateway is running
        var gatewayReady = await gatewayManager.EnsureGatewayRunningAsync();
        if (!gatewayReady)
        {
            Console.Error.WriteLine("[MCP] WARNING: IB Gateway is not running");
            Console.Error.WriteLine("[MCP] MCP server will start, but IBKR operations will fail until Gateway is running");
        }

        // Create IBKR connection
        var ibkr = new IbkrConnection(config.IBKRCredentials);

        // Connect to IBKR
        var connected = await ibkr.ConnectAsync();
        if (!connected)
        {
            Console.Error.WriteLine("[MCP] WARNING: Failed to connect to IBKR API");
            Console.Error.WriteLine("[MCP] MCP server will start, but IBKR operations will fail");
        }
        else
        {
            Console.Error.WriteLine("[MCP] ✅ Connected to IBKR API");
        }

        // Create MCP server
        var server = new MonitorMcpServer(ibkr, gatewayManager, config.IBKRCredentials);

        Console.Error.WriteLine($"[MCP] {server.Name} v{server.Version} started");
        Console.Error.WriteLine("[MCP] Listening on stdio...");

        // Start Gateway monitoring in background
        var cts = new CancellationTokenSource();
        var monitorTask = Task.Run(() => gatewayManager.MonitorGatewayAsync(cts.Token));

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

            Console.Error.WriteLine("[MCP] Server stopped");
        }
    }
}
