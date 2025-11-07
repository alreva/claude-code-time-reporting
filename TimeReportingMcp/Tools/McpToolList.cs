using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

public static class McpToolListDi
{
    public static IServiceCollection RegisterToolDefinitions(this IServiceCollection services) =>
        services
            .AddSingleton<HelloTool>()
            .AddSingleton<LogTimeTool>()
            .AddSingleton<QueryEntriesTool>()
            .AddSingleton<UpdateEntryTool>()
            .AddSingleton<MoveTaskTool>()
            .AddSingleton<DeleteEntryTool>()
            .AddSingleton<GetProjectsTool>()
            .AddSingleton<SubmitEntryTool>()
            .AddSingleton<ApproveEntryTool>()
            .AddSingleton<DeclineEntryTool>()
            .AddSingleton<McpToolList>();
}

public class McpToolList
{
        private readonly List<(IMcpTool Tool, ToolDefinition Definitiion)> _toolsRepo;

        private readonly List<ToolDefinition> _toolDefinitions;
        private readonly Dictionary<string, IMcpTool> _toolsByName;

        public McpToolList(
            HelloTool helloTool,
            LogTimeTool  logTimeTool,
            QueryEntriesTool  queryEntriesTool,
            UpdateEntryTool  updateEntryTool,
            MoveTaskTool  moveTaskTool,
            DeleteEntryTool   deleteEntryTool,
            GetProjectsTool  getProjectsTool,
            SubmitEntryTool submitEntryTool,
            ApproveEntryTool approveEntryTool,
            DeclineEntryTool declineEntryTool)
        {
            _toolsRepo = [
                (helloTool, new()
                {
                    Name = "hello",
                    Description = "Test connectivity to the GraphQL API. Calls the { hello } query and returns its response.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        additionalProperties = false
                    }
                }),
                (logTimeTool, new()
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
                }),
                (queryEntriesTool, new()
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
                            endDate = new { type = "string", description = "Filter to date (optional)" },
                            userEmail = new { type = "string", description = "Filter by user email (optional)" }
                        }
                    }
                }),
                (updateEntryTool, new()
                {
                    Name = "update_time_entry",
                    Description = "Update an existing time entry",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "Time entry ID (UUID)" },
                            task = new { type = "string", description = "Task name (optional)" },
                            issueId = new { type = "string", description = "Issue/ticket ID (optional)" },
                            standardHours = new { type = "number", description = "Standard hours (optional)" },
                            overtimeHours = new { type = "number", description = "Overtime hours (optional)" },
                            description = new { type = "string", description = "Description (optional)" },
                            startDate = new { type = "string", description = "Start date (YYYY-MM-DD) (optional)" },
                            completionDate = new
                                { type = "string", description = "Completion date (YYYY-MM-DD) (optional)" },
                            tags = new { type = "object", description = "Metadata tags (optional)" }
                        },
                        required = new[] { "id" }
                    }
                }),
                (moveTaskTool, new()
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
                }),
                (deleteEntryTool, new()
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
                }),
                (getProjectsTool, new()
                {
                    Name = "get_available_projects",
                    Description = "List all available projects with their tasks and tags",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            activeOnly = new
                                { type = "boolean", description = "Only return active projects (default: true)" }
                        }
                    }
                }),
                (submitEntryTool, new()
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
                }),
                (approveEntryTool, new()
                {
                    Name = "approve_time_entry",
                    Description = "Approve a submitted time entry",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "Time entry ID (UUID)" }
                        },
                        required = new[] { "id" }
                    }
                }),
                (declineEntryTool, new()
                {
                    Name = "decline_time_entry",
                    Description = "Decline a submitted time entry with a reason",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "Time entry ID (UUID)" },
                            comment = new { type = "string", description = "Reason for declining" }
                        },
                        required = new[] { "id", "comment" }
                    }
                })
            ];

            _toolDefinitions = [.. _toolsRepo.Select(x => x.Definitiion)];
            _toolsByName = _toolsRepo.ToDictionary(x => x.Definitiion.Name, x => x.Tool);
        }

        public List<ToolDefinition> GetToolDescriptions()
        {
            return _toolDefinitions;
        }

        public Task<ToolResult> CallTool(string toolName, JsonElement payload)
        {
            return !_toolsByName.TryGetValue(toolName, out var tool)
                ? throw new InvalidOperationException($"Unknown tool: {toolName}")
                : tool.ExecuteAsync(payload);
        }
}