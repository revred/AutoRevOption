# Test Safety Guide - CRITICAL

## ⚠️ LIVE ACCOUNT PROTECTION ⚠️

**ALL TESTS MUST BE READ-ONLY OR USE PAPER TRADING ONLY**

This guide ensures that **no tests ever modify a live trading account**.

## Critical Rules

### 1. **NEVER Run Tests Against Live Account**

Tests must **NEVER** be run when:
- `IsPaperTrading: false` in secrets.json
- Connected to live trading port (7496 or custom live port)
- Live IB Gateway is running

### 2. **All Tests Are Read-Only**

Current test implementation:
- ✅ **RulesEngineTests** - Pure logic tests, no IBKR connection
- ✅ **MinimalMcpServerTests** - Uses MockAutoRevOption (no real IBKR)
- ✅ **MonitorMcpServerTests_Simple** - Protocol validation only, no IBKR connection

**NO TESTS PLACE ORDERS OR MODIFY ACCOUNT**

### 3. **Monitor Tests Require Manual Validation**

Monitor MCP server tests that connect to IBKR **MUST**:
- Only be run manually
- Require explicit paper trading verification
- Never be part of automated CI/CD
- Only perform READ operations

## Test Categories

### Category 1: Safe Tests (Always Safe)

These tests **NEVER** connect to IBKR:

1. **RulesEngineTests.cs**
   - Tests OptionsRadar rule validation
   - Pure logic, no external connections
   - ✅ Safe for CI/CD

2. **MinimalMcpServerTests.cs**
   - Tests Minimal MCP server protocol
   - Uses MockAutoRevOption backend
   - No IBKR connection
   - ✅ Safe for CI/CD

3. **MonitorMcpServerTests_Simple.cs**
   - Tests Monitor MCP protocol definitions
   - No actual IBKR connection
   - Validates JSON schemas and expected responses
   - ✅ Safe for CI/CD

**Command:**
```bash
dotnet test
```
✅ **Safe to run anytime**

### Category 2: Manual Integration Tests (Requires Verification)

These tests would connect to IBKR if implemented:

**NOT IMPLEMENTED - Manual Testing Only**

If you need to test Monitor MCP with live IBKR:
1. ✅ Verify `IsPaperTrading: true` in secrets.json
2. ✅ Verify paper trading port (7497, not 7496)
3. ✅ Connect to Paper Trading IB Gateway only
4. ✅ Run manual tests interactively
5. ❌ NEVER automate these tests

## secrets.json Configuration

### ✅ Safe Configuration (Paper Trading)

```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7497,
    "ClientId": 1,
    "IsPaperTrading": true,
    "Username": "your_username"
  }
}
```

### ❌ UNSAFE Configuration (Live Trading)

```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7496,
    "ClientId": 1,
    "IsPaperTrading": false,  // ❌ DANGEROUS
    "Username": "your_username"
  }
}
```

**NEVER run tests with IsPaperTrading: false**

## Pre-Test Checklist

Before running **ANY** tests that might connect to IBKR:

- [ ] Check `IsPaperTrading: true` in secrets.json
- [ ] Verify paper trading port (7497)
- [ ] Confirm Paper Trading IB Gateway is running (not live)
- [ ] Review test code to ensure READ-ONLY operations
- [ ] Verify no order placement code in tests

## Current Test Implementation Safety

### RulesEngineTests.cs
**Status:** ✅ SAFE

**Why:** No IBKR connection, pure logic testing

**Tests:**
- DTE validation
- Delta range validation
- Credit/width ratio validation
- Risk gate validation

**No external connections**

### MinimalMcpServerTests.cs
**Status:** ✅ SAFE

**Why:** Uses MockAutoRevOption, no real IBKR connection

**Tests:**
- MCP protocol (initialize, tools/list, tools/call)
- All 6 tools (scan_candidates, validate_candidate, verify_candidate, build_order_plan, get_account_status, act_on_order)
- Error handling
- JSON serialization

**Backend:** MockAutoRevOption (demo data only)

**Order submission:** act_on_order is DEMO ONLY, never places real orders

### MonitorMcpServerTests_Simple.cs
**Status:** ✅ SAFE

**Why:** Protocol validation only, no IBKR connection

**Tests:**
- Tool name validation
- JSON request format validation
- Expected response structure validation
- Parameter requirements validation

**No actual IBKR calls**

## What Is NOT Tested Automatically

These require **manual testing** with paper trading account:

1. **Real IBKR Connection**
   - Gateway connection
   - Account data retrieval
   - Position queries
   - Market data subscription

2. **Order Placement**
   - Entry orders
   - Exit brackets (TP/SL)
   - OCA groups
   - Order modifications

