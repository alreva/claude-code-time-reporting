using System.Text.Json;

namespace TimeReportingMcpSdk.Utils;

/// <summary>
/// Formats TimeEntry data into consistent JSON responses across all MCP tools.
/// Uses the TimeEntryFields GraphQL fragment to ensure complete field coverage.
/// </summary>
public static class TimeEntryFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Format a single TimeEntry as JSON.
    /// Uses duck typing to work with any StrawberryShake generated type that implements TimeEntryFields fragment.
    /// </summary>
    /// <param name="entry">TimeEntry object from any GraphQL operation that uses ...TimeEntryFields</param>
    /// <returns>JSON string with all TimeEntry fields</returns>
    public static string FormatAsJson(dynamic entry)
    {
        // Extract tags with explicit type cast to avoid lambda/dynamic issues
        object[] tagArray = Array.Empty<object>();
        if (entry.Tags != null)
        {
            var tagsList = new List<object>();
            foreach (var t in entry.Tags)
            {
                tagsList.Add(new
                {
                    name = (string)t.TagValue.ProjectTag.TagName,
                    value = (string)t.TagValue.Value
                });
            }
            tagArray = tagsList.ToArray();
        }

        var jsonEntry = new
        {
            id = ((Guid)entry.Id).ToString(),
            projectCode = (string)entry.Project.Code,
            projectName = (string)entry.Project.Name,
            task = (string)entry.ProjectTask.TaskName,
            standardHours = (decimal)entry.StandardHours,
            overtimeHours = (decimal)entry.OvertimeHours,
            startDate = ((DateOnly)entry.StartDate).ToString("yyyy-MM-dd"),
            completionDate = ((DateOnly)entry.CompletionDate).ToString("yyyy-MM-dd"),
            status = entry.Status.ToString(),
            declineComment = (string?)entry.DeclineComment,
            description = (string?)entry.Description,
            issueId = (string?)entry.IssueId,
            userId = (string)entry.UserId,
            userEmail = (string)entry.UserEmail,
            userName = (string)entry.UserName,
            tags = tagArray,
            createdAt = ((DateTime)entry.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"),
            updatedAt = ((DateTime)entry.UpdatedAt).ToString("yyyy-MM-dd HH:mm:ss")
        };

        return JsonSerializer.Serialize(jsonEntry, JsonOptions);
    }

    /// <summary>
    /// Format multiple TimeEntry objects as JSON array.
    /// </summary>
    /// <param name="entries">Collection of TimeEntry objects</param>
    /// <returns>JSON array string</returns>
    public static string FormatAsJsonArray(IEnumerable<dynamic> entries)
    {
        var jsonEntries = new List<object>();

        foreach (var entry in entries)
        {
            // Extract tags with explicit type cast to avoid lambda/dynamic issues
            object[] tagArray = Array.Empty<object>();
            if (entry.Tags != null)
            {
                var tagsList = new List<object>();
                foreach (var t in entry.Tags)
                {
                    tagsList.Add(new
                    {
                        name = (string)t.TagValue.ProjectTag.TagName,
                        value = (string)t.TagValue.Value
                    });
                }
                tagArray = tagsList.ToArray();
            }

            jsonEntries.Add(new
            {
                id = ((Guid)entry.Id).ToString(),
                projectCode = (string)entry.Project.Code,
                projectName = (string)entry.Project.Name,
                task = (string)entry.ProjectTask.TaskName,
                standardHours = (decimal)entry.StandardHours,
                overtimeHours = (decimal)entry.OvertimeHours,
                startDate = ((DateOnly)entry.StartDate).ToString("yyyy-MM-dd"),
                completionDate = ((DateOnly)entry.CompletionDate).ToString("yyyy-MM-dd"),
                status = entry.Status.ToString(),
                declineComment = (string?)entry.DeclineComment,
                description = (string?)entry.Description,
                issueId = (string?)entry.IssueId,
                userId = (string)entry.UserId,
                userEmail = (string)entry.UserEmail,
                userName = (string)entry.UserName,
                tags = tagArray,
                createdAt = ((DateTime)entry.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"),
                updatedAt = ((DateTime)entry.UpdatedAt).ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        return JsonSerializer.Serialize(jsonEntries, JsonOptions);
    }
}
