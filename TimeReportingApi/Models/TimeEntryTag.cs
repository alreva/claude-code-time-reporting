namespace TimeReportingApi.Models;

/// <summary>
/// Represents the association between a time entry and a specific tag value.
/// This is a join/bridge entity in the many-to-many relationship between TimeEntries and TagValues.
/// </summary>
/// <remarks>
/// <para><strong>Business Purpose:</strong></para>
/// <para>TimeEntryTag associates metadata tags with time entries, allowing each time entry
/// to have multiple tags from its project's configured tag system. This enables flexible
/// categorization and filtering of time entries beyond the core fields.</para>
///
/// <para><strong>Tag System Flow:</strong></para>
/// <list type="number">
/// <item><description>Project defines available ProjectTags (e.g., "Environment", "Priority")</description></item>
/// <item><description>Each ProjectTag has multiple TagValues (e.g., "Production", "High")</description></item>
/// <item><description>TimeEntryTag links a TimeEntry to a specific TagValue</description></item>
/// <item><description>Through TagValue, we can navigate to ProjectTag to get the tag name</description></item>
/// </list>
///
/// <para><strong>Example Data Structure:</strong></para>
/// <code>
/// TimeEntry: "8 hours of development work"
/// ├── TimeEntryTag #1
/// │   └── TagValue: "Production"
/// │       └── ProjectTag: "Environment"
/// └── TimeEntryTag #2
///     └── TagValue: "High"
///         └── ProjectTag: "Priority"
///
/// // Resulting tags displayed to user:
/// // Environment: Production
/// // Priority: High
/// </code>
///
/// <para><strong>Key Characteristics:</strong></para>
/// <list type="bullet">
/// <item><description>Many-to-many bridge entity between TimeEntry and TagValue</description></item>
/// <item><description>Each TimeEntryTag represents one tag on one time entry</description></item>
/// <item><description>A time entry can have multiple TimeEntryTags (multiple metadata tags)</description></item>
/// <item><description>Tag value must come from the project's allowed values</description></item>
/// <item><description>Unique constraint: one time entry cannot have same TagValue twice</description></item>
/// </list>
///
/// <para><strong>Database Mapping:</strong></para>
/// <list type="bullet">
/// <item><description>Table: time_entry_tags</description></item>
/// <item><description>Primary Key: Id (auto-increment)</description></item>
/// <item><description>Foreign Keys: TimeEntryId (shadow), TagValueId (shadow)</description></item>
/// <item><description>Unique Constraint: (TimeEntryId, TagValueId) via uq_time_entry_tags_entry_value</description></item>
/// <item><description>Index: TimeEntryId via idx_time_entry_tags_entry</description></item>
/// </list>
///
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
/// <item><description>Many-to-One with TimeEntry (many tags belong to one entry)</description></item>
/// <item><description>Many-to-One with TagValue (many entries can use same value)</description></item>
/// </list>
///
/// <para><strong>Validation Rules:</strong></para>
/// <list type="bullet">
/// <item><description>TagValue must exist and belong to the time entry's project</description></item>
/// <item><description>TagValue's ProjectTag must be active</description></item>
/// <item><description>Cannot duplicate same TagValue on a single TimeEntry</description></item>
/// <item><description>Tag name (via TagValue → ProjectTag) should only appear once per TimeEntry</description></item>
/// </list>
///
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Get project with tag configuration
/// var project = await dbContext.Projects
///     .Include(p => p.Tags)
///         .ThenInclude(t => t.AllowedValues)
///     .FirstAsync(p => p.Code == "WEB-2025");
///
/// // Find specific tag values
/// var environmentTag = project.Tags.First(t => t.TagName == "Environment");
/// var prodValue = environmentTag.AllowedValues.First(v => v.Value == "Production");
///
/// var priorityTag = project.Tags.First(t => t.TagName == "Priority");
/// var highValue = priorityTag.AllowedValues.First(v => v.Value == "High");
///
/// // Create time entry with tags
/// TimeEntry entry = new()
/// {
///     Project = project,
///     // ... other required fields
///     Tags = new()
///     {
///         new TimeEntryTag { TagValue = prodValue },   // Environment: Production
///         new TimeEntryTag { TagValue = highValue }    // Priority: High
///     }
/// };
///
/// await dbContext.TimeEntries.AddAsync(entry);
/// await dbContext.SaveChangesAsync();
///
/// // Query entries by tag
/// var productionEntries = await dbContext.TimeEntries
///     .Include(e => e.Tags)
///         .ThenInclude(t => t.TagValue)
///             .ThenInclude(v => v.ProjectTag)
///     .Where(e => e.Tags.Any(t =>
///         t.TagValue.ProjectTag.TagName == "Environment" &&
///         t.TagValue.Value == "Production"))
///     .ToListAsync();
///
/// // Display tags for a time entry
/// foreach (var tag in entry.Tags)
/// {
///     var tagName = tag.TagValue.ProjectTag.TagName;
///     var tagValue = tag.TagValue.Value;
///     Console.WriteLine($"{tagName}: {tagValue}");
/// }
/// // Output:
/// // Environment: Production
/// // Priority: High
/// </code>
///
/// <para><strong>Design Rationale:</strong></para>
/// <para>TimeEntryTag uses a normalized many-to-many pattern (rather than JSONB) because:</para>
/// <list type="bullet">
/// <item><description>Enforces referential integrity - tags must be valid for the project</description></item>
/// <item><description>Enables efficient queries and filtering by tag values</description></item>
/// <item><description>Provides type safety and compile-time validation</description></item>
/// <item><description>Supports indexing for performance on tag-based searches</description></item>
/// <item><description>Maintains data consistency across the application</description></item>
/// </list>
/// <para>See ADR 0004 (Normalized Schema) and ADR 0005 (Relational over JSONB) for architectural decisions.</para>
///
/// <para><strong>Delete Behavior:</strong></para>
/// <list type="bullet">
/// <item><description>Cascade when TimeEntry is deleted (tags deleted with entry)</description></item>
/// <item><description>Restrict when TagValue is deleted (prevent deleting values in use)</description></item>
/// </list>
///
/// <para><strong>GraphQL Representation:</strong></para>
/// <para>In the GraphQL API, TimeEntryTag is flattened to a simpler structure:</para>
/// <code>
/// type Tag {
///   name: String!   # From TagValue.ProjectTag.TagName
///   value: String!  # From TagValue.Value
/// }
/// </code>
/// <para>This provides a user-friendly API while maintaining normalized storage internally.</para>
///
/// <para><strong>Related Entities:</strong></para>
/// <para>See <see cref="TimeEntry"/>, <see cref="TagValue"/>, <see cref="ProjectTag"/>, <see cref="Project"/>.</para>
/// </remarks>
public class TimeEntryTag
{
    /// <summary>
    /// Unique identifier for this time entry tag association.
    /// Auto-generated database identity column.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> id (integer, PRIMARY KEY, AUTO INCREMENT)</para>
    /// <para><strong>Generation:</strong> Automatically assigned by database on insert</para>
    /// <para><strong>Purpose:</strong> Provides unique identification for each tag association,
    /// though the combination of (TimeEntryId, TagValueId) is also unique via constraint.</para>
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Navigation property to the time entry that this tag is associated with.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> Many-to-One (many TimeEntryTags belong to one TimeEntry)</para>
    /// <para><strong>Foreign Key:</strong> Shadow property "TimeEntryId" managed by EF Core</para>
    /// <para><strong>Access Pattern:</strong></para>
    /// <code>
    /// // Load tag with time entry
    /// var tag = await dbContext.Set&lt;TimeEntryTag&gt;()
    ///     .Include(t => t.TimeEntry)
    ///     .FirstOrDefaultAsync(t => t.Id == tagId);
    ///
    /// // Get time entry ID via shadow property
    /// var timeEntryId = dbContext.Entry(tag)
    ///     .Property&lt;Guid&gt;("TimeEntryId")
    ///     .CurrentValue;
    /// </code>
    /// <para><strong>Delete Behavior:</strong> Cascade (tag deleted when time entry is deleted)</para>
    /// <para><strong>Required:</strong> null! indicates this is required but lazily loaded by EF Core</para>
    /// <para><strong>Index:</strong> Indexed via idx_time_entry_tags_entry for query performance</para>
    /// <para>See <see cref="TimeEntry"/> for time entry details.</para>
    /// </remarks>
    public TimeEntry TimeEntry { get; set; } = null!;

