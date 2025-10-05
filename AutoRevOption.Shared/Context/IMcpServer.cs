// IMcpServer.cs — Common MCP server interface

namespace AutoRevOption.Shared.Context;

/// <summary>
/// Common interface for MCP servers
/// </summary>
public interface IMcpServer
{
    /// <summary>
    /// Server name (e.g., "AutoRevOption", "AutoRevOption-Monitor")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Server version (e.g., "1.0.0")
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Handle an MCP request and return a response
    /// </summary>
    Task<McpResponse> HandleRequest(McpRequest request);
}
