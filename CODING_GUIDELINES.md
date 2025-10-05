# AutoRevOption Coding Guidelines

## Project Structure

### File Organization
- **Root folder**: Keep clean - only solution files, README, LICENSE, .gitignore
- **Scripts**: All shell scripts go in `scripts/` folder
- **Documentation**: All docs go in `DOCS/` or `docs/` folder
- **Tests**: Unit tests in `AutoRevOption.Tests/` project
- **Source**: Code organized by project (Shared, Monitor, Minimal)

### Scripts
- **Format**: Use `.sh` for shell scripts (cross-platform bash)
- **Location**: `scripts/` directory only
- **Naming**: Use kebab-case (e.g., `get-positions.sh`, `start-mcp.sh`)
- **Shebang**: Always start with `#!/usr/bin/env bash`
- **Executable**: Make scripts executable with `chmod +x scripts/*.sh`

**DO NOT**:
- ❌ Create `.ps1` PowerShell scripts (Windows-only)
- ❌ Create `.csx` C# scripts in root
- ❌ Place scripts in root folder
- ❌ Use Windows-specific commands (use cross-platform alternatives)

### Background Processes
- **Always clean up**: Kill background processes before exiting
- **Check status**: Use `BashOutput` to monitor background jobs
- **Timeout**: Set appropriate timeouts for long-running commands
- **Kill on failure**: Always kill hanging processes to free resources

## C# Code Standards

### Naming Conventions
- **Classes**: PascalCase (e.g., `IbkrConnection`, `ScreenContext`)
- **Methods**: PascalCase (e.g., `ConnectAsync`, `GetPositions`)
- **Private fields**: _camelCase (e.g., `_client`, `_credentials`)
- **Parameters**: camelCase (e.g., `accountId`, `clientId`)
- **Constants**: PascalCase (e.g., `DefaultTimeout`)

### Async/Await
- **Always** use `async/await` for I/O operations
- **Never** block on async code with `.Result` or `.Wait()`
- **Always** suffix async methods with `Async` (e.g., `ConnectAsync`)
- **Use** `ConfigureAwait(false)` in library code

### Error Handling
- **Log errors** to console with `[Component] ❌ Error: message` format
- **Use** structured logging prefixes: `[IBKR]`, `[Gateway]`, `[MCP]`
- **Validate** inputs at method entry
- **Provide** actionable error messages with next steps

### Testing
- **Location**: `AutoRevOption.Tests/` project
- **Framework**: xUnit
- **Naming**: `MethodName_Scenario_ExpectedResult` or descriptive names
- **Output**: Use `ITestOutputHelper` for diagnostic output
- **Coverage**: Write tests for all critical paths

## Git Practices

### Commits
- **Format**: `Type: Brief description`
- **Types**: `Fix`, `Feature`, `Refactor`, `Docs`, `Test`, `Debug`
- **Examples**:
  - `Fix: Resolve IbkrConnection timeout on eConnect()`
  - `Feature: Add MCP server for Monitor project`
  - `Docs: Update Gateway connection troubleshooting`
  - `Debug: Add verbose logging to connection handshake`

### Branches
- **Main**: Production-ready code only
- **Feature**: `feature/description` (e.g., `feature/mcp-server`)
- **Fix**: `fix/issue-description` (e.g., `fix/ibkr-timeout`)
- **Never** commit directly to main - use PRs

### .gitignore
- **Always** ignore:
  - `secrets.json` (contains API credentials)
  - `bin/`, `obj/` (build artifacts)
  - `.vs/`, `.vscode/` (IDE settings)
  - `*.user` (user-specific settings)

## IB Gateway Integration

### Connection Management
- **ClientId**: Document which ClientIds are in use
- **Cleanup**: Always disconnect on exit
- **Timeout**: Set reasonable timeouts (10s for connection, 5s for requests)
- **Logging**: Log all connection attempts with timestamps

### API Best Practices
- **Check IsConnected()** before making requests
- **Handle callbacks** in separate methods
- **Use ManualResetEvent** for async coordination
- **Log all errors** with error codes and descriptions

### Troubleshooting
- **Gateway status**: Check if Gateway is running first
- **Port check**: Verify port 4001 is listening
- **API settings**: Document required Gateway settings
- **Logs**: Always check Gateway logs in `C:\IBKR\ibgateway\`

## MCP Protocol

### Server Implementation
- **Read from stdin**: Use `Console.OpenStandardInput()`
- **Write to stdout**: Use `Console.OpenStandardOutput()`
- **Log to stderr**: Use `Console.Error.WriteLine()`
- **Handle gracefully**: Catch all exceptions, return error responses

### Tools
- **Naming**: Use snake_case (e.g., `get_positions`, `get_account_summary`)
- **Descriptions**: Clear, concise descriptions of what tool does
- **Schema**: Always provide input schema with required fields
- **Validation**: Validate all inputs before processing

## Documentation

### Code Comments
- **When**: Complex logic, non-obvious behavior, workarounds
- **Format**: `// Brief explanation` or XML docs for public APIs
- **What not**: Don't comment obvious code

### README Files
- **Every project**: Should have a README.md
- **Include**:
  - Purpose of the project
  - Setup instructions
  - Usage examples
  - Troubleshooting section

### DOCS Folder
- **Guides**: Step-by-step setup/usage guides
- **Architecture**: System design documents
- **API**: API reference documentation
- **Troubleshooting**: Common issues and solutions

## Process Management

### Background Jobs
```bash
# Start background process
dotnet run --project Project.csproj &
PROC_ID=$!

# Always trap exit to clean up
trap "kill $PROC_ID 2>/dev/null" EXIT

# Do work...

# Clean up explicitly
kill $PROC_ID 2>/dev/null
```

### Resource Cleanup
- **Always** kill background processes before exit
- **Use** trap handlers for cleanup on script exit
- **Check** for existing processes before starting new ones
- **Log** all process starts and stops

## Anti-Patterns to Avoid

### ❌ DON'T
- Create files in root directory (use proper folders)
- Use PowerShell scripts (use bash instead)
- Leave background processes running
- Commit secrets or credentials
- Use blocking calls in async methods
- Ignore errors silently
- Write platform-specific code
- Skip cleanup in finally blocks

### ✅ DO
- Keep root folder clean
- Use scripts folder for all scripts
- Clean up background processes
- Use environment variables or secrets.json for credentials
- Use async/await properly
- Log errors with context
- Write cross-platform code
- Always clean up resources

## Example: Proper Script Structure

```bash
#!/usr/bin/env bash
# scripts/get-positions.sh - Get current positions from IBKR

set -e  # Exit on error
set -u  # Exit on undefined variable

# Change to project root
cd "$(dirname "$0")/.."

# Cleanup function
cleanup() {
    echo "Cleaning up background processes..."
    # Kill specific processes here
}

# Register cleanup on exit
trap cleanup EXIT

# Main logic
echo "Starting Monitor..."
dotnet run --project AutoRevOption.Monitor/AutoRevOption.Monitor.csproj

# Explicit cleanup (trap will also run)
cleanup
```

## Review Checklist

Before committing code, verify:

- [ ] No files in root folder (except solution/config files)
- [ ] All scripts in `scripts/` folder
- [ ] All scripts use `.sh` extension
- [ ] No background processes left running
- [ ] No secrets committed
- [ ] Code follows naming conventions
- [ ] Async methods use `Async` suffix
- [ ] Error handling is comprehensive
- [ ] Tests written for new functionality
- [ ] Documentation updated
- [ ] Commit message follows format

---

**Last Updated**: 2025-10-05
**Version**: 1.0
