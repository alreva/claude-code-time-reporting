namespace TimeReportingApi.Models;

/// <summary>
/// Represents a metadata tag category available for a project.
/// Tags provide structured extensibility for project-specific custom fields.
/// </summary>
/// <remarks>
/// <para><strong>Business Purpose:</strong></para>
/// <para>ProjectTag defines a category of metadata that can be attached to time entries.
/// Each tag has a name (e.g., "Environment", "Priority") and a set of allowed values.
/// This provides a controlled vocabulary system for categorizing time entries with
/// project-specific metadata beyond the core fields.</para>
///
/// <para><strong>Tag System Architecture:</strong></para>
/// <para>The tag system uses a three-level hierarchy:</para>
/// <list type="number">
/// <item><description><strong>ProjectTag:</strong> Defines the tag category (e.g., "Environment")</description></item>
/// <item><description><strong>TagValue:</strong> Defines allowed values (e.g., "Production", "Staging", "Development")</description></item>
/// <item><description><strong>TimeEntryTag:</strong> Associates a specific tag value with a time entry</description></item>
/// </list>
///
/// <para><strong>Common Tag Categories:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Environment:</strong> Production, Staging, Development, QA</description></item>
/// <item><description><strong>Priority:</strong> High, Medium, Low, Critical</description></item>
/// <item><description><strong>Sprint:</strong> Sprint-1, Sprint-2, Sprint-3</description></item>
/// <item><description><strong>Billable:</strong> Yes, No</description></item>
/// <item><description><strong>Client Facing:</strong> Yes, No</description></item>
/// <item><description><strong>Work Type:</strong> Remote, Onsite, Hybrid</description></item>
/// <item><description><strong>Phase:</strong> Planning, Implementation, Testing, Deployment</description></item>
/// </list>
///
/// <para><strong>Key Characteristics:</strong></para>
/// <list type="bullet">
/// <item><description>Tags are project-specific (different projects can have different tags)</description></item>
/// <item><description>Tag name must be unique within a project</description></item>
/// <item><description>Each tag defines a closed set of allowed values (controlled vocabulary)</description></item>
/// <item><description>Tags can be active or inactive</description></item>
/// <item><description>Tags are optional on time entries (not all projects require tags)</description></item>
/// </list>
///
/// <para><strong>Database Mapping:</strong></para>
/// <list type="bullet">
/// <item><description>Table: project_tags</description></item>
/// <item><description>Primary Key: Id (auto-increment)</description></item>
/// <item><description>Foreign Key: ProjectCode (shadow property, references projects.code)</description></item>
/// <item><description>Unique Constraint: (ProjectCode, TagName) via uq_project_tags_project_tag</description></item>
/// <item><description>Index: ProjectCode via idx_project_tags_project</description></item>
/// </list>
///
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
/// <item><description>Many-to-One with Project (many tags belong to one project)</description></item>
/// <item><description>One-to-Many with TagValue (one tag has many allowed values)</description></item>
/// </list>
///
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Define tags for a web development project
/// Project project = new()
/// {
///     Code = "WEB-2025",
///     Name = "Web Development 2025",
///     Tags = new()
///     {
///         new ProjectTag
///         {
///             TagName = "Environment",
///             IsActive = true,
///             AllowedValues = new()
///             {
///                 new TagValue { Value = "Production" },
///                 new TagValue { Value = "Staging" },
///                 new TagValue { Value = "Development" }
///             }
///         },
///         new ProjectTag
///         {
///             TagName = "Priority",
///             IsActive = true,
///             AllowedValues = new()
///             {
///                 new TagValue { Value = "High" },
///                 new TagValue { Value = "Medium" },
///                 new TagValue { Value = "Low" }
///             }
///         }
///     }
/// };
///
/// // Time entries can now use these tags
/// TimeEntry entry = new()
/// {
///     Project = project,
///     // ... other properties
///     Tags = new()
///     {
///         new TimeEntryTag
///         {
///             TagValue = project.Tags
///                 .First(t => t.TagName == "Environment")
///                 .AllowedValues.First(v => v.Value == "Production")
///         }
///     }
/// };
/// </code>
///
/// <para><strong>Validation Rules:</strong></para>
/// <list type="bullet">
/// <item><description>TagName is required and max 20 characters</description></item>
/// <item><description>TagName must be unique within the project</description></item>
/// <item><description>Tag must have at least one allowed value to be useful</description></item>
/// <item><description>Time entry tags must reference values from AllowedValues</description></item>
/// </list>
///
/// <para><strong>Naming Consistency:</strong></para>
/// <para>This entity was renamed from "TagConfiguration" to "ProjectTag" for consistency
/// with the naming pattern established by "ProjectTask". See ADR 0003 for the rationale.</para>
///
/// <para><strong>Related Entities:</strong></para>
/// <para>See <see cref="Project"/>, <see cref="TagValue"/>, <see cref="TimeEntryTag"/>.</para>
/// </remarks>
public class ProjectTag
{
    /// <summary>
    /// Unique identifier for the tag configuration.
    /// Auto-generated database identity column.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> id (integer, PRIMARY KEY, AUTO INCREMENT)</para>
    /// <para><strong>Generation:</strong> Automatically assigned by database on insert</para>
    /// <para><strong>Usage:</strong> Used as foreign key in tag_values.project_tag_id</para>
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Name of the tag category.
    /// Must be unique within the project.
    /// </summary>
    /// <remarks>
    /// <para><strong>Format Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Max 20 characters (keep concise)</description></item>
    /// <item><description>Should describe a category or dimension of metadata</description></item>
    /// <item><description>Recommended: Title Case without spaces (e.g., "Environment", "Priority")</description></item>
    /// <item><description>Avoid abbreviations unless widely understood</description></item>
    /// </list>
    /// <para><strong>Naming Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item><description>Use singular nouns: "Environment" not "Environments"</description></item>
    /// <item><description>Be specific but not overly detailed</description></item>
    /// <item><description>Consider how it will appear in UI dropdowns and reports</description></item>
    /// </list>
    /// <para><strong>Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description>"Environment" - Deployment environment</description></item>
    /// <item><description>"Priority" - Work priority level</description></item>
    /// <item><description>"Sprint" - Agile sprint identifier</description></item>
    /// <item><description>"Billable" - Whether work is billable to client</description></item>
    /// <item><description>"WorkType" - Remote/onsite classification</description></item>
    /// </list>
    /// <para><strong>Validation:</strong></para>
    /// <list type="bullet">
    /// <item><description>Required field (cannot be null or empty)</description></item>
    /// <item><description>Max length 20 characters</description></item>
    /// <item><description>Must be unique within the project (enforced by uq_project_tags_project_tag)</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> tag_name (varchar(20), NOT NULL)</para>
    /// <para><strong>Database Constraint:</strong> Unique within project via composite index</para>
    /// </remarks>
    [Required]
    [MaxLength(20)]
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this tag is required when creating or updating time entries.
    /// </summary>
    /// <remarks>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Required tags must be provided when logging time</description></item>
    /// <item><description>Optional tags can be omitted</description></item>
    /// <item><description>Used for enforcing project-specific metadata requirements</description></item>
    /// </list>
    /// <para><strong>Example:</strong> "Priority" tag marked as required for client projects</para>
    /// <para><strong>Database Column:</strong> is_required (boolean, NOT NULL, DEFAULT false)</para>
    /// </remarks>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Indicates whether the tag is currently active and can be used for new time entries.
    /// </summary>
    /// <remarks>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Only active tags can be used when creating or updating time entries</description></item>
    /// <item><description>Inactive tags remain in system for historical data</description></item>
    /// <item><description>Existing time entries with inactive tags remain valid</description></item>
    /// <item><description>Tags can be reactivated by setting IsActive = true</description></item>
    /// </list>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description>Deprecating outdated tag categories (e.g., old sprint naming scheme)</description></item>
    /// <item><description>Temporarily disabling tags during project restructuring</description></item>
    /// <item><description>Maintaining historical data while evolving metadata structure</description></item>
    /// </list>
    /// <para><strong>Example Scenario:</strong></para>
    /// <code>
    /// // Project switches from numeric to named sprints
    /// var oldSprintTag = project.Tags.First(t => t.TagName == "SprintNumber");
    /// oldSprintTag.IsActive = false; // Deprecate numeric sprint tag
    ///
    /// // Add new sprint tag with names
    /// project.Tags.Add(new ProjectTag
    /// {
    ///     TagName = "SprintName",
    ///     IsActive = true,
    ///     AllowedValues = new()
    ///     {
    ///         new TagValue { Value = "Alpha Release" },
    ///         new TagValue { Value = "Beta Testing" },
    ///         new TagValue { Value = "GA Preparation" }
    ///     }
    /// });
    ///
    /// // Old entries with "SprintNumber" remain accessible
    /// // New entries must use "SprintName"
    /// </code>
    /// <para><strong>Validation:</strong> New time entries can only use active tags</para>
    /// <para><strong>Database Column:</strong> is_active (boolean, NOT NULL, DEFAULT true)</para>
    /// </remarks>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the project this tag belongs to.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> Many-to-One (many ProjectTags belong to one Project)</para>
    /// <para><strong>Foreign Key:</strong> Shadow property "ProjectCode" managed by EF Core</para>
    /// <para><strong>Rationale:</strong> Shadow properties prevent naming conflicts and ensure
    /// consistent data access patterns. See ADR 0001 for architectural decision.</para>
    /// <para><strong>Access Pattern:</strong></para>
    /// <code>
    /// // Load tag with project and values
    /// var tag = await dbContext.ProjectTags
    ///     .Include(t => t.Project)
    ///     .Include(t => t.AllowedValues)
    ///     .FirstOrDefaultAsync(t => t.Id == tagId);
    ///
    /// // Get project code via shadow property
    /// var projectCode = dbContext.Entry(tag)
    ///     .Property&lt;string&gt;("ProjectCode")
    ///     .CurrentValue;
    /// </code>
    /// <para><strong>Delete Behavior:</strong> Cascade (tag deleted when project is deleted)</para>
    /// <para><strong>Required:</strong> null! indicates this is required but lazily loaded by EF Core</para>
    /// <para>See <see cref="Project"/> for project details.</para>
    /// </remarks>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Collection of allowed values for this tag.
    /// Defines the controlled vocabulary for this tag category.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> One-to-Many (one ProjectTag has many TagValues)</para>
    /// <para><strong>Purpose:</strong> Enforces data integrity by restricting tag values to a
    /// predefined set of valid options. This prevents typos, inconsistencies, and invalid data.</para>
    /// <para><strong>Validation:</strong></para>
    /// <list type="bullet">
    /// <item><description>Time entry tags must reference a value from this collection</description></item>
    /// <item><description>Each value should be unique within the tag (enforced at validation layer)</description></item>
    /// <item><description>Recommended: At least one value should be defined for tag to be useful</description></item>
    /// </list>
    /// <para><strong>Examples:</strong></para>
    /// <code>
    /// // Environment tag with common values
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
    /// // Binary tag (Yes/No)
    /// ProjectTag billableTag = new()
    /// {
    ///     TagName = "Billable",
    ///     AllowedValues = new()
    ///     {
    ///         new TagValue { Value = "Yes" },
    ///         new TagValue { Value = "No" }
    ///     }
    /// };
    ///
    /// // Sprint tag with sprint identifiers
    /// ProjectTag sprintTag = new()
    /// {
    ///     TagName = "Sprint",
    ///     AllowedValues = new()
    ///     {
    ///         new TagValue { Value = "Sprint-1" },
    ///         new TagValue { Value = "Sprint-2" },
    ///         new TagValue { Value = "Sprint-3" }
    ///     }
    /// };
    /// </code>
    /// <para><strong>Access Pattern:</strong> Use .Include(t => t.AllowedValues) when querying
    /// to load the complete tag configuration for validation.</para>
    /// <para><strong>Delete Behavior:</strong> Cascade (values deleted when tag is deleted)</para>
    /// <para><strong>Management:</strong> Values can be added/removed as project needs evolve,
    /// but consider impact on existing time entries.</para>
    /// <para>See <see cref="TagValue"/> for individual value details.</para>
    /// </remarks>
    public List<TagValue> AllowedValues { get; set; } = new();
}
