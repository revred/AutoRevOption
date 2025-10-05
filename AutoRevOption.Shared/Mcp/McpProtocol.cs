// McpProtocol.cs — MCP (Model Context Protocol) types

using System.Text.Json;

namespace AutoRevOption.Shared.Mcp;

/// <summary>
/// MCP request message
/// </summary>
public class McpRequest
{
    public string Method { get; set; } = "";
    public McpParams? Params { get; set; }
}

/// <summary>
/// MCP request parameters
/// </summary>
public class McpParams
{
    public string? Name { get; set; }
    public JsonElement? Arguments { get; set; }
}

/// <summary>
/// MCP response message
/// </summary>
public class McpResponse
{
    public object? Result { get; set; }
    public McpError? Error { get; set; }
}

/// <summary>
/// MCP error details
/// </summary>
public class McpError
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
}
