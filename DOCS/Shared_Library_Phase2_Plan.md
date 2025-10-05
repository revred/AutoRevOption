# AutoRevOption.Shared - Phase 2 Implementation Plan

## Status: Phase 1 Complete ✅

**Completed:**
- ✅ AutoRevOption.Shared library created (.NET 9)
- ✅ Configuration classes moved (SecretConfig, IBKRCredentials, etc.)
- ✅ MCP protocol types unified (McpRequest, McpResponse, McpParams, McpError)
- ✅ GatewayManager moved to Shared
- ✅ Legacy data models extracted (Candidate, OrderPlan, etc.)
- ✅ TVC models implemented (TVCSelection, ExecutionCard, complete spec)
- ✅ Project references added (Minimal, Monitor, Tests → Shared)
- ✅ Shared library builds successfully (0 errors, 0 warnings)
- ✅ Committed to GitHub

**Files Created:** 21 new files in AutoRevOption.Shared

---

## Phase 2: Update Namespaces & Remove Duplication

### Objective
Update Minimal and Monitor projects to use Shared library, removing duplicated code.

---

## Step 1: Update Minimal Project

### 1.1 Update McpServer.cs

**Remove duplicated types** (lines 506-529):
```csharp
// DELETE THESE (now in Shared):
public class McpRequest { ... }
public class McpParams { ... }
public class McpResponse { ... }
public class McpError { ... }
```

**Add using statements** (top of file):
```csharp
using AutoRevOption.Shared.Mcp;
```

### 1.2 Update Program.cs

**Remove duplicated data models** (lines 12-32):
```csharp
// DELETE THESE (now in Shared.Models.Legacy):
public enum StrategyType { ... }
public record OptionLeg(...);
public record Candidate(...);
public record OrderPlan(...);
public record Combo(...);
public record Exits(...);
public record TakeProfit(...);
public record StopLoss(...);
public record RiskGuards(...);
public record RiskCheckRequest(...);
public record ValidateResponse(...);
public record VerifyResponse(...);
public record AccountSnapshot(...);
```

**Add using statements**:
```csharp
using AutoRevOption.Shared.Models.Legacy;
```

### 1.3 Delete GatewayManager.cs

**File to delete:**
- `AutoRevOption.Minimal/GatewayManager.cs` (57 lines)

**Reason:** Lightweight version; Monitor's full implementation (236 lines) is in Shared

**Alternative:** If lightweight version needed, keep as `GatewayChecker.cs` utility

### 1.4 Update IbkrClient.cs (if needed)

**Check if using any types that should come from Shared**
- No immediate changes needed (IbkrClient is Minimal-specific placeholder)

---

## Step 2: Update Monitor Project

### 2.1 Delete SecretConfig.cs

**File to delete:**
- `AutoRevOption.Monitor/SecretConfig.cs` (51 lines)

**Reason:** Moved to Shared.Configuration

### 2.2 Update all Monitor files using SecretConfig

**Files to update:**
```
AutoRevOption.Monitor/Program.cs
AutoRevOption.Monitor/ProgramMcp.cs
AutoRevOption.Monitor/GatewayManager.cs (will be deleted)
AutoRevOption.Monitor/IbkrConnection.cs
```

**Change:**
```csharp
// OLD:
using AutoRevOption.Monitor;

// NEW:
using AutoRevOption.Monitor;
using AutoRevOption.Shared.Configuration;
```

### 2.3 Delete GatewayManager.cs

**File to delete:**
- `AutoRevOption.Monitor/GatewayManager.cs` (236 lines)

**Reason:** Moved to Shared.Ibkr

**Update references:**
```csharp
// OLD:
using AutoRevOption.Monitor;

// NEW:
using AutoRevOption.Monitor;
using AutoRevOption.Shared.Ibkr;
```

**Files affected:**
- `Program.cs` - uses GatewayManager
- `ProgramMcp.cs` - uses GatewayManager
- `MonitorMcpServer.cs` - uses GatewayManager

### 2.4 Update MonitorMcpServer.cs

**Remove duplicated types** (bottom of file):
```csharp
// DELETE THESE (now in Shared.Mcp):
public class McpRequest { ... }
public class McpParams { ... }
public class McpResponse { ... }
public class McpError { ... }
```

**Add using statements**:
```csharp
using AutoRevOption.Shared.Mcp;
using AutoRevOption.Shared.Configuration;
using AutoRevOption.Shared.Ibkr;
```

---

## Step 3: Update Tests Project

### 3.1 Update MinimalMcpServerTests.cs

**Add using statements**:
```csharp
using AutoRevOption.Shared.Mcp;
using AutoRevOption.Shared.Models.Legacy;
```

**No code changes needed** - tests use same types, just different namespace

### 3.2 Update MonitorMcpServerTests_Simple.cs

**Add using statements**:
```csharp
using AutoRevOption.Shared.Mcp;
```

**No code changes needed** - protocol validation only

### 3.3 Update RulesEngineTests.cs

**Add using statements** (if needed):
```csharp
using AutoRevOption.Shared.Models.Legacy;
```

**Check if any models used** - RulesEngine tests may not need changes

---

## Step 4: Build & Verify

### 4.1 Build Each Project

```bash
dotnet build AutoRevOption.Shared/AutoRevOption.Shared.csproj
dotnet build AutoRevOption.Minimal/AutoRevOption.Minimal.csproj
dotnet build AutoRevOption.Monitor/AutoRevOption.Monitor.csproj
dotnet build AutoRevOption.Tests/AutoRevOption.Tests.csproj
```

