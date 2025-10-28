# Task 7.3: JSON-RPC Request/Response Models

**Phase:** 7 - MCP Server Setup
**Estimated Time:** 30 minutes
**Prerequisites:** Task 7.2 complete (Dependencies installed)
**Status:** Pending

---

## Objective

Define C# models for the MCP protocol's JSON-RPC request and response structures used in stdio communication.

---

## Acceptance Criteria

- [ ] JSON-RPC request model created
- [ ] JSON-RPC response model created
- [ ] MCP tool call parameter models created
- [ ] Models support System.Text.Json serialization
- [ ] Models include proper null handling
- [ ] Project builds successfully
- [ ] Models match MCP protocol specification

---

## Background

The Model Context Protocol (MCP) uses JSON-RPC 2.0 over stdio. Requests come in on `stdin`, responses go out on `stdout`.

**Example MCP request (stdin):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "log_time",
    "arguments": {
      "projectCode": "INTERNAL",
      "task": "Development",
      "standardHours": 8.0,
      "startDate": "2025-10-28",
      "completionDate": "2025-10-28"
    }
  }
}
```

**Example MCP response (stdout):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Time entry created successfully (ID: abc-123)"
      }
    ]
  }
}
```

---

## Implementation Steps

### 1. Create JSON-RPC Request Models

Create `Models/JsonRpcRequest.cs`:

```csharp
using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// JSON-RPC 2.0 request structure
/// </summary>
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// Parameters for tools/call method
/// </summary>
public class ToolCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, object?>? Arguments { get; set; }
}

/// <summary>
/// Parameters for tools/list method
/// </summary>
public class ToolsListParams
{
    // MCP tools/list has no parameters, but we keep for future extensions
}
```

### 2. Create JSON-RPC Response Models

Create `Models/JsonRpcResponse.cs`:

```csharp
using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// JSON-RPC 2.0 response structure
/// </summary>
public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC error structure
/// </summary>
public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    public static JsonRpcError InvalidRequest(string message) =>
        new JsonRpcError { Code = -32600, Message = message };

    public static JsonRpcError MethodNotFound(string method) =>
        new JsonRpcError { Code = -32601, Message = $"Method not found: {method}" };

    public static JsonRpcError InvalidParams(string message) =>
        new JsonRpcError { Code = -32602, Message = message };

    public static JsonRpcError InternalError(string message) =>
        new JsonRpcError { Code = -32603, Message = message };
}
```

### 3. Create MCP Content Models

Create `Models/McpContent.cs`:

```csharp
using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// MCP tool result wrapper
/// </summary>
public class ToolResult
{
    [JsonPropertyName("content")]
    public List<ContentItem> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    public bool? IsError { get; set; }
}

/// <summary>
/// MCP content item (text or resource)
/// </summary>
public class ContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Create text content
    /// </summary>
    public static ContentItem Text(string text) =>
        new ContentItem { Type = "text", Text = text };

    /// <summary>
    /// Create error content
    /// </summary>
    public static ContentItem Error(string message) =>
        new ContentItem { Type = "text", Text = $"Error: {message}" };
}
```

### 4. Create Tool Definition Models

Create `Models/ToolDefinition.cs`:

```csharp
using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// MCP tool definition for tools/list response
/// </summary>
public class ToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };
}

/// <summary>
/// Response for tools/list method
/// </summary>
public class ToolsListResult
{
    [JsonPropertyName("tools")]
    public List<ToolDefinition> Tools { get; set; } = new();
}
```

### 5. Create Helper Extension Methods

Create `Utils/JsonHelper.cs`:

```csharp
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Utils;

/// <summary>
/// JSON serialization helpers for MCP protocol
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Deserialize JSON-RPC request from string
    /// </summary>
    public static JsonRpcRequest? DeserializeRequest(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonRpcRequest>(json, Options);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Failed to deserialize request: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Serialize JSON-RPC response to string
    /// </summary>
    public static string SerializeResponse(JsonRpcResponse response)
    {
        return JsonSerializer.Serialize(response, Options);
    }

    /// <summary>
    /// Parse tool call parameters from request
    /// </summary>
    public static ToolCallParams? ParseToolCallParams(object? paramsObj)
    {
        if (paramsObj == null) return null;

        try
        {
            var json = JsonSerializer.Serialize(paramsObj, Options);
            return JsonSerializer.Deserialize<ToolCallParams>(json, Options);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Failed to parse tool call params: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create success response with text content
    /// </summary>
    public static JsonRpcResponse SuccessResponse(int? requestId, string message)
    {
        return new JsonRpcResponse
        {
            Id = requestId,
            Result = new ToolResult
            {
                Content = new List<ContentItem> { ContentItem.Text(message) }
            }
        };
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static JsonRpcResponse ErrorResponse(int? requestId, JsonRpcError error)
    {
        return new JsonRpcResponse
        {
            Id = requestId,
            Error = error
        };
    }
}
```

