namespace TimeReportingApi.Models;

/// <summary>
/// Represents a project that users can log time against.
/// Projects define the scope of work and determine available tasks and tag configurations.
/// </summary>
/// <remarks>
/// <para><strong>Business Purpose:</strong></para>
/// <para>Projects are the top-level organizational unit for time tracking. Each project has its own
/// set of allowed tasks and tag configurations, ensuring that time entries are properly categorized
/// and validated according to project-specific requirements.</para>
///
/// <para><strong>Key Characteristics:</strong></para>
/// <list type="bullet">
/// <item><description>Identified by a short, unique code (max 10 characters)</description></item>
/// <item><description>Can be active or inactive (inactive projects cannot receive new time entries)</description></item>
/// <item><description>Defines available tasks (e.g., "Development", "Bug Fixing", "Meetings")</description></item>
/// <item><description>Defines tag configurations for metadata (e.g., "Environment", "Priority")</description></item>
/// </list>
///
/// <para><strong>Database Mapping:</strong></para>
/// <list type="bullet">
/// <item><description>Table: projects</description></item>
/// <item><description>Primary Key: Code (VARCHAR(10))</description></item>
/// <item><description>Unique Constraint: Name (uq_projects_name)</description></item>
/// <item><description>Index: IsActive (idx_projects_active)</description></item>
/// </list>
///
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
/// <item><description>One-to-Many with ProjectTask (delete cascade)</description></item>
/// <item><description>One-to-Many with ProjectTag (delete cascade)</description></item>
/// <item><description>One-to-Many with TimeEntry (delete restrict)</description></item>
/// </list>
///
/// <para><strong>Usage Examples:</strong></para>
/// <code>
/// // Internal development project
/// Project internal = new()
/// {
///     Code = "INTERNAL",
///     Name = "Internal Development",
///     IsActive = true
/// };
///
/// // Client project with code
/// Project clientA = new()
/// {
///     Code = "CLIENT-A",
///     Name = "Acme Corporation - Mobile App",
///     IsActive = true
/// };
/// </code>
///
/// <para><strong>Related Entities:</strong></para>
/// <para>See <see cref="ProjectTask"/>, <see cref="ProjectTag"/>, <see cref="TimeEntry"/>.</para>
/// </remarks>
public class Project
{
    /// <summary>
    /// Unique short code identifying the project.
    /// Used as the primary key and in time entry references.
    /// </summary>
    /// <remarks>
    /// <para><strong>Format Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Max 10 characters (uppercase recommended)</description></item>
    /// <item><description>Alphanumeric with hyphens (e.g., "PROJ-123", "INTERNAL")</description></item>
    /// <item><description>Should be meaningful and easy to remember</description></item>
    /// <item><description>Cannot be changed after creation (primary key)</description></item>
    /// </list>
    /// <para><strong>Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description>"INTERNAL" - Internal development work</description></item>
    /// <item><description>"CLIENT-A" - Client project A</description></item>
    /// <item><description>"MAINT" - Maintenance and support</description></item>
    /// <item><description>"PROJ-2025" - Year-specific project</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> code (varchar(10), PRIMARY KEY)</para>
    /// <para><strong>Validation:</strong> Required, max length 10 characters</para>
    /// </remarks>
    [Key]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full display name of the project.
    /// Provides human-readable description for UI and reports.
    /// </summary>
    /// <remarks>
    /// <para><strong>Purpose:</strong> Used in dropdowns, reports, and user interfaces
    /// where the short code alone might not be descriptive enough.</para>
    /// <para><strong>Format Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Max 200 characters</description></item>
    /// <item><description>Should be clear and descriptive</description></item>
    /// <item><description>May include client name, project purpose, or phase</description></item>
    /// <item><description>Must be unique across all projects</description></item>
    /// </list>
    /// <para><strong>Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description>"Internal Development and R&amp;D"</description></item>
    /// <item><description>"Acme Corporation - Mobile App Development"</description></item>
    /// <item><description>"Client B - Legacy System Maintenance"</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> name (varchar(200), NOT NULL, UNIQUE)</para>
    /// <para><strong>Database Constraint:</strong> uq_projects_name</para>
    /// </remarks>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the project is currently active and accepting new time entries.
    /// </summary>
    /// <remarks>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Only active projects can receive new time entries</description></item>
    /// <item><description>Inactive projects remain in the system for historical reporting</description></item>
    /// <item><description>Existing time entries on inactive projects remain accessible</description></item>
    /// <item><description>Projects can be reactivated by setting IsActive = true</description></item>
    /// </list>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description>Project completion: Set IsActive = false when project ends</description></item>
    /// <item><description>Project pause: Temporarily disable time tracking</description></item>
    /// <item><description>Project archive: Keep historical data without allowing new entries</description></item>
    /// </list>
    /// <para><strong>Validation:</strong> New time entries must reference an active project</para>
    /// <para><strong>Database Column:</strong> is_active (boolean, NOT NULL, DEFAULT true)</para>
    /// <para><strong>Database Index:</strong> idx_projects_active (for filtering queries)</para>
    /// </remarks>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the project was first created.
    /// Automatically set by the database context.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> created_at (timestamptz, NOT NULL, DEFAULT NOW())</para>
    /// <para><strong>Management:</strong> Automatically populated by TimeReportingDbContext.SaveChanges()
    /// when project is added.</para>
    /// <para><strong>Type:</strong> DateTime (stored as UTC in database)</para>
    /// <para><strong>Immutable:</strong> Never updated after initial creation</para>
    /// <para><strong>Use Cases:</strong> Audit trails, project history, data migration tracking</para>
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the project was last modified.
    /// Automatically updated by the database context.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> updated_at (timestamptz, NOT NULL, DEFAULT NOW())</para>
    /// <para><strong>Management:</strong> Automatically updated by TimeReportingDbContext.SaveChanges()
    /// whenever project is modified.</para>
    /// <para><strong>Type:</strong> DateTime (stored as UTC in database)</para>
    /// <para><strong>Use Cases:</strong> Tracking configuration changes, audit trails</para>
    /// </remarks>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Collection of tasks available for this project.
    /// Users must select from these tasks when logging time.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> One-to-Many (one Project has many ProjectTasks)</para>
    /// <para><strong>Purpose:</strong> Defines the types of work activities that can be performed
    /// on this project, providing structured categorization of time entries.</para>
    /// <para><strong>Examples:</strong></para>
    /// <code>
    /// Project project = new()
    /// {
    ///     Code = "DEV-2025",
    ///     Name = "Development Project 2025",
    ///     AvailableTasks = new()
    ///     {
    ///         new ProjectTask { TaskName = "Development", IsActive = true },
    ///         new ProjectTask { TaskName = "Bug Fixing", IsActive = true },
    ///         new ProjectTask { TaskName = "Code Review", IsActive = true },
    ///         new ProjectTask { TaskName = "Meetings", IsActive = true }
    ///     }
    /// };
    /// </code>
    /// <para><strong>Validation:</strong> Time entries must reference a task from this collection
    /// that is both active and belongs to the same project.</para>
    /// <para><strong>Access Pattern:</strong> Use .Include(p => p.AvailableTasks) when querying
    /// to avoid N+1 query problems.</para>
    /// <para><strong>Delete Behavior:</strong> Cascade (tasks deleted when project is deleted)</para>
    /// <para>See <see cref="ProjectTask"/> for task details.</para>
    /// </remarks>
    public List<ProjectTask> AvailableTasks { get; set; } = new();

