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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be added in Task 2.3
        // This method is currently minimal but required for EF Core
    }
}