### 6. Build and Verify

```bash
cd TimeReportingMcp

# Build project
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Testing

### Test 1: Serialization/Deserialization

Create a simple test in `Program.cs` (temporary, will be replaced in Task 7.4):

```csharp
using System;
using System.Text.Json;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Error.WriteLine("Testing JSON-RPC models...");

        // Test request deserialization
        var requestJson = @"{
            ""jsonrpc"": ""2.0"",
            ""id"": 1,
            ""method"": ""tools/call"",
            ""params"": {
                ""name"": ""log_time"",
                ""arguments"": {
                    ""projectCode"": ""INTERNAL"",
                    ""task"": ""Development"",
                    ""standardHours"": 8.0
                }
            }
        }";

        var request = JsonHelper.DeserializeRequest(requestJson);
        Console.Error.WriteLine($"Request method: {request?.Method}");

        // Test response serialization
        var response = JsonHelper.SuccessResponse(1, "Test successful");
        var responseJson = JsonHelper.SerializeResponse(response);
        Console.Error.WriteLine($"Response: {responseJson}");

        Console.Error.WriteLine("JSON-RPC models working correctly!");
    }
}
```

Run the test:

```bash
dotnet run
```

**Expected output:**
```
Testing JSON-RPC models...
Request method: tools/call
Response: {"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"Test successful"}]}}
JSON-RPC models working correctly!
```

### Test 2: Error Responses

Add to `Program.cs`:

```csharp
// Test error response
var errorResponse = JsonHelper.ErrorResponse(
    1,
    JsonRpcError.MethodNotFound("unknown_method")
);
var errorJson = JsonHelper.SerializeResponse(errorResponse);
Console.Error.WriteLine($"Error response: {errorJson}");
```

**Expected output:**
```
Error response: {"jsonrpc":"2.0","id":1,"error":{"code":-32601,"message":"Method not found: unknown_method"}}
```

---

## Verification Checklist

- [ ] All model files created in `Models/` directory
- [ ] JsonHelper created in `Utils/` directory
- [ ] Project builds with no warnings
- [ ] Request deserialization works
- [ ] Response serialization works
- [ ] Error responses serialize correctly
- [ ] Null properties are omitted from JSON
- [ ] camelCase naming is applied

---

## Related Files

**Created:**
- `Models/JsonRpcRequest.cs` - Request models
- `Models/JsonRpcResponse.cs` - Response and error models
- `Models/McpContent.cs` - Content item models
- `Models/ToolDefinition.cs` - Tool definition models
- `Utils/JsonHelper.cs` - JSON serialization helpers

**Modified:**
- `Program.cs` - Added JSON model tests (temporary)

---

## Next Steps

Proceed to [Task 7.4: Implement MCP Server](./task-7.4-mcp-server.md) to create the main server that handles stdio communication.

---

## Notes

### JSON-RPC 2.0 Error Codes

Standard error codes used:

| Code | Meaning | Usage |
|------|---------|-------|
| -32600 | Invalid Request | Malformed JSON or missing required fields |
| -32601 | Method Not Found | Unknown method name |
| -32602 | Invalid Params | Missing or invalid parameters |
| -32603 | Internal Error | Server-side error during execution |

### MCP Protocol

- **Request:** JSON-RPC request on stdin (one per line)
- **Response:** JSON-RPC response on stdout (one per line)
- **Logging:** Use stderr to avoid polluting stdout
- **Format:** One JSON object per line (no pretty-printing)

### System.Text.Json Attributes

- `[JsonPropertyName("name")]` - Maps C# property to JSON field
- `JsonIgnoreCondition.WhenWritingNull` - Omits null properties
- `JsonNamingPolicy.CamelCase` - Converts PascalCase to camelCase

**Example mapping:**
```csharp
public string ProjectCode { get; set; }  // C# property
// Serializes to: "projectCode": "INTERNAL"  // JSON field
```
