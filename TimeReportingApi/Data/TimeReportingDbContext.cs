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
    public DbSet<TagConfiguration> TagConfigurations { get; set; }
    public DbSet<TimeEntryTag> TimeEntryTags { get; set; }
    public DbSet<TagAllowedValue> TagAllowedValues { get; set; }

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
        ConfigureTagConfiguration(modelBuilder);
        ConfigureTimeEntryTag(modelBuilder);
        ConfigureTagAllowedValue(modelBuilder);
    }

    private static void ConfigureTimeEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.ToTable("time_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.ProjectCode)
                .HasColumnName("project_code")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.ProjectTaskId)
                .HasColumnName("project_task_id")
                .IsRequired();

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
                        v.Replace("_", ""), ignoreCase: true))
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

            entity.HasOne(e => e.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(e => e.ProjectCode);

            entity.HasOne(e => e.ProjectTask)
                .WithMany()
                .HasForeignKey(e => e.ProjectTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Tags)
                .WithOne(t => t.TimeEntry)
                .HasForeignKey(t => t.TimeEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectCode, e.StartDate })
                .HasDatabaseName("idx_time_entries_project_date");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_time_entries_status");

            entity.HasIndex(e => new { e.UserId, e.StartDate })
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

            entity.HasMany(e => e.AvailableTasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.TagConfigurations)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectCode)
                .OnDelete(DeleteBehavior.Cascade);

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

            entity.Property(e => e.ProjectCode)
                .HasColumnName("project_code")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.TaskName)
                .HasColumnName("task_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active");

            entity.HasIndex(e => new { e.ProjectCode, e.TaskName })
                .IsUnique()
                .HasDatabaseName("uq_project_tasks_project_task");

            entity.HasIndex(e => e.ProjectCode)
                .HasDatabaseName("idx_project_tasks_project");
        });
    }

    private static void ConfigureTagConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TagConfiguration>(entity =>
        {
            entity.ToTable("tag_configurations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.ProjectCode)
                .HasColumnName("project_code")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.TagName)
                .HasColumnName("tag_name")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active");

            entity.HasMany(e => e.AllowedValues)
                .WithOne(v => v.TagConfiguration)
                .HasForeignKey(v => v.TagConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectCode, e.TagName })
                .IsUnique()
                .HasDatabaseName("uq_tag_configurations_project_tag");

            entity.HasIndex(e => e.ProjectCode)
                .HasDatabaseName("idx_tag_configurations_project");
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

            entity.Property(e => e.TimeEntryId)
                .HasColumnName("time_entry_id")
                .IsRequired();

            entity.Property(e => e.TagAllowedValueId)
                .HasColumnName("tag_allowed_value_id")
                .IsRequired();

            entity.HasOne(e => e.TagAllowedValue)
                .WithMany()
                .HasForeignKey(e => e.TagAllowedValueId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TimeEntryId)
                .HasDatabaseName("idx_time_entry_tags_entry");

            entity.HasIndex(e => new { e.TimeEntryId, e.TagAllowedValueId })
                .IsUnique()
                .HasDatabaseName("uq_time_entry_tags_entry_value");
        });
    }

    private static void ConfigureTagAllowedValue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TagAllowedValue>(entity =>
        {
            entity.ToTable("tag_allowed_values");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.TagConfigurationId)
                .HasColumnName("tag_configuration_id")
                .IsRequired();

            entity.Property(e => e.Value)
                .HasColumnName("value")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => e.TagConfigurationId)
                .HasDatabaseName("idx_tag_allowed_values_config");
        });
    }
}
