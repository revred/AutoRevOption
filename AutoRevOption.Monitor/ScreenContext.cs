// ScreenContext.cs — Screen Context Server for IBKR Monitor (read-only operations)

using System.Text.Json;
using AutoRevOption.Monitor;
using AutoRevOption.Shared.Configuration;
using AutoRevOption.Shared.Portal;
using AutoRevOption.Shared.Context;

namespace AutoRevOption.Monitor.Mcp;

public interface IScreenContext : IMcpServer
{
}

public class ScreenContext : IScreenContext
{
    private readonly Connection _ibkr;

    public string Name => "AutoRevOption-Screen";
    public string Version => "1.0.0";

    public ScreenContext(Connection ibkr)
    {
        _ibkr = ibkr;
    }

    public async Task<McpResponse> HandleRequest(McpRequest request)
    {
        try
        {
            return request.Method?.ToLower() switch
            {
                "initialize" => HandleInitialize(),
                "tools/list" => HandleListTools(),
                "tools/call" => await HandleToolCall(request),
                _ => new McpResponse
                {
                    Error = new McpError
                    {
                        Code = -32601,
                        Message = $"Method not found: {request.Method}"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                }
            };
        }
    }

    private McpResponse HandleInitialize()
    {
        return new McpResponse
        {
            Result = new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new
                {
                    name = Name,
                    version = Version
                },
                capabilities = new
                {
                    tools = new { }
                }
            }
        };
    }

    private McpResponse HandleListTools()
    {
        var tools = new object[]
        {
            new
            {
                name = "get_connection_status",
                description = "Get IBKR Gateway connection status (running, connected, port).",
                inputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "get_account_summary",
                description = "Get account summary with net liquidation, cash, buying power, margin.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        accountId = new
                        {
                            type = "string",
                            description = "Account ID (default: 'All' for primary account)"
                        }
                    }
                }
            },
            new
            {
                name = "get_positions",
                description = "Get all open positions (stocks, options) with quantity and avg cost.",
                inputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "get_option_positions",
                description = "Get only options positions with greeks, expiry, strike details.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        ticker = new
                        {
                            type = "string",
                            description = "Filter by ticker symbol (optional)"
                        }
                    }
                }
            },
            new
            {
                name = "get_account_greeks",
                description = "Calculate portfolio-wide greeks (delta, gamma, theta, vega) from all option positions.",
                inputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "check_gateway",
                description = "Check if IB Gateway is running and attempt auto-launch if configured.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        autoLaunch = new
                        {
                            type = "boolean",
                            description = "Attempt auto-launch if not running (default: false)"
                        }
                    }
                }
            }
        };

        return new McpResponse
        {
            Result = new { tools }
        };
    }

    private async Task<McpResponse> HandleToolCall(McpRequest request)
    {
        var toolName = request.Params?.Name;
        var argsJson = request.Params?.Arguments;

        // Convert JsonElement to Dictionary for existing tool methods
        var args = new Dictionary<string, object>();
        if (argsJson.HasValue && argsJson.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in argsJson.Value.EnumerateObject())
            {
                args[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? "",
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    _ => prop.Value.ToString()
                };
            }
        }

        try
        {
            var result = toolName switch
            {
                "get_connection_status" => await GetConnectionStatus(),
                "get_account_summary" => await GetAccountSummary(args),
                "get_positions" => await GetPositions(),
                "get_option_positions" => await GetOptionPositions(args),
                "get_account_greeks" => await GetAccountGreeks(),
                "check_gateway" => await CheckGateway(args),
                _ => throw new Exception($"Unknown tool: {toolName}")
            };

            return new McpResponse
            {
                Result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32000,
                    Message = $"Tool execution failed: {ex.Message}"
                }
            };
        }
    }

    // Tool Implementations

    private Task<object> GetConnectionStatus()
    {
        var connected = _ibkr.IsConnected();

        return Task.FromResult<object>(new
        {
            connected,
            apiUrl = "https://localhost:5000/v1/api",
            timestamp = DateTime.UtcNow
        });
    }

    private async Task<object> GetAccountSummary(Dictionary<string, object> args)
    {
        var accountId = args.ContainsKey("accountId") ? args["accountId"]?.ToString() : "All";

        var account = await _ibkr.GetAccountInfoAsync();

        if (account == null)
        {
            throw new Exception("Failed to retrieve account data from IBKR");
        }

        return new
        {
            accountId = account.AccountId,
            netLiquidation = account.NetLiquidation,
            cash = account.Cash,
            buyingPower = account.BuyingPower,
            maintenanceMargin = account.MaintenanceMargin,
            maintenancePct = account.MaintenancePct,
            timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> GetPositions()
    {
        var positions = await _ibkr.GetPositionsAsync();

        return new
        {
            count = positions.Count,
            positions = positions.Select(p => new
            {
                symbol = p.Symbol,
                secType = p.SecType,
                right = p.Right,
                strike = p.Strike,
                expiry = p.Expiry,
                position = p.Position,
                avgCost = p.AvgCost,
                marketValue = p.MarketValue,
                unrealizedPnL = p.UnrealizedPnL
            }).ToList(),
            timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> GetOptionPositions(Dictionary<string, object> args)
    {
        var ticker = args.ContainsKey("ticker") ? args["ticker"]?.ToString() : null;

        var positions = await _ibkr.GetPositionsAsync();
        var optionPositions = positions.Where(p => p.SecType == "OPT");

        if (!string.IsNullOrEmpty(ticker))
        {
            optionPositions = optionPositions.Where(p =>
                p.Symbol.Equals(ticker, StringComparison.OrdinalIgnoreCase));
        }

        return new
        {
            count = optionPositions.Count(),
            ticker = ticker ?? "all",
            positions = optionPositions.Select(p => new
            {
                symbol = p.Symbol,
                right = p.Right,
                strike = p.Strike,
                expiry = p.Expiry,
                position = p.Position,
                avgCost = p.AvgCost,
                marketValue = p.MarketValue,
                unrealizedPnL = p.UnrealizedPnL
            }).ToList(),
            timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> GetAccountGreeks()
    {
        var positions = await _ibkr.GetPositionsAsync();
        var optionPositions = positions.Where(p => p.SecType == "OPT").ToList();

        if (!optionPositions.Any())
        {
            return new
            {
                totalDelta = 0.0,
                totalGamma = 0.0,
                totalTheta = 0.0,
                totalVega = 0.0,
                optionCount = 0,
                message = "No option positions found",
                timestamp = DateTime.UtcNow
            };
        }

        // Note: Greeks would come from real-time market data subscription
        // For now, return placeholder indicating data not available via snapshot
        return new
        {
            totalDelta = 0.0,
            totalGamma = 0.0,
            totalTheta = 0.0,
            totalVega = 0.0,
            optionCount = optionPositions.Count,
            message = "Greeks require real-time market data subscription (not implemented in snapshot mode)",
            positions = optionPositions.Select(p => new
            {
                symbol = p.Symbol,
                right = p.Right,
                strike = p.Strike,
                expiry = p.Expiry,
                quantity = p.Position
            }).ToList(),
            timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> CheckGateway(Dictionary<string, object> args)
    {
        // Gateway management is now handled by GatewayProcessManager singleton
        // This tool is deprecated but kept for backwards compatibility
        await Task.CompletedTask;

        return new
        {
            message = "Gateway management is now handled automatically by ClientPortalLoginService",
            apiUrl = "https://localhost:5000/v1/api",
            timestamp = DateTime.UtcNow
        };
    }
}