    /// <summary>
    /// Navigation property to the specific tag value that is applied to the time entry.
    /// Navigate through TagValue.ProjectTag to get the tag name.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> Many-to-One (many TimeEntryTags can use one TagValue)</para>
    /// <para><strong>Foreign Key:</strong> Shadow property "TagValueId" managed by EF Core</para>
    /// <para><strong>Navigation Path:</strong></para>
    /// <para>To get complete tag information, navigate: TimeEntryTag → TagValue → ProjectTag</para>
    /// <code>
    /// // Load tag with full tag information
    /// var entryTag = await dbContext.Set&lt;TimeEntryTag&gt;()
    ///     .Include(t => t.TagValue)
    ///         .ThenInclude(v => v.ProjectTag)
    ///     .FirstOrDefaultAsync(t => t.Id == tagId);
    ///
    /// // Extract tag name and value
    /// string tagName = entryTag.TagValue.ProjectTag.TagName;   // e.g., "Environment"
    /// string tagValue = entryTag.TagValue.Value;               // e.g., "Production"
    ///
    /// // Full display: "Environment: Production"
    /// Console.WriteLine($"{tagName}: {tagValue}");
    ///
    /// // Get tag value ID via shadow property
    /// var tagValueId = dbContext.Entry(entryTag)
    ///     .Property&lt;int&gt;("TagValueId")
    ///     .CurrentValue;
    /// </code>
    /// <para><strong>Validation:</strong></para>
    /// <list type="bullet">
    /// <item><description>TagValue must exist in the database</description></item>
    /// <item><description>TagValue's ProjectTag must belong to same project as TimeEntry</description></item>
    /// <item><description>TagValue's ProjectTag must be active</description></item>
    /// <item><description>Cannot duplicate same TagValue on a TimeEntry (unique constraint)</description></item>
    /// </list>
    /// <para><strong>Delete Behavior:</strong> Restrict (cannot delete TagValue if in use by TimeEntryTags)</para>
    /// <para><strong>Required:</strong> null! indicates this is required but lazily loaded by EF Core</para>
    /// <para><strong>Unique Constraint:</strong> (TimeEntryId, TagValueId) must be unique via
    /// uq_time_entry_tags_entry_value constraint.</para>
    /// <para>See <see cref="TagValue"/> and <see cref="ProjectTag"/> for tag system architecture.</para>
    /// </remarks>
    public TagValue TagValue { get; set; } = null!;
}