**Expected:** 0 errors, 0 warnings

### 4.2 Build Solution

```bash
dotnet build AutoRevOption.sln
```

**Expected:** 0 errors, 0 warnings

### 4.3 Run Tests

```bash
dotnet test
```

**Expected:** 75 tests passing (same as before)

---

## Step 5: Clean Up

### 5.1 Files to Delete

**Minimal:**
- `AutoRevOption.Minimal/GatewayManager.cs` (57 lines)

**Monitor:**
- `AutoRevOption.Monitor/SecretConfig.cs` (51 lines)
- `AutoRevOption.Monitor/GatewayManager.cs` (236 lines)

**Total reduction:** ~344 lines of duplicated code

### 5.2 Verify No Broken References

```bash
cd /c/Code/AutoRevOption
grep -r "namespace AutoRevOption.Monitor" --include="*.cs" AutoRevOption.Monitor/
grep -r "class SecretConfig" --include="*.cs" AutoRevOption.Monitor/
grep -r "class GatewayManager" --include="*.cs" AutoRevOption.Monitor/
```

**Expected:** No results (all moved to Shared)

---

## Step 6: Commit Phase 2

```bash
git add -A
git commit -m "Refactor: Use Shared library - Phase 2 (Remove duplication)

Update Minimal and Monitor to use AutoRevOption.Shared:

Minimal changes:
- Remove duplicated MCP types (McpRequest, McpResponse, etc.)
- Remove duplicated data models (Candidate, OrderPlan, etc.)
- Delete GatewayManager.cs (using Shared version)
- Add using AutoRevOption.Shared.*

Monitor changes:
- Delete SecretConfig.cs (moved to Shared)
- Delete GatewayManager.cs (moved to Shared)
- Remove duplicated MCP types
- Add using AutoRevOption.Shared.*

Tests changes:
- Add using statements for Shared namespaces
- No functional changes

Code reduction:
- Removed ~344 lines of duplicated code
- Maintained 100% test coverage (75 tests passing)

Build status:
- 0 errors, 0 warnings
- All tests passing

🤖 Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>"

git push
```

---

## Expected Outcomes

### Before Phase 2
- Shared library exists but not used
- Minimal/Monitor have duplicated code
- Tests passing: 75

### After Phase 2
- ✅ No duplicated MCP types
- ✅ No duplicated Configuration classes
- ✅ No duplicated GatewayManager
- ✅ No duplicated data models
- ✅ ~344 lines removed
- ✅ All tests still passing: 75
- ✅ Clean build (0 errors, 0 warnings)

---

## Namespace Migration Map

| Type | Old Namespace | New Namespace |
|------|--------------|---------------|
| McpRequest, McpResponse, etc. | AutoRevOption (Minimal) | AutoRevOption.Shared.Mcp |
| McpRequest, McpResponse, etc. | AutoRevOption.Monitor.Mcp | AutoRevOption.Shared.Mcp |
| SecretConfig, IBKRCredentials | AutoRevOption.Monitor | AutoRevOption.Shared.Configuration |
| GatewayManager | AutoRevOption.Monitor | AutoRevOption.Shared.Ibkr |
| Candidate, OrderPlan, etc. | AutoRevOption (Minimal) | AutoRevOption.Shared.Models.Legacy |
| StrategyType | AutoRevOption (Minimal) | AutoRevOption.Shared.Models.Legacy |

---

## Risk Mitigation

### Potential Issues

1. **Namespace conflicts**
   - Solution: Use fully qualified names if needed
   - Verify with build after each file update

2. **Test failures**
   - Solution: Run tests after each major change
   - Rollback if tests fail

3. **Missing dependencies**
   - Solution: Ensure all projects reference Shared
   - Already done in Phase 1

### Rollback Plan

If Phase 2 breaks:
1. `git revert HEAD`
2. Review error messages
3. Fix issues incrementally
4. Re-attempt Phase 2

---

## Estimated Time

- Step 1 (Minimal updates): 15 minutes
- Step 2 (Monitor updates): 20 minutes
- Step 3 (Tests updates): 10 minutes
- Step 4 (Build & verify): 10 minutes
- Step 5 (Clean up): 5 minutes
- Step 6 (Commit): 5 minutes

**Total:** ~65 minutes (1 hour)

---

## Next Phase: Phase 3 (Future)

Once Phase 2 is complete:

1. **Implement SelectTVC Service**
   - Use TVCSelection models from Shared
   - Implement per SelectTVC.md spec

2. **Implement WriteTVC Service**
   - Use ExecutionCard models from Shared
   - Implement per WriteTVC.md spec

3. **Add MCP Tools for TVC**
   - select_tvc tool
   - write_tvc tool
   - Integration with existing tools

4. **Deprecate Legacy Models**
   - Migrate from Candidate → TVCSelection
   - Update existing code gradually

---

## Success Criteria

Phase 2 is complete when:
- ✅ No duplicated code between Minimal/Monitor
- ✅ All projects use Shared library
- ✅ Build succeeds (0 errors, 0 warnings)
- ✅ All 75 tests passing
- ✅ Code reduction: ~344 lines
- ✅ Clean git commit
- ✅ Documentation updated

---

*Generated: 2025-10-04*
*Status: Phase 1 Complete, Phase 2 Ready to Execute*
