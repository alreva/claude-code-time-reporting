namespace TimeReportingApi.Models;

/// <summary>
/// Represents a type of work activity available within a project.
/// Tasks provide structured categorization for time tracking entries.
/// </summary>
/// <remarks>
/// <para><strong>Business Purpose:</strong></para>
/// <para>ProjectTask defines the specific types of work that can be performed on a project.
/// When users log time, they must select a task from the project's available tasks,
/// ensuring consistent categorization and enabling detailed reporting by activity type.</para>
///
/// <para><strong>Key Characteristics:</strong></para>
/// <list type="bullet">
/// <item><description>Tasks are project-specific (same task name can exist in multiple projects)</description></item>
/// <item><description>Task name must be unique within a project</description></item>
/// <item><description>Tasks can be active or inactive (inactive tasks cannot be used for new entries)</description></item>
/// <item><description>Tasks cannot be deleted if time entries reference them</description></item>
/// </list>
///
/// <para><strong>Common Task Examples:</strong></para>
/// <list type="bullet">
/// <item><description>"Development" - Feature implementation and coding</description></item>
/// <item><description>"Bug Fixing" - Troubleshooting and bug resolution</description></item>
/// <item><description>"Code Review" - Reviewing pull requests and code quality</description></item>
/// <item><description>"Meetings" - Team meetings, client calls, planning sessions</description></item>
/// <item><description>"Documentation" - Technical writing and documentation</description></item>
/// <item><description>"Testing" - QA, unit testing, integration testing</description></item>
/// <item><description>"DevOps" - Infrastructure, deployment, CI/CD work</description></item>
/// </list>
///
/// <para><strong>Database Mapping:</strong></para>
/// <list type="bullet">
/// <item><description>Table: project_tasks</description></item>
/// <item><description>Primary Key: Id (auto-increment)</description></item>
/// <item><description>Foreign Key: ProjectCode (shadow property, references projects.code)</description></item>
/// <item><description>Unique Constraint: (ProjectCode, TaskName) via uq_project_tasks_project_task</description></item>
/// <item><description>Index: ProjectCode via idx_project_tasks_project</description></item>
/// </list>
///
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
/// <item><description>Many-to-One with Project (many tasks belong to one project)</description></item>
/// <item><description>One-to-Many with TimeEntry (many time entries reference one task)</description></item>
/// </list>
///
/// <para><strong>Validation Rules:</strong></para>
/// <list type="bullet">
/// <item><description>TaskName is required and max 100 characters</description></item>
/// <item><description>TaskName must be unique within the same project</description></item>
/// <item><description>Task must be active to be used in new time entries</description></item>
/// <item><description>Task must belong to same project as the time entry</description></item>
/// </list>
///
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Create project with tasks
/// Project project = new()
/// {
///     Code = "MOBILE-APP",
///     Name = "Mobile Application Development",
///     AvailableTasks = new()
///     {
///         new ProjectTask { TaskName = "Development", IsActive = true },
///         new ProjectTask { TaskName = "Bug Fixing", IsActive = true },
///         new ProjectTask { TaskName = "Code Review", IsActive = true },
///         new ProjectTask { TaskName = "Testing", IsActive = true },
///         new ProjectTask { TaskName = "Deployment", IsActive = false } // Deprecated task
///     }
/// };
///
/// // Time entry must reference one of these tasks
/// TimeEntry entry = new()
/// {
///     Project = project,
///     ProjectTask = project.AvailableTasks.First(t => t.TaskName == "Development"),
///     // ... other properties
/// };
/// </code>
///
/// <para><strong>Related Entities:</strong></para>
/// <para>See <see cref="Project"/>, <see cref="TimeEntry"/>.</para>
/// </remarks>
public class ProjectTask
{
    /// <summary>
    /// Unique identifier for the task.
    /// Auto-generated database identity column.
    /// </summary>
    /// <remarks>
    /// <para><strong>Database Column:</strong> id (integer, PRIMARY KEY, AUTO INCREMENT)</para>
    /// <para><strong>Generation:</strong> Automatically assigned by database on insert</para>
    /// <para><strong>Usage:</strong> Used as foreign key in time_entries.project_task_id</para>
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Name of the task describing the type of work activity.
    /// Must be unique within the project.
    /// </summary>
    /// <remarks>
    /// <para><strong>Format Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Max 100 characters</description></item>
    /// <item><description>Should be clear and concise</description></item>
    /// <item><description>Use consistent naming across projects when possible</description></item>
    /// <item><description>Recommended: Title Case (e.g., "Code Review" not "code review")</description></item>
    /// </list>
    /// <para><strong>Common Naming Patterns:</strong></para>
    /// <list type="bullet">
    /// <item><description>Activity-based: "Development", "Testing", "Deployment"</description></item>
    /// <item><description>Phase-based: "Requirements Gathering", "Design", "Implementation"</description></item>
    /// <item><description>Type-based: "Bug Fixing", "Feature Development", "Maintenance"</description></item>
    /// </list>
    /// <para><strong>Validation:</strong></para>
    /// <list type="bullet">
    /// <item><description>Required field (cannot be null or empty)</description></item>
    /// <item><description>Max length 100 characters</description></item>
    /// <item><description>Must be unique within the project (enforced by uq_project_tasks_project_task)</description></item>
    /// </list>
    /// <para><strong>Database Column:</strong> task_name (varchar(100), NOT NULL)</para>
    /// <para><strong>Database Constraint:</strong> Unique within project via composite index</para>
    /// </remarks>
    [Required]
    [MaxLength(100)]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the task is currently active and can be used for new time entries.
    /// </summary>
    /// <remarks>
    /// <para><strong>Business Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>Only active tasks can be selected when creating new time entries</description></item>
    /// <item><description>Inactive tasks remain in system for historical reporting</description></item>
    /// <item><description>Existing time entries with inactive tasks remain valid and accessible</description></item>
    /// <item><description>Tasks can be reactivated by setting IsActive = true</description></item>
    /// </list>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description>Deprecating outdated task types without losing history</description></item>
    /// <item><description>Temporarily disabling tasks during project phase changes</description></item>
    /// <item><description>Maintaining data integrity for reporting while evolving project structure</description></item>
    /// </list>
    /// <para><strong>Example Scenarios:</strong></para>
    /// <code>
    /// // Deprecate "Waterfall Design" task when moving to Agile
    /// var designTask = project.AvailableTasks
    ///     .First(t => t.TaskName == "Waterfall Design");
    /// designTask.IsActive = false;
    ///
    /// // Old time entries with this task remain valid
    /// var historicalEntries = dbContext.TimeEntries
    ///     .Where(e => e.ProjectTask.Id == designTask.Id)
    ///     .ToList(); // Still accessible for reporting
    ///
    /// // But new entries cannot use it
    /// // ValidationService will reject new entries with inactive tasks
    /// </code>
    /// <para><strong>Validation:</strong> New time entries must reference an active task</para>
    /// <para><strong>Database Column:</strong> is_active (boolean, NOT NULL, DEFAULT true)</para>
    /// </remarks>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the project this task belongs to.
    /// </summary>
    /// <remarks>
    /// <para><strong>Relationship:</strong> Many-to-One (many ProjectTasks belong to one Project)</para>
    /// <para><strong>Foreign Key:</strong> Shadow property "ProjectCode" managed by EF Core</para>
    /// <para><strong>Rationale:</strong> Shadow properties prevent naming conflicts and ensure
    /// consistent data access patterns. See ADR 0001 for architectural decision.</para>
    /// <para><strong>Access Pattern:</strong></para>
    /// <code>
    /// // Load task with project
    /// var task = await dbContext.ProjectTasks
    ///     .Include(t => t.Project)
    ///     .FirstOrDefaultAsync(t => t.Id == taskId);
    ///
    /// // Get project code via shadow property
    /// var projectCode = dbContext.Entry(task)
    ///     .Property&lt;string&gt;("ProjectCode")
    ///     .CurrentValue;
    /// </code>
    /// <para><strong>Delete Behavior:</strong> Cascade (task deleted when project is deleted)</para>
    /// <para><strong>Required:</strong> null! indicates this is required but lazily loaded by EF Core</para>
    /// <para>See <see cref="Project"/> for project details and <see cref="Data.TimeReportingDbContext"/>
    /// for shadow property configuration.</para>
    /// </remarks>
    public Project Project { get; set; } = null!;
}
