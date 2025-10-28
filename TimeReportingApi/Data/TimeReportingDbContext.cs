using System.Text.Json;
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

            entity.Property(e => e.Task)
                .HasColumnName("task")
                .HasMaxLength(100)
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
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.DeclineComment)
                .HasColumnName("decline_comment");

            entity.Property(e => e.Tags)
                .HasColumnName("tags")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Models.Tag>>(v, (JsonSerializerOptions?)null) ?? new List<Models.Tag>());

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

            entity.HasIndex(e => new { e.ProjectCode, e.StartDate })
                .HasDatabaseName("idx_time_entries_project_date");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_time_entries_status");

            entity.HasIndex(e => new { e.UserId, e.StartDate })
                .HasDatabaseName("idx_time_entries_user");
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

            entity.Property(e => e.AllowedValues)
                .HasColumnName("allowed_values")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active");

            entity.HasIndex(e => new { e.ProjectCode, e.TagName })
                .IsUnique()
                .HasDatabaseName("uq_tag_configurations_project_tag");

            entity.HasIndex(e => e.ProjectCode)
                .HasDatabaseName("idx_tag_configurations_project");
        });
    }
}
