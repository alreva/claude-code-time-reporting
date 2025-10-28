namespace TimeReportingApi.Models;

/// <summary>
/// Represents a single allowed value within a project tag's controlled vocabulary.
/// Part of the tag system that provides structured metadata for time entries.
/// </summary>
/// <remarks>
/// <para><strong>Business Purpose:</strong></para>
/// <para>TagValue defines one valid option within a tag category. For example, if "Environment"
/// is a ProjectTag, then "Production", "Staging", and "Development" would each be TagValues.
/// This forms a controlled vocabulary system that ensures data consistency and enables
/// structured filtering and reporting.</para>
///
/// <para><strong>Tag System Hierarchy:</strong></para>
/// <list type="number">
/// <item><description><strong>Project:</strong> "WEB-2025" (defines available tags)</description></item>
/// <item><description><strong>ProjectTag:</strong> "Environment" (defines tag category)</description></item>
/// <item><description><strong>TagValue:</strong> "Production", "Staging", "Development" (allowed values)</description></item>
/// <item><description><strong>TimeEntryTag:</strong> Associates a TagValue with a TimeEntry</description></item>
/// </list>
///
/// <para><strong>Key Characteristics:</strong></para>
/// <list type="bullet">
/// <item><description>Values belong to a specific ProjectTag</description></item>
/// <item><description>Values should be unique within their tag (enforced at validation layer)</description></item>
/// <item><description>Values form a closed set (only listed values are allowed)</description></item>
/// <item><description>Values are immutable once referenced by time entries</description></item>
/// </list>
///
/// <para><strong>Common Value Examples by Tag:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Environment:</strong> "Production", "Staging", "Development", "QA", "Local"</description></item>
/// <item><description><strong>Priority:</strong> "Critical", "High", "Medium", "Low"</description></item>
/// <item><description><strong>Sprint:</strong> "Sprint-1", "Sprint-2", "Alpha", "Beta"</description></item>
/// <item><description><strong>Billable:</strong> "Yes", "No", "Negotiable"</description></item>
/// <item><description><strong>WorkType:</strong> "Remote", "Onsite", "Hybrid"</description></item>
/// <item><description><strong>Phase:</strong> "Planning", "Development", "Testing", "Deployment", "Maintenance"</description></item>
/// </list>
///
/// <para><strong>Database Mapping:</strong></para>
/// <list type="bullet">
/// <item><description>Table: tag_values</description></item>
/// <item><description>Primary Key: Id (auto-increment)</description></item>
/// <item><description>Foreign Key: ProjectTagId (shadow property, references project_tags.id)</description></item>
/// <item><description>Index: ProjectTagId via idx_tag_values_project_tag</description></item>
/// </list>
///
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
/// <item><description>Many-to-One with ProjectTag (many values belong to one tag)</description></item>
/// <item><description>One-to-Many with TimeEntryTag (many time entries can use this value)</description></item>
/// </list>
///
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Define Environment tag with values
/// ProjectTag environmentTag = new()
/// {
///     TagName = "Environment",
///     AllowedValues = new()
///     {
///         new TagValue { Value = "Production" },
///         new TagValue { Value = "Staging" },
///         new TagValue { Value = "Development" },
///         new TagValue { Value = "QA" }
///     }
/// };
///
/// // Use value in time entry
/// var prodValue = environmentTag.AllowedValues
///     .First(v => v.Value == "Production");
///
/// TimeEntry entry = new()
/// {
///     // ... other properties
///     Tags = new()
///     {
///         new TimeEntryTag { TagValue = prodValue }
///     }
/// };
///
/// // Query entries by tag value
/// var productionEntries = dbContext.TimeEntries
///     .Where(e => e.Tags.Any(t => t.TagValue.Value == "Production"))
///     .ToList();
/// </code>
///
/// <para><strong>Validation Rules:</strong></para>
/// <list type="bullet">
/// <item><description>Value is required (max 100 characters)</description></item>
/// <item><description>Value should be unique within the same ProjectTag</description></item>
/// <item><description>Value should be meaningful and user-friendly</description></item>
/// <item><description>Time entries can only use values from their project's tags</description></item>
/// </list>
///
/// <para><strong>Design Rationale:</strong></para>
/// <para>Storing tag values in a normalized table (instead of JSONB arrays) provides:</para>
/// <list type="bullet">
/// <item><description>Referential integrity via foreign keys</description></item>
/// <item><description>Efficient querying and indexing</description></item>
/// <item><description>Type safety and compile-time checks</description></item>
/// <item><description>Better performance for tag-based filtering</description></item>
/// </list>
/// <para>See ADR 0004 and ADR 0005 for architectural decisions on normalized schema.</para>
///
/// <para><strong>Related Entities:</strong></para>
/// <para>See <see cref="ProjectTag"/>, <see cref="TimeEntryTag"/>, <see cref="Project"/>.</para>
/// </remarks>
public class TagValue
{
    /// <summary>
    /// Unique identifier for the tag value.
    /// Auto-generated database identity column.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> id (integer, PRIMARY KEY, AUTO INCREMENT)</para>
    /// <para><strong>Generation:</strong> Automatically assigned by database on insert</para>
    /// <para><strong>Usage:</strong> Used as foreign key in time_entry_tags.tag_value_id</para>
    /// <para><strong>Note:</strong> ID is used for relationships rather than the value string
    /// to support future value changes without breaking existing time entry references.</para>
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// The actual value string for this tag option.
    /// This is what users see and select when tagging time entries.
    /// </summary>
    /// <remarks>
    /// <para><strong>Format Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Max 100 characters</description></item>
    /// <item><description>Should be clear and unambiguous</description></item>
    /// <item><description>Recommended: Use consistent casing and formatting</description></item>
    /// <item><description>Consider how it will appear in dropdowns and reports</description></item>
    /// </list>
    /// <para><strong>Naming Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item><description>Use Title Case for multi-word values: "Code Review" not "code review"</description></item>
    /// <item><description>Be concise but descriptive</description></item>
    /// <item><description>Avoid abbreviations unless universally understood</description></item>
    /// <item><description>Use consistent terminology across projects</description></item>
    /// </list>
    /// <para><strong>Good Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description>"Production" (clear, professional)</description></item>
    /// <item><description>"High Priority" (descriptive)</description></item>
    /// <item><description>"Sprint 1 - Alpha Release" (informative)</description></item>
    /// <item><description>"Yes" (simple, unambiguous)</description></item>
    /// </list>
    /// <para><strong>Examples to Avoid:</strong></para>
    /// <list type="bullet">
    /// <item><description>"prod" (abbreviation, unclear)</description></item>
    /// <item><description>"PRODUCTION!!!" (excessive emphasis)</description></item>
    /// <item><description>"Maybe/Sometimes" (ambiguous)</description></item>
    /// </list>
    /// <para><strong>Validation:</strong></para>
    /// <list type="bullet">
    /// <item><description>Required field (cannot be null or empty)</description></item>
    /// <item><description>Max length 100 characters</description></item>
    /// <item><description>Should be unique within the same ProjectTag</description></item>
    /// <item><description>Should not contain leading/trailing whitespace</description></item>
    /// </list>
    /// <para><strong>Immutability:</strong></para>
    /// <para>Once a TagValue is referenced by TimeEntryTags, consider it immutable.
    /// Changing the value will affect historical time entries. Instead, add a new value
    /// and deprecate the old one if needed.</para>
    /// <para><strong>Database Column:</strong> value (varchar(100), NOT NULL)</para>
    /// </remarks>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the project tag this value belongs to.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> Many-to-One (many TagValues belong to one ProjectTag)</para>
    /// <para><strong>Foreign Key:</strong> Shadow property "ProjectTagId" managed by EF Core</para>
    /// <para><strong>Rationale:</strong> Shadow properties prevent naming conflicts and ensure
    /// consistent data access patterns. See ADR 0001 for architectural decision.</para>
    /// <para><strong>Access Pattern:</strong></para>
    /// <code>
    /// // Load value with tag and project
    /// var tagValue = await dbContext.TagValues
    ///     .Include(v => v.ProjectTag)
    ///         .ThenInclude(t => t.Project)
    ///     .FirstOrDefaultAsync(v => v.Id == valueId);
    ///
    /// // Get tag name from value
    /// string tagName = tagValue.ProjectTag.TagName;
    ///
    /// // Get project code from value
    /// var projectCode = dbContext.Entry(tagValue.ProjectTag)
    ///     .Property&lt;string&gt;("ProjectCode")
    ///     .CurrentValue;
    ///
    /// // Validate value belongs to correct project
    /// bool isValid = tagValue.ProjectTag.Project.Code == "WEB-2025";
    /// </code>
    /// <para><strong>Delete Behavior:</strong> Cascade (value deleted when ProjectTag is deleted)</para>
    /// <para><strong>Required:</strong> null! indicates this is required but lazily loaded by EF Core</para>
    /// <para><strong>Business Rule:</strong> When validating time entry tags, ensure the TagValue's
    /// ProjectTag belongs to the same project as the time entry.</para>
    /// <para>See <see cref="ProjectTag"/> for tag configuration details.</para>
    /// </remarks>
    public ProjectTag ProjectTag { get; set; } = null!;
}