    /// <summary>
    /// Collection of tag configurations available for this project.
    /// Defines the metadata tags that can be attached to time entries.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> One-to-Many (one Project has many ProjectTags)</para>
    /// <para><strong>Purpose:</strong> Defines additional structured metadata fields that provide
    /// extra categorization and filtering capabilities for time entries.</para>
    /// <para><strong>Tag System Design:</strong></para>
    /// <para>Each ProjectTag represents a tag category (e.g., "Environment", "Priority")
    /// with a set of allowed values (e.g., "Production", "Staging", "Development").</para>
    /// <para><strong>Examples:</strong></para>
    /// <code>
    /// Project project = new()
    /// {
    ///     Code = "WEB-APP",
    ///     Name = "Web Application Project",
    ///     Tags = new()
    ///     {
    ///         new ProjectTag
    ///         {
    ///             TagName = "Environment",
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
    ///             AllowedValues = new()
    ///             {
    ///                 new TagValue { Value = "High" },
    ///                 new TagValue { Value = "Medium" },
    ///                 new TagValue { Value = "Low" }
    ///             }
    ///         }
    ///     }
    /// };
    /// </code>
    /// <para><strong>Validation:</strong> Time entry tags must reference tags from this collection
    /// with values from the tag's AllowedValues list.</para>
    /// <para><strong>Access Pattern:</strong> Use .Include(p => p.Tags).ThenInclude(t => t.AllowedValues)
    /// for full tag configuration data.</para>
    /// <para><strong>Delete Behavior:</strong> Cascade (tags deleted when project is deleted)</para>
    /// <para>See <see cref="ProjectTag"/>, <see cref="TagValue"/> for tag system architecture.</para>
    /// </remarks>
    public List<ProjectTag> Tags { get; set; } = new();

    /// <summary>
    /// Collection of all time entries logged against this project.
    /// Provides navigation from project to its associated time data.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> One-to-Many (one Project has many TimeEntries)</para>
    /// <para><strong>Purpose:</strong> Enables querying all time tracked for a project,
    /// useful for project reporting, billing, and analysis.</para>
    /// <para><strong>Access Pattern:</strong></para>
    /// <code>
    /// // Get project with all time entries
    /// var project = await dbContext.Projects
    ///     .Include(p => p.TimeEntries)
    ///     .FirstOrDefaultAsync(p => p.Code == "INTERNAL");
    ///
    /// // Calculate total hours for project
    /// decimal totalHours = project.TimeEntries
    ///     .Where(e => e.Status == TimeEntryStatus.Approved)
    ///     .Sum(e => e.StandardHours + e.OvertimeHours);
    /// </code>
    /// <para><strong>Performance Note:</strong> This collection can be large for long-running projects.
    /// Use filtering and pagination when loading time entries. Avoid eager loading this collection
    /// unless specifically needed.</para>
    /// <para><strong>Delete Behavior:</strong> Restrict (cannot delete project if time entries exist)</para>
    /// <para>See <see cref="TimeEntry"/> for time entry details.</para>
    /// </remarks>
    public List<TimeEntry> TimeEntries { get; set; } = new();
}
