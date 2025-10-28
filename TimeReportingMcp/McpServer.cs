using System;
using System.Text.Json;
using TimeReportingMcp.Models;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

/// <summary>
/// MCP server that handles JSON-RPC communication via stdio
/// </summary>
public class McpServer
{
    private readonly GraphQLClientWrapper _graphqlClient;
    private readonly List<ToolDefinition> _availableTools;

    public McpServer(GraphQLClientWrapper graphqlClient)
    {
        _graphqlClient = graphqlClient;
        _availableTools = InitializeToolDefinitions();
    }

    /// <summary>
    /// Start the MCP server and process requests from stdin
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Console.Error.WriteLine("MCP Server ready - listening on stdin");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Read one line from stdin (JSON-RPC request)
                var line = await Console.In.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                {
                    Console.Error.WriteLine("Received empty line, continuing...");
                    continue;
                }

                // Process request and write response to stdout
                await HandleRequestAsync(line);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error in MCP server: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Handle a single JSON-RPC request
    /// </summary>
    private async Task HandleRequestAsync(string requestJson)
    {
        JsonRpcResponse response;
        int? requestId = null;

        try
        {
            Console.Error.WriteLine($"Received request: {requestJson.Substring(0, Math.Min(100, requestJson.Length))}...");

            // Deserialize request
            var request = JsonHelper.DeserializeRequest(requestJson);
            if (request == null)
            {
                response = JsonHelper.ErrorResponse(
                    null,
                    JsonRpcError.InvalidRequest("Failed to parse JSON-RPC request")
                );
            }
            else
            {
                requestId = request.Id;
                response = await ProcessRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error handling request: {ex.Message}");
            response = JsonHelper.ErrorResponse(
                requestId,
                JsonRpcError.InternalError(ex.Message)
            );
        }

        // Write response to stdout (one line)
        var responseJson = JsonHelper.SerializeResponse(response);
        Console.WriteLine(responseJson);
        await Console.Out.FlushAsync();
    }

    /// <summary>
    /// Process a JSON-RPC request and return response
    /// </summary>
    private async Task<JsonRpcResponse> ProcessRequestAsync(JsonRpcRequest request)
    {
        return request.Method switch
        {
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolCallAsync(request),
            _ => JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.MethodNotFound(request.Method)
            )
        };
    }

    /// <summary>
    /// Handle tools/list - return list of available tools
    /// </summary>
    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        Console.Error.WriteLine("Handling tools/list");

        var result = new ToolsListResult
        {
            Tools = _availableTools
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    /// <summary>
    /// Handle tools/call - route to specific tool handler
    /// </summary>
    private async Task<JsonRpcResponse> HandleToolCallAsync(JsonRpcRequest request)
    {
        var toolParams = JsonHelper.ParseToolCallParams(request.Params);

        if (toolParams == null || string.IsNullOrEmpty(toolParams.Name))
        {
            return JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.InvalidParams("Missing tool name")
            );
        }

        Console.Error.WriteLine($"Handling tool call: {toolParams.Name}");

        try
        {
            var result = await ExecuteToolAsync(toolParams);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Tool execution failed: {ex.Message}");
            return JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.InternalError($"Tool execution failed: {ex.Message}")
            );
        }
    }

    /// <summary>
    /// Execute a specific tool by name
    /// </summary>
    private async Task<ToolResult> ExecuteToolAsync(ToolCallParams toolParams)
    {
        // Convert arguments dictionary to JsonElement for tool handlers
        var argumentsJson = JsonSerializer.Serialize(toolParams.Arguments ?? new Dictionary<string, object?>());
        var argumentsElement = JsonSerializer.Deserialize<JsonElement>(argumentsJson);

        return toolParams.Name switch
        {
            "log_time" => await new LogTimeTool(_graphqlClient).ExecuteAsync(argumentsElement),
            "query_time_entries" => await new QueryEntriesTool(_graphqlClient).ExecuteAsync(argumentsElement),
            "update_time_entry" => PlaceholderToolResult("update_time_entry"),
            "move_task_to_project" => PlaceholderToolResult("move_task_to_project"),
            "delete_time_entry" => PlaceholderToolResult("delete_time_entry"),
            "get_available_projects" => PlaceholderToolResult("get_available_projects"),
            "submit_time_entry" => PlaceholderToolResult("submit_time_entry"),
            _ => throw new InvalidOperationException($"Unknown tool: {toolParams.Name}")
        };
    }

    /// <summary>
    /// Placeholder tool result (will be replaced in Phase 8)
    /// </summary>
    private ToolResult PlaceholderToolResult(string toolName)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText($"Tool '{toolName}' not yet implemented (placeholder)")
            }
        };
    }

    /// <summary>
    /// Initialize tool definitions for tools/list
    /// </summary>
    private List<ToolDefinition> InitializeToolDefinitions()
    {
        return new List<ToolDefinition>
        {
            new()
            {
                Name = "log_time",
                Description = "Create a new time entry for tracking work hours",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectCode = new { type = "string", description = "Project code (e.g., INTERNAL)" },
                        task = new { type = "string", description = "Task name (e.g., Development)" },
                        standardHours = new { type = "number", description = "Standard hours worked" },
                        overtimeHours = new { type = "number", description = "Overtime hours (optional)" },
                        startDate = new { type = "string", description = "Start date (YYYY-MM-DD)" },
                        completionDate = new { type = "string", description = "Completion date (YYYY-MM-DD)" },
                        description = new { type = "string", description = "Work description (optional)" },
                        issueId = new { type = "string", description = "Issue/ticket ID (optional)" },
                        tags = new { type = "object", description = "Metadata tags (optional)" }
                    },
                    required = new[] { "projectCode", "task", "standardHours", "startDate", "completionDate" }
                }
            },
            new()
            {
                Name = "query_time_entries",
                Description = "Query time entries with filters",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectCode = new { type = "string", description = "Filter by project code (optional)" },
                        status = new { type = "string", description = "Filter by status (optional)" },
                        startDate = new { type = "string", description = "Filter from date (optional)" },
                        endDate = new { type = "string", description = "Filter to date (optional)" }
                    }
                }
            },
            new()
            {
                Name = "update_time_entry",
                Description = "Update an existing time entry",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Time entry ID (UUID)" },
                        standardHours = new { type = "number", description = "Standard hours (optional)" },
                        overtimeHours = new { type = "number", description = "Overtime hours (optional)" },
                        description = new { type = "string", description = "Description (optional)" }
                    },
                    required = new[] { "id" }
                }
            },
            new()
            {
                Name = "move_task_to_project",
                Description = "Move a time entry to a different project",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        entryId = new { type = "string", description = "Time entry ID (UUID)" },
                        newProjectCode = new { type = "string", description = "New project code" },
                        newTask = new { type = "string", description = "New task name" }
                    },
                    required = new[] { "entryId", "newProjectCode", "newTask" }
                }
            },
            new()
            {
                Name = "delete_time_entry",
                Description = "Delete a time entry (only if not submitted)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Time entry ID (UUID)" }
                    },
                    required = new[] { "id" }
                }
            },
            new()
            {
                Name = "get_available_projects",
                Description = "List all available projects with their tasks and tags",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        activeOnly = new { type = "boolean", description = "Only return active projects (default: true)" }
                    }
                }
            },
            new()
            {
                Name = "submit_time_entry",
                Description = "Submit a time entry for approval",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Time entry ID (UUID)" }
                    },
                    required = new[] { "id" }
                }
            }
        };
    }
}
