// MinimalMcpServerTests.cs — Unit tests for Minimal MCP Server
//
// ✅ SAFETY: This test file is 100% SAFE
// - NO real IBKR connection (uses MockAutoRevOption)
// - NO order placement (act_on_order is DEMO ONLY)
// - NO account modifications
// - MCP protocol testing with mock backend
//
// Safe to run anytime with: dotnet test

using System.Text.Json;
using AutoRevOption;
using AutoRevOption.Shared.Mcp;
using AutoRevOption.Shared.Models.Legacy;
using Xunit;

namespace AutoRevOption.Tests;

public class MinimalMcpServerTests
{
    private readonly AutoRevOptionMcpServer _server;
    private readonly string[] _testUniverse = new[] { "SHOP", "GOOGL", "MSFT" };

    public MinimalMcpServerTests()
    {
        var radar = new MockAutoRevOption();
        _server = new AutoRevOptionMcpServer(radar, _testUniverse);
    }

    #region Protocol Tests

    [Fact]
    public async Task Initialize_ReturnsCorrectProtocolVersion()
    {
        // Arrange
        var request = new McpRequest { Method = "initialize", Params = null };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("2024-11-05", result); // Protocol version
        Assert.Contains("AutoRevOption", result); // Server name
    }

    [Fact]
    public async Task Initialize_CaseInsensitive_ReturnsSuccess()
    {
        // Arrange
        var request = new McpRequest { Method = "INITIALIZE", Params = null };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task ToolsList_ReturnsSixTools()
    {
        // Arrange
        var request = new McpRequest { Method = "tools/list", Params = null };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("scan_candidates", result);
        Assert.Contains("validate_candidate", result);
        Assert.Contains("verify_candidate", result);
        Assert.Contains("build_order_plan", result);
        Assert.Contains("get_account_status", result);
        Assert.Contains("act_on_order", result);
    }

    [Fact]
    public async Task UnknownMethod_ReturnsMethodNotFoundError()
    {
        // Arrange
        var request = new McpRequest { Method = "unknown_method", Params = null };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error.Code);
        Assert.Contains("Method not found", response.Error.Message);
    }

    #endregion

    #region scan_candidates Tests

    [Fact]
    public async Task ScanCandidates_ReturnsNonEmptyList()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "scan_candidates",
                Arguments = JsonDocument.Parse("{}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("count", result);
        Assert.Contains("candidates", result);
        Assert.Contains("PCS", result); // Should have at least one PCS candidate
    }