3. **Live Market Data**
   - Option chains
   - Greeks calculation
   - Real-time quotes

**Reason:** These operations could potentially affect account state, even in read-only mode (e.g., market data subscriptions have costs).

## Manual Testing Protocol

If you need to manually test Monitor MCP with IBKR:

### Step 1: Verify Paper Trading
```bash
# Check secrets.json
cat secrets.json | grep IsPaperTrading
# Output should be: "IsPaperTrading": true
```

### Step 2: Verify Paper Trading Port
```bash
# Check port
cat secrets.json | grep Port
# Output should be: "Port": 7497  (paper trading)
```

### Step 3: Verify Paper Trading Gateway
```bash
# Check window title
# Should say "IB Gateway - Paper Trading"
# NOT "IB Gateway - Live Trading"
```

### Step 4: Run Monitor Interactively
```bash
cd AutoRevOption.Monitor
dotnet run
# Select option 1: Get Account Summary
# Verify account starts with "D" (demo) not "U" (live)
```

### Step 5: Manual MCP Testing
```bash
dotnet run -- --mcp
# Send read-only commands:
# - get_connection_status
# - get_account_summary
# - get_positions
```

**NEVER:**
- Run against live account
- Automate these tests
- Include in CI/CD pipeline

## CI/CD Configuration

### Safe CI/CD Test Command

```yaml
# .github/workflows/test.yml
- name: Run Tests
  run: dotnet test
  # Only runs Category 1 tests (safe tests)
```

### ❌ NEVER Do This in CI/CD

```yaml
# ❌ DANGEROUS - DO NOT DO THIS
- name: Run Integration Tests
  env:
    IBKR_USERNAME: ${{ secrets.IBKR_USERNAME }}
    IBKR_PASSWORD: ${{ secrets.IBKR_PASSWORD }}
  run: dotnet test --filter "Category=Integration"
```

**Why:** Even with paper trading, credentials should never be in CI/CD.

## Error Prevention

### Test Failure Modes

If a test accidentally connects to live account:

1. **Prevention:** Test code validates `IsPaperTrading: true`
2. **Detection:** Connection fails if not in test mode
3. **Mitigation:** All tests are read-only

### Current Implementation

**No tests connect to real IBKR**, so there is **zero risk** of live account modification.

## Future Test Development

If you add new tests that connect to IBKR:

### ✅ Required Safety Measures

1. **Validate Paper Trading:**
```csharp
[Fact]
public async Task MyTest_RequiresPaperTrading()
{
    // Arrange
    var credentials = SecretConfig.LoadFromFile("../secrets.json");

    // SAFETY CHECK
    if (!credentials.IBKRCredentials.IsPaperTrading)
    {
        throw new InvalidOperationException(
            "SAFETY: This test requires paper trading account. " +
            "Set IsPaperTrading: true in secrets.json"
        );
    }

    // Test code...
}
```

2. **Mark as Manual Test:**
```csharp
[Fact(Skip = "Manual test only - requires paper trading IB Gateway")]
public async Task MyManualTest()
{
    // Test code...
}
```

3. **Use Trait for Categorization:**
```csharp
[Fact]
[Trait("Category", "Integration")]
[Trait("Requires", "PaperTrading")]
public async Task MyIntegrationTest()
{
    // Test code...
}
```

### ❌ Never Do This

```csharp
// ❌ DANGEROUS - NO SAFETY CHECK
[Fact]
public async Task PlaceOrder_LiveAccount()
{
    var ibkr = new IbkrConnection(credentials);
    await ibkr.PlaceOrderAsync(order); // ❌ COULD AFFECT LIVE ACCOUNT
}
```

## Summary

| Test File | IBKR Connection | Order Placement | CI/CD Safe | Manual Required |
|-----------|----------------|-----------------|------------|-----------------|
| RulesEngineTests.cs | ❌ No | ❌ No | ✅ Yes | ❌ No |
| MinimalMcpServerTests.cs | ❌ No (Mock) | ❌ No (Demo) | ✅ Yes | ❌ No |
| MonitorMcpServerTests_Simple.cs | ❌ No | ❌ No | ✅ Yes | ❌ No |

**Current Status:** ✅ **ALL TESTS ARE SAFE**

**No tests connect to live IBKR**
**No tests place orders**
**No tests modify account state**

## Documentation References

- [Monitor MCP Setup](Monitor_MCP_Setup.md) - Manual testing guide
- [MCP Setup](MCP_Setup.md) - Minimal MCP manual testing
- [README](../README.md) - Quick start guide

---

**⚠️ REMEMBER: NEVER RUN TESTS AGAINST LIVE ACCOUNT ⚠️**

*Generated: 2025-10-04*
*Last Updated: Test safety documentation created*
