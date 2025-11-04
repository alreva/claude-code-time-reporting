using System.Text.Json;
using StreamJsonRpc;
using TimeReportingMcp.WebSocket.Generated;
using TimeReportingMcp.WebSocket.Services;

namespace TimeReportingMcp.WebSocket;

/// <summary>
/// MCP (Model Context Protocol) server implementation using StreamJsonRpc.
/// Handles JSON-RPC 2.0 messages over WebSocket connections.
/// </summary>
/// <remarks>
/// This server exposes MCP tools for time tracking via JSON-RPC methods.
/// StreamJsonRpc automatically maps method names to JSON-RPC method calls.
///
/// MCP Protocol Flow:
/// 1. Client connects via WebSocket
/// 2. Client sends "initialize" request
/// 3. Client calls "tools/list" to discover available tools
/// 4. Client calls "tools/call" to execute specific tools
///
/// Authentication: Tokens are acquired via TokenService (Azure CLI)
/// and passed to the GraphQL API for each request.
/// </remarks>
public class McpServer
{
    private readonly ITimeReportingClient _client;
    private readonly ILogger<McpServer> _logger;

    public McpServer(
        ITimeReportingClient client,
        ILogger<McpServer> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// MCP initialize method. Called when client first connects.
    /// Returns server capabilities and version information.
    /// </summary>
    [JsonRpcMethod("initialize")]
    public object Initialize(object? clientInfo)
    {
        _logger.LogInformation("MCP client initializing: {ClientInfo}", clientInfo);

        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { }  // We support tools capability
            },
            serverInfo = new
            {
                name = "time-reporting-mcp-websocket",
                version = "2.0.0"
            }
        };
    }

    /// <summary>
    /// MCP tools/list method. Returns list of available tools.
    /// </summary>
    [JsonRpcMethod("tools/list")]
    public object ListTools()
    {
        _logger.LogDebug("Client requested tools list");

        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "log_time",
                    description = "Log time spent on a project task",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            projectCode = new { type = "string", description = "Project code (e.g., INTERNAL)" },
                            task = new { type = "string", description = "Task name (e.g., Development)" },
                            standardHours = new { type = "number", description = "Regular hours worked" },
                            overtimeHours = new { type = "number", description = "Overtime hours (optional)" },
                            startDate = new { type = "string", format = "date", description = "Start date (YYYY-MM-DD)" },
                            completionDate = new { type = "string", format = "date", description = "Completion date (YYYY-MM-DD)" },
                            description = new { type = "string", description = "Work description (optional)" },
                            issueId = new { type = "string", description = "Issue/ticket ID (optional)" }
                        },
                        required = new[] { "projectCode", "task", "standardHours", "startDate", "completionDate" }
                    }
                },
                new
                {
                    name = "get_available_projects",
                    description = "Get list of available projects and their tasks",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            activeOnly = new { type = "boolean", description = "Filter to active projects only (default: true)" }
                        }
                    }
                }
                // TODO: Add remaining 5 tools (query_time_entries, update_time_entry, etc.)
            }
        };
    }

    /// <summary>
    /// MCP tools/call method. Executes a specific tool.
    /// </summary>
    [JsonRpcMethod("tools/call")]
    public async Task<object> CallTool(JsonElement toolParams)
    {
        _logger.LogInformation("Tool call received. ValueKind: {Kind}, Raw: {Params}", toolParams.ValueKind, toolParams.GetRawText());

        try
        {
            // Parse tool name and arguments
            if (toolParams.ValueKind != JsonValueKind.Object)
            {
                return CreateErrorResponse($"Expected object, got {toolParams.ValueKind}");
            }

            if (!toolParams.TryGetProperty("name", out var nameElement))
            {
                return CreateErrorResponse("Missing 'name' property in tool call");
            }

            var toolName = nameElement.GetString();
            if (string.IsNullOrEmpty(toolName))
            {
                return CreateErrorResponse("Tool name cannot be empty");
            }

            if (!toolParams.TryGetProperty("arguments", out var argsElement))
            {
                return CreateErrorResponse("Missing 'arguments' property in tool call");
            }

            _logger.LogInformation("Executing tool: {ToolName}", toolName);

            // Execute tool based on name using StrawberryShake generated client
            return toolName switch
            {
                "log_time" => await ExecuteLogTime(argsElement),
                "get_available_projects" => await ExecuteGetAvailableProjects(argsElement),
                "query_time_entries" => await ExecuteQueryTimeEntries(argsElement),
                "update_time_entry" => await ExecuteUpdateTimeEntry(argsElement),
                "submit_time_entry" => await ExecuteSubmitTimeEntry(argsElement),
                "move_task_to_project" => await ExecuteMoveTaskToProject(argsElement),
                "delete_time_entry" => await ExecuteDeleteTimeEntry(argsElement),
                _ => CreateErrorResponse($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool");
            return CreateErrorResponse($"Tool execution failed: {ex.Message}");
        }
    }

    private object CreateErrorResponse(string errorMessage)
    {
        _logger.LogError("Tool execution error: {Error}", errorMessage);
        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = $"❌ Error: {errorMessage}"
                }
            },
            isError = true
        };
    }

    private async Task<object> ExecuteLogTime(JsonElement args)
    {
        // Parse arguments into strongly-typed input
        var projectCode = args.GetProperty("projectCode").GetString()!;
        var task = args.GetProperty("task").GetString()!;
        var standardHours = (decimal)args.GetProperty("standardHours").GetDouble();
        var startDate = DateOnly.Parse(args.GetProperty("startDate").GetString()!);
        var completionDate = DateOnly.Parse(args.GetProperty("completionDate").GetString()!);

        decimal? overtimeHours = args.TryGetProperty("overtimeHours", out var ot) ? (decimal)ot.GetDouble() : null;
        var description = args.TryGetProperty("description", out var desc) ? desc.GetString() : null;
        var issueId = args.TryGetProperty("issueId", out var issue) ? issue.GetString() : null;

        // Parse tags if provided
        List<TagInput>? tags = null;
        if (args.TryGetProperty("tags", out var tagsElement))
        {
            tags = JsonSerializer.Deserialize<List<TagInput>>(tagsElement.GetRawText());
        }

        var input = new LogTimeInput
        {
            ProjectCode = projectCode,
            Task = task,
            StandardHours = standardHours,
            OvertimeHours = overtimeHours,
            StartDate = startDate,
            CompletionDate = completionDate,
            Description = description,
            IssueId = issueId,
            Tags = tags
        };

        // Execute strongly-typed mutation
        var result = await _client.LogTime.ExecuteAsync(input);

        // Handle errors
        if (result.Errors is { Count: > 0 })
        {
            var errorMessage = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            return CreateErrorResponse($"GraphQL error:\n{errorMessage}");
        }

        // Format success response
        var entry = result.Data!.LogTime;
        var message = $"✅ Time entry logged successfully!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                      $"Task: {entry.ProjectTask.TaskName}\n" +
                      $"Hours: {entry.StandardHours} standard";

        if (entry.OvertimeHours > 0)
        {
            message += $", {entry.OvertimeHours} overtime";
        }

        message += $"\nPeriod: {entry.StartDate} to {entry.CompletionDate}\n" +
                   $"Status: {entry.Status}";

        if (!string.IsNullOrEmpty(entry.Description))
        {
            message += $"\nDescription: {entry.Description}";
        }

        if (!string.IsNullOrEmpty(entry.IssueId))
        {
            message += $"\nIssue: {entry.IssueId}";
        }

        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = message
                }
            }
        };
    }

    private async Task<object> ExecuteGetAvailableProjects(JsonElement args)
    {
        var activeOnly = args.TryGetProperty("activeOnly", out var active) ? active.GetBoolean() : true;

        // Execute strongly-typed query
        var result = await _client.GetAvailableProjects.ExecuteAsync(activeOnly);

        // Handle errors
        if (result.Errors is { Count: > 0 })
        {
            var errorMessage = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            return CreateErrorResponse($"GraphQL error:\n{errorMessage}");
        }

        // Format projects
        var projects = result.Data!.Projects.ToList();
        if (projects.Count == 0)
        {
            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = "No projects found."
                    }
                }
            };
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Available Projects ({projects.Count}):\n");

        foreach (var project in projects)
        {
            sb.AppendLine($"📊 {project.Code} - {project.Name}");
            sb.AppendLine($"   Status: {(project.IsActive ? "Active" : "Inactive")}");

            // Tasks
            var activeTasks = project.AvailableTasks.Where(t => t.IsActive).ToList();
            if (activeTasks.Any())
            {
                sb.AppendLine($"   Tasks: {string.Join(", ", activeTasks.Select(t => t.TaskName))}");
            }
            else
            {
                sb.AppendLine("   Tasks: None");
            }

            // Tags
            var activeTags = project.Tags.Where(t => t.IsActive).ToList();
            if (activeTags.Any())
            {
                sb.AppendLine("   Tags:");
                foreach (var tag in activeTags)
                {
                    var values = string.Join(", ", tag.AllowedValues.Select(v => v.Value));
                    sb.AppendLine($"     • {tag.TagName}: {values}");
                }
            }

            sb.AppendLine();
        }

        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = sb.ToString()
                }
            }
        };
    }

    private Task<object> ExecuteQueryTimeEntries(JsonElement args)
    {
        // TODO: Implement query_time_entries tool using StrawberryShake
        return Task.FromResult(CreateErrorResponse("query_time_entries tool not yet implemented"));
    }

    private Task<object> ExecuteUpdateTimeEntry(JsonElement args)
    {
        // TODO: Implement update_time_entry tool using StrawberryShake
        return Task.FromResult(CreateErrorResponse("update_time_entry tool not yet implemented"));
    }

    private Task<object> ExecuteSubmitTimeEntry(JsonElement args)
    {
        // TODO: Implement submit_time_entry tool using StrawberryShake
        return Task.FromResult(CreateErrorResponse("submit_time_entry tool not yet implemented"));
    }

    private Task<object> ExecuteMoveTaskToProject(JsonElement args)
    {
        // TODO: Implement move_task_to_project tool using StrawberryShake
        return Task.FromResult(CreateErrorResponse("move_task_to_project tool not yet implemented"));
    }

    private Task<object> ExecuteDeleteTimeEntry(JsonElement args)
    {
        // TODO: Implement delete_time_entry tool using StrawberryShake
        return Task.FromResult(CreateErrorResponse("delete_time_entry tool not yet implemented"));
    }
}
