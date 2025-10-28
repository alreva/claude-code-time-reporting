using TimeReportingApi.Models;

namespace TimeReportingApi.Data;

/// <summary>
/// Database context for the Time Reporting System.
/// Manages all entities and database configuration.
/// </summary>
public class TimeReportingDbContext : DbContext
{
    public TimeReportingDbContext(DbContextOptions<TimeReportingDbContext> options)
        : base(options)
    {
    }

    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<ProjectTag> ProjectTags { get; set; }
    public DbSet<TimeEntryTag> TimeEntryTags { get; set; }
    public DbSet<TagValue> TagValues { get; set; }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Project project)
            {
                if (entry.State == EntityState.Added)
                {
                    project.CreatedAt = DateTime.UtcNow;
                }
                project.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is TimeEntry timeEntry)
            {
                if (entry.State == EntityState.Added)
                {
                    timeEntry.CreatedAt = DateTime.UtcNow;
                }
                timeEntry.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTimeEntry(modelBuilder);
        ConfigureProject(modelBuilder);
        ConfigureProjectTask(modelBuilder);
        ConfigureProjectTag(modelBuilder);
        ConfigureTimeEntryTag(modelBuilder);
        ConfigureTagValue(modelBuilder);
    }

    private static void ConfigureTimeEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.ToTable("time_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.IssueId)
                .HasColumnName("issue_id")
                .HasMaxLength(30);

            entity.Property(e => e.StandardHours)
                .HasColumnName("standard_hours")
                .HasPrecision(10, 2);

            entity.Property(e => e.OvertimeHours)
                .HasColumnName("overtime_hours")
                .HasPrecision(10, 2);

            entity.Property(e => e.Description)
                .HasColumnName("description");

            entity.Property(e => e.StartDate)
                .HasColumnName("start_date");

            entity.Property(e => e.CompletionDate)
                .HasColumnName("completion_date");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion(
                    v => System.Text.RegularExpressions.Regex.Replace(
                        v.ToString(), "([a-z])([A-Z])", "$1_$2").ToUpperInvariant(),
                    v => (TimeEntryStatus)Enum.Parse(typeof(TimeEntryStatus),
                        v.Replace("_", ""), true))
                .IsRequired();

            entity.Property(e => e.DeclineComment)
                .HasColumnName("decline_comment");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(100);

            // Configure shadow properties for foreign keys
            entity.Property<string>("ProjectCode")
                .HasColumnName("project_code")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property<int>("ProjectTaskId")
                .HasColumnName("project_task_id")
                .IsRequired();

            entity.HasOne(e => e.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey("ProjectCode")
                .IsRequired();

            entity.HasOne(e => e.ProjectTask)
                .WithMany()
                .HasForeignKey("ProjectTaskId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Tags)
                .WithOne(t => t.TimeEntry)
                .HasForeignKey("TimeEntryId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex("ProjectCode", nameof(TimeEntry.StartDate))
                .HasDatabaseName("idx_time_entries_project_date");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_time_entries_status");

            entity.HasIndex(nameof(TimeEntry.UserId), nameof(TimeEntry.StartDate))
                .HasDatabaseName("idx_time_entries_user");

            // Check constraints for data integrity
            entity.ToTable(t => t.HasCheckConstraint("chk_standard_hours_positive", "standard_hours >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("chk_overtime_hours_positive", "overtime_hours >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("chk_date_range", "start_date <= completion_date"));
            entity.ToTable(t => t.HasCheckConstraint("chk_status_valid",
                "status IN ('NOT_REPORTED', 'SUBMITTED', 'APPROVED', 'DECLINED')"));
        });
    }

    private static void ConfigureProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Code);

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(10);

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            // Relationships configured from the "many" side (ProjectTask, ProjectTag)

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_projects_active");

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("uq_projects_name");
        });
    }

    private static void ConfigureProjectTask(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.ToTable("project_tasks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.TaskName)
                .HasColumnName("task_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active");

            // Configure shadow property for foreign key
            entity.Property<string>("ProjectCode")
                .HasColumnName("project_code")
                .HasMaxLength(10)
                .IsRequired();

            entity.HasOne(e => e.Project)
                .WithMany(p => p.AvailableTasks)
                .HasForeignKey("ProjectCode")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex("ProjectCode", nameof(ProjectTask.TaskName))
                .IsUnique()
                .HasDatabaseName("uq_project_tasks_project_task");

            entity.HasIndex("ProjectCode")
                .HasDatabaseName("idx_project_tasks_project");
        });
    }

    private static void ConfigureProjectTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectTag>(entity =>
        {
            entity.ToTable("project_tags");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.TagName)
                .HasColumnName("tag_name")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.IsRequired)
                .HasColumnName("is_required");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active");

            // Configure shadow property for foreign key
            entity.Property<string>("ProjectCode")
                .HasColumnName("project_code")
                .HasMaxLength(10)
                .IsRequired();

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Tags)
                .HasForeignKey("ProjectCode")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex("ProjectCode", nameof(ProjectTag.TagName))
                .IsUnique()
                .HasDatabaseName("uq_project_tags_project_tag");

            entity.HasIndex("ProjectCode")
                .HasDatabaseName("idx_project_tags_project");
        });
    }

    private static void ConfigureTimeEntryTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntryTag>(entity =>
        {
            entity.ToTable("time_entry_tags");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            // Configure shadow properties for foreign keys
            entity.Property<Guid>("TimeEntryId")
                .HasColumnName("time_entry_id")
                .IsRequired();

            entity.Property<int>("TagValueId")
                .HasColumnName("tag_value_id")
                .IsRequired();

            entity.HasOne(e => e.TagValue)
                .WithMany()
                .HasForeignKey("TagValueId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex("TimeEntryId")
                .HasDatabaseName("idx_time_entry_tags_entry");

            entity.HasIndex("TimeEntryId", "TagValueId")
                .IsUnique()
                .HasDatabaseName("uq_time_entry_tags_entry_value");
        });
    }

    private static void ConfigureTagValue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TagValue>(entity =>
        {
            entity.ToTable("tag_values");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Value)
                .HasColumnName("value")
                .HasMaxLength(100)
                .IsRequired();

            // Configure shadow property for foreign key
            entity.Property<int>("ProjectTagId")
                .HasColumnName("project_tag_id")
                .IsRequired();

            entity.HasOne(e => e.ProjectTag)
                .WithMany(t => t.AllowedValues)
                .HasForeignKey("ProjectTagId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex("ProjectTagId")
                .HasDatabaseName("idx_tag_values_project_tag");
        });
    }
}
