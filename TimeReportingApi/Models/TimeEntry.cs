namespace TimeReportingApi.Models;

/// <summary>
/// Represents a single time tracking entry in the system.
/// This is the central entity for logging work hours against projects and tasks.
/// </summary>
/// <remarks>
/// <para><strong>Business Purpose:</strong></para>
/// <para>TimeEntry captures all information about work performed, including hours worked,
/// project/task assignment, time period, and approval workflow status.</para>
///
/// <para><strong>Lifecycle:</strong></para>
/// <list type="number">
/// <item><description>User creates entry with project, task, hours, and date range</description></item>
/// <item><description>Entry can be edited multiple times while in NOT_REPORTED status</description></item>
/// <item><description>User submits entry for approval (status → SUBMITTED)</description></item>
/// <item><description>Manager approves (→ APPROVED) or declines (→ DECLINED) with comment</description></item>
/// <item><description>If declined, user can correct and resubmit</description></item>
/// </list>
///
/// <para><strong>Database Mapping:</strong></para>
/// <list type="bullet">
/// <item><description>Table: time_entries</description></item>
/// <item><description>Primary Key: Id (UUID)</description></item>
/// <item><description>Foreign Keys: ProjectCode (shadow), ProjectTaskId (shadow)</description></item>
/// <item><description>Indexes: (ProjectCode, StartDate), (Status), (UserId, StartDate)</description></item>
/// </list>
///
/// <para><strong>Validation Rules:</strong></para>
/// <list type="bullet">
/// <item><description>StandardHours and OvertimeHours must be >= 0</description></item>
/// <item><description>StartDate must be <= CompletionDate</description></item>
/// <item><description>Project must exist and be active</description></item>
/// <item><description>Task must be in project's available tasks</description></item>
/// <item><description>Tags must match project's tag configuration</description></item>
/// <item><description>Only NOT_REPORTED and DECLINED entries can be edited/deleted</description></item>
/// </list>
///
/// <para><strong>Related Entities:</strong></para>
/// <para>See <see cref="Project"/>, <see cref="ProjectTask"/>, <see cref="TimeEntryTag"/>, <see cref="TimeEntryStatus"/>.</para>
/// </remarks>
public class TimeEntry
{
    /// <summary>
    /// Unique identifier for the time entry.
    /// </summary>
    /// <remarks>
    /// <para>Generated automatically using Guid.NewGuid() when creating new entries.</para>
    /// <para><strong>Database Column:</strong> id (uuid, PRIMARY KEY)</para>
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    /// Optional reference to an external issue tracking system (e.g., Jira ticket ID).
    /// </summary>
    /// <remarks>
    /// <para>Used to link time entries to specific tickets, stories, or issues in external systems.</para>
    /// <para><strong>Examples:</strong> "PROJ-123", "BUG-456", "FEATURE-789"</para>
    /// <para><strong>Database Column:</strong> issue_id (varchar(30), nullable)</para>
    /// <para><strong>Validation:</strong> Max length 30 characters</para>
    /// </remarks>
    [MaxLength(30)]
    public string? IssueId { get; set; }

    /// <summary>
    /// Number of regular work hours for this entry.
    /// </summary>
    /// <remarks>
    /// <para>Represents standard business hours worked (typically within normal work schedule).</para>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Must be >= 0</description></item>
    /// <item><description>Stored with 2 decimal places precision (e.g., 7.5 hours)</description></item>
    /// <item><description>Used for standard payroll and billing calculations</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> standard_hours (decimal(10,2), NOT NULL)</para>
    /// <para><strong>Database Constraint:</strong> chk_standard_hours_positive</para>
    /// </remarks>
    [Range(0, double.MaxValue)]
    public decimal StandardHours { get; set; }

    /// <summary>
    /// Number of overtime hours for this entry.
    /// </summary>
    /// <remarks>
    /// <para>Represents hours worked beyond normal schedule (may have different billing/pay rates).</para>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Must be >= 0</description></item>
    /// <item><description>Defaults to 0 if not specified</description></item>
    /// <item><description>Stored with 2 decimal places precision</description></item>
    /// <item><description>Used for overtime pay calculations and client billing</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> overtime_hours (decimal(10,2), NOT NULL, DEFAULT 0)</para>
    /// <para><strong>Database Constraint:</strong> chk_overtime_hours_positive</para>
    /// </remarks>
    [Range(0, double.MaxValue)]
    public decimal OvertimeHours { get; set; }