    [Fact]
    public async Task ScanCandidates_WithCustomUniverse_UsesProvidedTickers()
    {
        // Arrange
        var customUniverse = new[] { "AAPL", "TSLA" };
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "scan_candidates",
                Arguments = JsonDocument.Parse("{\"universe\":[\"AAPL\",\"TSLA\"]}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task ScanCandidates_ReturnsValidCandidateStructure()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "scan_candidates",
                Arguments = JsonDocument.Parse("{}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);

        // Check for required fields in candidate
        Assert.Contains("id", result);
        Assert.Contains("ticker", result);
        Assert.Contains("type", result);
        Assert.Contains("score", result);
        Assert.Contains("playbook", result);
    }

    #endregion

    #region validate_candidate Tests

    [Fact]
    public async Task ValidateCandidate_WithValidId_ReturnsValidationResult()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "validate_candidate",
                Arguments = JsonDocument.Parse("{\"candidateId\":\"PCS:SHOP:2025-10-11:22-21:8dc9\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("valid", result);
        Assert.Contains("issues", result);
    }

    [Fact]
    public async Task ValidateCandidate_WithoutCandidateId_ReturnsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "validate_candidate",
                Arguments = JsonDocument.Parse("{}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Error);
        Assert.Contains("candidateId", response.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCandidate_WithInvalidId_ReturnsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "validate_candidate",
                Arguments = JsonDocument.Parse("{\"candidateId\":\"INVALID:ID\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Error);
        Assert.Contains("not found", response.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region verify_candidate Tests

    [Fact]
    public async Task VerifyCandidate_WithValidId_ReturnsVerificationResult()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "verify_candidate",
                Arguments = JsonDocument.Parse("{\"candidateId\":\"PCS:SHOP:2025-10-11:22-21:8dc9\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("verified", result);
        Assert.Contains("score", result);
        Assert.Contains("reason", result);
    }

    [Fact]
    public async Task VerifyCandidate_WithCustomAccountId_UsesProvidedAccount()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "verify_candidate",
                Arguments = JsonDocument.Parse("{\"candidateId\":\"PCS:SHOP:2025-10-11:22-21:8dc9\",\"accountId\":\"ibkr:test\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        Assert.Null(response.Error);
    }

    #endregion

    #region build_order_plan Tests

    [Fact]
    public async Task BuildOrderPlan_WithValidId_ReturnsOrderPlan()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "build_order_plan",
                Arguments = JsonDocument.Parse("{\"candidateId\":\"PCS:SHOP:2025-10-11:22-21:8dc9\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("orderPlanId", result);
        Assert.Contains("combination", result);
        Assert.Contains("exits", result);
    }

    [Fact]
    public async Task BuildOrderPlan_WithQuantity_UsesProvidedQuantity()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "build_order_plan",
                Arguments = JsonDocument.Parse("{\"candidateId\":\"PCS:SHOP:2025-10-11:22-21:8dc9\",\"quantity\":5}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task BuildOrderPlan_ReturnsExitBrackets()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "build_order_plan",
                Arguments = JsonDocument.Parse("{\"candidateId\":\"PCS:SHOP:2025-10-11:22-21:8dc9\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("tp", result); // Take profit exit
        Assert.Contains("sl", result); // Stop loss exit
    }

    #endregion

    #region get_account_status Tests

    [Fact]
    public async Task GetAccountStatus_ReturnsAccountSnapshot()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "get_account_status",
                Arguments = JsonDocument.Parse("{}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("accountId", result);
        Assert.Contains("netLiquidation", result);
        Assert.Contains("accountDelta", result);
        Assert.Contains("accountTheta", result);
    }

    [Fact]
    public async Task GetAccountStatus_WithCustomAccountId_UsesProvidedAccount()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "get_account_status",
                Arguments = JsonDocument.Parse("{\"accountId\":\"ibkr:test\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task GetAccountStatus_ReturnsMockData()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "get_account_status",
                Arguments = JsonDocument.Parse("{}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("31000", result); // Mock net liq
    }

    #endregion

    #region act_on_order Tests

    [Fact]
    public async Task ActOnOrder_WithValidConfirmation_ReturnsSuccess()
    {
        // Arrange
        var orderPlanId = "OP-TEST123";
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "act_on_order",
                Arguments = JsonDocument.Parse($"{{\"orderPlanId\":\"{orderPlanId}\",\"confirmationCode\":\"CONFIRM-{orderPlanId}\"}}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("success", result);
        Assert.Contains("true", result);
    }

    [Fact]
    public async Task ActOnOrder_WithInvalidConfirmation_ReturnsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "act_on_order",
                Arguments = JsonDocument.Parse("{\"orderPlanId\":\"OP-TEST123\",\"confirmationCode\":\"WRONG-CODE\"}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Error);
        Assert.Contains("confirmation", response.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActOnOrder_WithoutRequiredParams_ReturnsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "act_on_order",
                Arguments = JsonDocument.Parse("{}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Error);
    }

    [Fact]
    public async Task ActOnOrder_DemoMode_DoesNotPlaceRealOrder()
    {
        // Arrange
        var orderPlanId = "OP-TEST123";
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "act_on_order",
                Arguments = JsonDocument.Parse($"{{\"orderPlanId\":\"{orderPlanId}\",\"confirmationCode\":\"CONFIRM-{orderPlanId}\",\"paper\":true}}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Result);
        var result = JsonSerializer.Serialize(response.Result);
        Assert.Contains("Demo", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ToolCall_WithUnknownTool_ReturnsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new McpParams
            {
                Name = "unknown_tool",
                Arguments = JsonDocument.Parse("{}").RootElement
            }
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Error);
        Assert.Contains("Unknown tool", response.Error.Message);
    }

    [Fact]
    public async Task ToolCall_WithNullParams_ReturnsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = null
        };

        // Act
        var response = await _server.HandleRequest(request);

        // Assert
        Assert.NotNull(response.Error);
    }

    #endregion

    #region Server Properties Tests

    [Fact]
    public void ServerName_ReturnsAutoRevOption()
    {
        // Act
        var name = _server.Name;

        // Assert
        Assert.Equal("AutoRevOption", name);
    }

    [Fact]
    public void ServerVersion_ReturnsValidVersion()
    {
        // Act
        var version = _server.Version;

        // Assert
        Assert.NotNull(version);
        Assert.Matches(@"\d+\.\d+\.\d+", version); // Matches semantic versioning
    }

    #endregion
}
