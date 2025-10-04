# Test Safety - READ THIS FIRST

## ⚠️ CRITICAL: LIVE ACCOUNT PROTECTION ⚠️

**ALL TESTS IN THIS PROJECT ARE READ-ONLY AND SAFE**

### Current Test Safety Status

✅ **ALL TESTS ARE SAFE TO RUN**

**NO TESTS CONNECT TO LIVE IBKR ACCOUNT**
**NO TESTS PLACE ORDERS**
**NO TESTS MODIFY ACCOUNT STATE**

## Test Files

### 1. RulesEngineTests.cs
- **Safe:** ✅ YES
- **IBKR Connection:** NO
- **Order Placement:** NO
- **Description:** Pure logic tests for OptionsRadar rules
- **Can run anytime:** YES

### 2. MinimalMcpServerTests.cs
- **Safe:** ✅ YES
- **IBKR Connection:** NO (uses MockAutoRevOption)
- **Order Placement:** NO (act_on_order is demo only)
- **Description:** MCP protocol tests with mock backend
- **Can run anytime:** YES

### 3. MonitorMcpServerTests_Simple.cs
- **Safe:** ✅ YES
- **IBKR Connection:** NO
- **Order Placement:** NO
- **Description:** Protocol validation, no actual IBKR calls
- **Can run anytime:** YES

## Running Tests

```bash
# Run all tests (100% safe)
dotnet test

# All tests are safe - no live account risk
```

## What These Tests Do NOT Do

❌ Connect to IBKR Gateway
❌ Connect to live trading account
❌ Place orders
❌ Modify positions
❌ Change account settings
❌ Subscribe to market data
❌ Make any account modifications

## Manual Testing (If Needed)

If you need to manually test Monitor MCP with real IBKR:

**REQUIRED PREREQUISITES:**
1. ✅ `IsPaperTrading: true` in secrets.json
2. ✅ Port 7497 (paper trading, NOT 7496)
3. ✅ Paper Trading IB Gateway running
4. ✅ Manual execution only (not automated)

**See:** [Test Safety Guide](../DOCS/Test_Safety_Guide.md)

## Adding New Tests

If you add tests that connect to IBKR:

### ✅ Required Safety Measures

```csharp
[Fact(Skip = "Manual test only - requires paper trading")]
[Trait("Category", "Integration")]
public async Task MyIntegrationTest()
{
    // SAFETY CHECK
    var config = SecretConfig.LoadFromFile("../secrets.json");
    if (!config.IBKRCredentials.IsPaperTrading)
    {
        throw new InvalidOperationException(
            "SAFETY: Paper trading required. Set IsPaperTrading: true"
        );
    }

    // Test code (READ-ONLY operations only)...
}
```

### ❌ NEVER Do This

```csharp
// ❌ DANGEROUS - NO SAFETY CHECK
[Fact]
public async Task PlaceOrder()
{
    await ibkr.PlaceOrderAsync(order); // ❌ COULD AFFECT LIVE ACCOUNT
}
```

## Documentation

Full safety guide: [Test_Safety_Guide.md](../DOCS/Test_Safety_Guide.md)

---

**⚠️ REMEMBER: ALL CURRENT TESTS ARE SAFE ⚠️**

**No live account risk with current test implementation.**

*Last Updated: 2025-10-04*
