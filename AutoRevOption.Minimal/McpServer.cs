// McpServer.cs — MCP Server implementation for AutoRevOption

using System.Text.Json;
using AutoRevOption.Shared.Mcp;

namespace AutoRevOption;

public class AutoRevOptionMcpServer : IMcpServer
{
    private readonly IAutoRevOption _radar;
    private readonly string[] _universe;
    private readonly string _version = "1.0.0";

    public AutoRevOptionMcpServer(IAutoRevOption radar, string[] universe)
    {
        _radar = radar;
        _universe = universe;
    }

    public string Name => "AutoRevOption";
    public string Version => _version;

    public async Task<McpResponse> HandleRequest(McpRequest request)
    {
        return request.Method?.ToLower() switch
        {
            "initialize" => HandleInitialize(request),
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

    private McpResponse HandleInitialize(McpRequest request)
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
                name = "scan_candidates",
                description = "Scan the universe for options trade candidates. Returns ranked candidates based on OptionsRadar rules (PCS, CCS, BPS, BCS).",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        universe = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "List of ticker symbols to scan (e.g., ['AAPL', 'MSFT']). Leave empty to scan default universe."
                        }
                    }
                }
            },
            new
            {
                name = "validate_candidate",
                description = "Validate a candidate against OptionsRadar rules (DTE, delta, credit/width, R:R ratios).",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        candidateId = new
                        {
                            type = "string",
                            description = "Candidate ID from scan results"
                        }
                    },
                    required = new[] { "candidateId" }
                }
            },
            new
            {
                name = "verify_candidate",
                description = "Verify a candidate against risk gates and account limits.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        candidateId = new
                        {
                            type = "string",
                            description = "Candidate ID to verify"
                        },
                        accountId = new
                        {
                            type = "string",
                            description = "Account ID (default: 'ibkr:primary')"
                        }
                    },
                    required = new[] { "candidateId" }
                }
            },
            new
            {
                name = "build_order_plan",
                description = "Build an order plan with entry and exit brackets (TP/SL) for a candidate.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        candidateId = new
                        {
                            type = "string",
                            description = "Candidate ID to build order for"
                        },
                        quantity = new
                        {
                            type = "integer",
                            description = "Number of contracts (default: 1)"
                        }
                    },
                    required = new[] { "candidateId" }
                }
            },
            new
            {
                name = "get_account_status",
                description = "Get account snapshot (net liq, margin, delta, theta).",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        accountId = new
                        {
                            type = "string",
                            description = "Account ID (default: 'ibkr:primary')"
                        }
                    }
                }
            },
            new
            {
                name = "act_on_order",
                description = "Submit an order plan to IBKR (requires confirmation code). DEMO ONLY - does not place real orders.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        orderPlanId = new
                        {
                            type = "string",
                            description = "Order plan ID from build_order_plan"
                        },
                        confirmationCode = new
                        {
                            type = "string",
                            description = "Confirmation code (format: CONFIRM-{orderPlanId})"
                        },
                        paper = new
                        {
                            type = "boolean",
                            description = "Use paper trading (default: true)"
                        }
                    },
                    required = new[] { "orderPlanId", "confirmationCode" }
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
        try
        {
            var args = request.Params?.Arguments;
            var toolName = request.Params?.Name ?? "";

            return toolName switch
            {
                "scan_candidates" => await HandleScanCandidates(args),
                "validate_candidate" => await HandleValidateCandidate(args),
                "verify_candidate" => await HandleVerifyCandidate(args),
                "build_order_plan" => await HandleBuildOrderPlan(args),
                "get_account_status" => await HandleGetAccountStatus(args),
                "act_on_order" => await HandleActOnOrder(args),
                _ => new McpResponse
                {
                    Error = new McpError
                    {
                        Code = -32602,
                        Message = $"Unknown tool: {toolName}"
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

    private async Task<McpResponse> HandleScanCandidates(JsonElement? args)
    {
        var universe = _universe;
        if (args?.TryGetProperty("universe", out var universeArg) == true && universeArg.ValueKind == JsonValueKind.Array)
        {
            universe = universeArg.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        var candidates = await _radar.ScanAsync(universe);

        return new McpResponse
        {
            Result = new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            count = candidates.Count,
                            candidates = candidates.Select(c => new
                            {
                                id = c.Id,
                                ticker = c.Ticker,
                                type = c.Type.ToString(),
                                expiry = c.Legs.FirstOrDefault()?.Exp.ToString("yyyy-MM-dd"),
                                width = c.Width,
                                credit = c.Credit,
                                debit = c.Debit,
                                shortDelta = c.ShortDelta,
                                ivRank = c.IvRank,
                                score = c.Score,
                                playbook = c.Playbook,
                                notes = c.Notes
                            })
                        }, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            }
        };
    }

    private async Task<McpResponse> HandleValidateCandidate(JsonElement? args)
    {
        if (args?.TryGetProperty("candidateId", out var idArg) != true)
        {
            return ErrorResponse("candidateId is required");
        }

        var candidateId = idArg.GetString() ?? "";
        var candidates = await _radar.ScanAsync(_universe);
        var candidate = candidates.FirstOrDefault(c => c.Id == candidateId);

        if (candidate == null)
        {
            return ErrorResponse($"Candidate not found: {candidateId}");
        }

        var validation = await _radar.ValidateAsync(candidate);

        return new McpResponse
        {
            Result = new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            candidateId,
                            valid = validation.Ok,
                            issues = validation.Issues
                        }, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            }
        };
    }

    private async Task<McpResponse> HandleVerifyCandidate(JsonElement? args)
    {
        if (args?.TryGetProperty("candidateId", out var idArg) != true)
        {
            return ErrorResponse("candidateId is required");
        }

        var candidateId = idArg.GetString() ?? "";
        var accountId = "ibkr:primary";
        if (args?.TryGetProperty("accountId", out var acctArg) == true)
        {
            accountId = acctArg.GetString() ?? accountId;
        }

        var candidates = await _radar.ScanAsync(_universe);
        var candidate = candidates.FirstOrDefault(c => c.Id == candidateId);

        if (candidate == null)
        {
            return ErrorResponse($"Candidate not found: {candidateId}");
        }

        var verification = await _radar.VerifyAsync(accountId, candidate);

        return new McpResponse
        {
            Result = new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            candidateId,
                            verified = verification.Ok,
                            score = verification.Score,
                            reason = verification.Reason,
                            slippage = verification.Slippage
                        }, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            }
        };
    }

    private async Task<McpResponse> HandleBuildOrderPlan(JsonElement? args)
    {
        if (args?.TryGetProperty("candidateId", out var idArg) != true)
        {
            return ErrorResponse("candidateId is required");
        }

        var candidateId = idArg.GetString() ?? "";
        var quantity = 1;
        if (args?.TryGetProperty("quantity", out var qtyArg) == true)
        {
            quantity = qtyArg.GetInt32();
        }

        var candidates = await _radar.ScanAsync(_universe);
        var candidate = candidates.FirstOrDefault(c => c.Id == candidateId);

        if (candidate == null)
        {
            return ErrorResponse($"Candidate not found: {candidateId}");
        }

        var orderPlan = await _radar.BuildOrderPlanAsync(candidate, quantity);

        return new McpResponse
        {
            Result = new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(orderPlan, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            }
        };
    }

    private async Task<McpResponse> HandleGetAccountStatus(JsonElement? args)
    {
        var accountId = "ibkr:primary";
        if (args?.TryGetProperty("accountId", out var acctArg) == true)
        {
            accountId = acctArg.GetString() ?? accountId;
        }

        var account = await _radar.GetAccountAsync(accountId);

        return new McpResponse
        {
            Result = new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            accountId,
                            netLiquidation = account.NetLiq,
                            maintenancePercent = account.MaintPct,
                            accountDelta = account.AccountDelta,
                            accountTheta = account.AccountTheta
                        }, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            }
        };
    }

    private async Task<McpResponse> HandleActOnOrder(JsonElement? args)
    {
        if (args?.TryGetProperty("orderPlanId", out var idArg) != true)
        {
            return ErrorResponse("orderPlanId is required");
        }
        if (args?.TryGetProperty("confirmationCode", out var codeArg) != true)
        {
            return ErrorResponse("confirmationCode is required");
        }

        var orderPlanId = idArg.GetString() ?? "";
        var confirmationCode = codeArg.GetString() ?? "";
        var paper = true;
        if (args?.TryGetProperty("paper", out var paperArg) == true)
        {
            paper = paperArg.GetBoolean();
        }

        // Rebuild order plan (in real implementation, would retrieve from cache)
        var candidates = await _radar.ScanAsync(_universe);
        var candidate = candidates.FirstOrDefault();
        if (candidate == null)
        {
            return ErrorResponse("No candidates available");
        }

        var orderPlan = await _radar.BuildOrderPlanAsync(candidate);
        var (ok, message) = await _radar.ActAsync(orderPlan, confirmationCode, paper);

        return new McpResponse
        {
            Result = new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            success = ok,
                            message,
                            orderPlanId,
                            mode = paper ? "Paper Trading" : "Live Trading"
                        }, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            }
        };
    }

    private McpResponse ErrorResponse(string message)
    {
        return new McpResponse
        {
            Error = new McpError
            {
                Code = -32602,
                Message = message
            }
        };
    }
}