    /// <summary>
    /// Free-text description of the work performed during this time entry.
    /// </summary>
    /// <remarks>
    /// <para>Provides context about what was accomplished. Used for reporting and approval review.</para>
    /// <para><strong>Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description>"Implemented user authentication feature"</description></item>
    /// <item><description>"Fixed critical bug in payment processing"</description></item>
    /// <item><description>"Client meeting and requirements gathering"</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> description (text, nullable)</para>
    /// <para><strong>Best Practice:</strong> Include enough detail for managers to understand
    /// the work performed without being overly verbose.</para>
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// The date when work on this entry started.
    /// </summary>
    /// <remarks>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Must be <= CompletionDate</description></item>
    /// <item><description>Used for filtering and reporting by date range</description></item>
    /// <item><description>Can span multiple days (e.g., StartDate = Monday, CompletionDate = Friday)</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> start_date (date, NOT NULL)</para>
    /// <para><strong>Database Constraint:</strong> chk_date_range (start_date <= completion_date)</para>
    /// <para><strong>Type:</strong> DateOnly (date without time component, introduced in .NET 6)</para>
    /// </remarks>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The date when work on this entry was completed.
    /// </summary>
    /// <remarks>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Must be >= StartDate</description></item>
    /// <item><description>Can be the same as StartDate for single-day entries</description></item>
    /// <item><description>Used for filtering and reporting by date range</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> completion_date (date, NOT NULL)</para>
    /// <para><strong>Database Constraint:</strong> chk_date_range (start_date <= completion_date)</para>
    /// <para><strong>Type:</strong> DateOnly (date without time component)</para>
    /// </remarks>
    public DateOnly CompletionDate { get; set; }

    /// <summary>
    /// Current workflow status of the time entry.
    /// Controls editability and determines available actions.
    /// </summary>
    /// <remarks>
    /// <para><strong>Status Flow:</strong> NOT_REPORTED → SUBMITTED → APPROVED/DECLINED</para>
    /// <para><strong>Default:</strong> NotReported when entry is first created</para>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Only NOT_REPORTED and DECLINED entries can be edited or deleted</description></item>
    /// <item><description>SUBMITTED entries are read-only, awaiting approval</description></item>
    /// <item><description>APPROVED entries are immutable (terminal state)</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> status (varchar, NOT NULL)</para>
    /// <para><strong>Database Constraint:</strong> chk_status_valid</para>
    /// <para>See <see cref="TimeEntryStatus"/> for detailed status documentation.</para>
    /// </remarks>
    [Required]
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.NotReported;

    /// <summary>
    /// Manager's comment explaining why the entry was declined.
    /// Only populated when Status = DECLINED.
    /// </summary>
    /// <remarks>
    /// <para><strong>Purpose:</strong> Provides feedback to the user about what needs to be corrected
    /// before resubmitting the entry.</para>
    /// <para><strong>Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description>"Please add more details about the work performed"</description></item>
    /// <item><description>"Incorrect project code - should be CLIENT-B, not INTERNAL"</description></item>
    /// <item><description>"Hours seem too high for this type of task"</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> decline_comment (text, nullable)</para>
    /// <para><strong>Lifecycle:</strong> Set by manager when declining, user should read before
    /// making corrections and resubmitting.</para>
    /// </remarks>
    public string? DeclineComment { get; set; }

