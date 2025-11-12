using System.Text.Json;

namespace TimeReportingMcpSdk.AutoTracking;

/// <summary>
/// Handles persistence of session context to survive MCP server restarts.
/// Saves context to JSON file in user's config directory.
/// </summary>
public class ContextPersistence
{
    private readonly string _persistenceFilePath;
    private readonly int _maxStaleMinutes;

    public ContextPersistence(
        string? customPath = null,
        int maxStaleMinutes = 60)
    {
        _maxStaleMinutes = maxStaleMinutes;

        // Default to user's home directory/.time-reporting/context.json
        _persistenceFilePath = customPath ?? GetDefaultPersistencePath();

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_persistenceFilePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Save session context to persistent storage
    /// </summary>
    public async Task SaveContextAsync(SessionContext context)
    {
        try
        {
            var persistedData = new PersistedContext
            {
                LastProjectCode = context.LastProjectCode,
                LastTask = context.LastTask,
                LastEntryCreatedAt = context.LastEntryCreatedAt,
                LastActivityAt = context.LastActivityAt,
                SessionStartedAt = context.SessionStartedAt,
                LastEntryId = context.LastEntryId,
                SavedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(persistedData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_persistenceFilePath, json);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[Persistence] Failed to save context: {ex.Message}");
            // Don't throw - persistence failures shouldn't break the app
        }
    }

    /// <summary>
    /// Load session context from persistent storage
    /// Returns null if no valid context found or context is stale
    /// </summary>
    public async Task<SessionContext?> LoadContextAsync()
    {
        try
        {
            if (!File.Exists(_persistenceFilePath))
            {
                return null; // No saved context
            }

            var json = await File.ReadAllTextAsync(_persistenceFilePath);
            var persistedData = JsonSerializer.Deserialize<PersistedContext>(json);

            if (persistedData == null)
            {
                return null; // Invalid JSON
            }

            // Check if context is stale (based on last activity time, not save time)
            var ageMinutes = (DateTime.UtcNow - persistedData.LastActivityAt).TotalMinutes;
            if (ageMinutes > _maxStaleMinutes)
            {
                await Console.Error.WriteLineAsync($"[Persistence] Context is stale ({ageMinutes:F0} min old), ignoring");
                return null;
            }

            // Restore context
            var context = new SessionContext
            {
                LastProjectCode = persistedData.LastProjectCode,
                LastTask = persistedData.LastTask,
                LastEntryCreatedAt = persistedData.LastEntryCreatedAt,
                LastActivityAt = persistedData.LastActivityAt,
                SessionStartedAt = persistedData.SessionStartedAt,
                LastEntryId = persistedData.LastEntryId,
                ToolCallCount = 0, // Reset - new session
                SuggestionShownForCurrentSession = false // Reset
            };

            await Console.Error.WriteLineAsync($"[Persistence] Loaded context: {persistedData.LastProjectCode}/{persistedData.LastTask}");
            return context;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[Persistence] Failed to load context: {ex.Message}");
            return null; // Don't throw - just start fresh
        }
    }

    /// <summary>
    /// Clear persisted context (delete file)
    /// </summary>
    public void ClearContext()
    {
        try
        {
            if (File.Exists(_persistenceFilePath))
            {
                File.Delete(_persistenceFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Persistence] Failed to clear context: {ex.Message}");
        }
    }

    /// <summary>
    /// Get default persistence file path
    /// </summary>
    private static string GetDefaultPersistencePath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".time-reporting");
        return Path.Combine(configDir, "mcp-context.json");
    }
}

/// <summary>
/// Serializable context data for persistence
/// </summary>
internal class PersistedContext
{
    public string? LastProjectCode { get; set; }
    public string? LastTask { get; set; }
    public DateTime? LastEntryCreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? SessionStartedAt { get; set; }
    public Guid? LastEntryId { get; set; }
    public DateTime SavedAt { get; set; }
}
