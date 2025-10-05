// ProgramMcp.cs — MCP Server entry point

using System.Text.Json;
using AutoRevOption;
using AutoRevOption.Shared.Context;

public static class ProgramMcp
{
    private static readonly string[] Universe = new[] { "APP","SOFI","META","GOOGL","AMD","AAL","SHOP","MRVL","PLTR","TSLA","MSFT","ZETA" };

    public static async Task MainMcp(string[] args)
    {
        // Check if running in MCP mode
        if (args.Length > 0 && args[0] == "--mcp")
        {
            await RunMcpServer();
        }
        else
        {
            Console.WriteLine("AutoRevOption MCP Server");
            Console.WriteLine("Usage: dotnet run -- --mcp");
            Console.WriteLine("");
            Console.WriteLine("Or use the interactive console:");
            Console.WriteLine("  dotnet run");
        }
    }

    private static async Task RunMcpServer()
    {
        var radar = new MockAutoRevOption();
        var server = new AutoRevOptionMcpServer(radar, Universe);

        // MCP server runs via stdio
        Console.Error.WriteLine($"[MCP] {server.Name} v{server.Version} started");
        Console.Error.WriteLine("[MCP] Listening on stdio...");

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

        Console.Error.WriteLine("[MCP] Server stopped");
    }
}