    /// <summary>
    /// Timestamp when the entry was first created.
    /// Automatically set by the database context.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> created_at (timestamptz, NOT NULL, DEFAULT NOW())</para>
    /// <para><strong>Management:</strong> Automatically populated by TimeReportingDbContext.SaveChanges()
    /// when entry is added.</para>
    /// <para><strong>Type:</strong> DateTime (stored as UTC in database)</para>
    /// <para><strong>Immutable:</strong> Never updated after initial creation</para>
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entry was last modified.
    /// Automatically updated by the database context.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> updated_at (timestamptz, NOT NULL, DEFAULT NOW())</para>
    /// <para><strong>Management:</strong> Automatically updated by TimeReportingDbContext.SaveChanges()
    /// whenever entry is modified.</para>
    /// <para><strong>Type:</strong> DateTime (stored as UTC in database)</para>
    /// <para><strong>Use Cases:</strong> Tracking last modification, audit trails, conflict detection</para>
    /// </remarks>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Identifier of the user who created this time entry.
    /// Used for multi-tenant scenarios and filtering entries by user.
    /// </summary>
    /// <remarks>
    /// <para><strong>Source:</strong> Extracted from Azure Entra ID token 'oid' or 'sub' claim</para>
    /// <para><strong>Format:</strong> Azure AD Object ID (GUID) or Subject identifier</para>
    /// <para><strong>Examples:</strong> "a1b2c3d4-e5f6-7890-abcd-ef1234567890"</para>
    /// <para><strong>Database Column:</strong> user_id (varchar(100), nullable)</para>
    /// <para><strong>Database Index:</strong> idx_time_entries_user (user_id, start_date)</para>
    /// <para><strong>Security:</strong> Automatically populated from authenticated token claims
    /// to prevent users from creating entries for others.</para>
    /// <para><strong>Nullable:</strong> Allows for system-generated or imported entries (pre-Phase 14)</para>
    /// </remarks>
    [MaxLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// Email address of the user who created this time entry.
    /// Used for display purposes and user-friendly reporting.
    /// </summary>
    /// <remarks>
    /// <para><strong>Source:</strong> Extracted from Azure Entra ID token 'email' claim</para>
    /// <para><strong>Examples:</strong> "john.doe@company.com", "jane.smith@company.com"</para>
    /// <para><strong>Database Column:</strong> user_email (varchar(255), nullable)</para>
    /// <para><strong>Purpose:</strong> Human-readable identifier for reports and UI displays</para>
    /// <para><strong>Security:</strong> Automatically populated from authenticated token claims</para>
    /// <para><strong>Note:</strong> May be null for entries created before Phase 14 or by system</para>
    /// </remarks>
    [MaxLength(255)]
    public string? UserEmail { get; set; }

    /// <summary>
    /// Display name of the user who created this time entry.
    /// Used for user-friendly identification in reports and UI.
    /// </summary>
    /// <remarks>
    /// <para><strong>Source:</strong> Extracted from Azure Entra ID token 'name', 'given_name', or 'preferred_username' claims</para>
    /// <para><strong>Examples:</strong> "John Doe", "Jane Smith"</para>
    /// <para><strong>Database Column:</strong> user_name (varchar(255), nullable)</para>
    /// <para><strong>Purpose:</strong> Human-readable name for reports, audit logs, and UI displays</para>
    /// <para><strong>Format:</strong> Typically "FirstName LastName" or preferred username from Entra ID</para>
    /// <para><strong>Security:</strong> Automatically populated from authenticated token claims</para>
    /// <para><strong>Note:</strong> May be null for entries created before Phase 14 or by system</para>
    /// </remarks>
    [MaxLength(255)]
    public string? UserName { get; set; }

    /// <summary>
    /// Navigation property to the project this time entry is associated with.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> Many-to-One (many TimeEntries belong to one Project)</para>
    /// <para><strong>Foreign Key:</strong> Shadow property "ProjectCode" managed by EF Core</para>
    /// <para><strong>Access Pattern:</strong> Use .Include(e => e.Project) when querying to eagerly load</para>
    /// <para><strong>Required:</strong> null! indicates this is required but lazily loaded</para>
    /// <para>See <see cref="Project"/> for project details and <see cref="Data.TimeReportingDbContext"/>
    /// for shadow property configuration.</para>
    /// </remarks>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation property to the specific task within the project.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> Many-to-One (many TimeEntries use one ProjectTask)</para>
    /// <para><strong>Foreign Key:</strong> Shadow property "ProjectTaskId" managed by EF Core</para>
    /// <para><strong>Validation:</strong> Task must belong to the same project as Project property</para>
    /// <para><strong>Access Pattern:</strong> Use .Include(e => e.ProjectTask) when querying</para>
    /// <para><strong>Required:</strong> null! indicates this is required but lazily loaded</para>
    /// <para><strong>Delete Behavior:</strong> Restrict (cannot delete task if time entries exist)</para>
    /// <para>See <see cref="ProjectTask"/> for task details and ADR 0001 for shadow property rationale.</para>
    /// </remarks>
    public ProjectTask ProjectTask { get; set; } = null!;

    /// <summary>
    /// Collection of metadata tags associated with this time entry.
    /// Tags are project-specific and must match the project's tag configuration.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> One-to-Many (one TimeEntry has many TimeEntryTags)</para>
    /// <para><strong>Purpose:</strong> Store additional structured metadata like Environment, Priority, Sprint</para>
    /// <para><strong>Examples:</strong></para>
    /// <code>
    /// // Time entry with tags
    /// TimeEntry entry = new()
    /// {
    ///     // ... other properties
    ///     Tags = new()
    ///     {
    ///         new TimeEntryTag { TagValue = environmentProdValue },  // Environment: Production
    ///         new TimeEntryTag { TagValue = priorityHighValue }     // Priority: High
    ///     }
    /// };
    /// </code>
    /// <para><strong>Validation:</strong></para>
    /// <list type="bullet">
    /// <item><description>Tag names must exist in Project.Tags</description></item>
    /// <item><description>Tag values must be in ProjectTag.AllowedValues</description></item>
    /// <item><description>Tags are optional but validated if provided</description></item>
    /// </list>
    /// <para><strong>Access Pattern:</strong> Use .Include(e => e.Tags).ThenInclude(t => t.TagValue)
    /// .ThenInclude(tv => tv.ProjectTag) for full tag data</para>
    /// <para><strong>Delete Behavior:</strong> Cascade (tags deleted when entry is deleted)</para>
    /// <para>See <see cref="TimeEntryTag"/>, <see cref="ProjectTag"/>, <see cref="TagValue"/> for tag system architecture.</para>
    /// </remarks>
    public List<TimeEntryTag> Tags { get; set; } = new();
}
