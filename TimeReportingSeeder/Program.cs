using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TimeReportingApi.Data;
using TimeReportingSeeder.Data;

Console.WriteLine("=== Time Reporting Database Seeder ===\n");

// Load configuration from appsettings.json and environment variables
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Get connection string
var connectionString = configuration.GetConnectionString("TimeReportingDb");

if (string.IsNullOrEmpty(connectionString))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: Connection string 'TimeReportingDb' not found.");
    Console.WriteLine("\nPlease provide connection string via:");
    Console.WriteLine("  1. appsettings.json in current directory");
    Console.WriteLine("  2. Environment variable: ConnectionStrings__TimeReportingDb");
    Console.WriteLine("\nExample:");
    Console.WriteLine("  export ConnectionStrings__TimeReportingDb=\"Host=localhost;Port=5432;Database=time_reporting;Username=postgres;Password=postgres\"");
    Console.ResetColor();
    return 1;
}

Console.WriteLine($"Connection string: {MaskConnectionString(connectionString)}\n");

// Create DbContext
var optionsBuilder = new DbContextOptionsBuilder<TimeReportingDbContext>();
optionsBuilder.UseNpgsql(connectionString);

using var context = new TimeReportingDbContext(optionsBuilder.Options);

try
{
    Console.WriteLine("Testing database connection...");
    if (!await context.Database.CanConnectAsync())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ERROR: Cannot connect to database.");
        Console.ResetColor();
        return 1;
    }
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓ Database connection successful\n");
    Console.ResetColor();

    Console.WriteLine("Running database seeder...\n");
    DbSeeder.SeedData(context);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n✓ Seeding completed successfully!");
    Console.ResetColor();

    // Show summary
    Console.WriteLine("\nDatabase summary:");
    Console.WriteLine($"  Projects: {context.Projects.Count()}");
    Console.WriteLine($"  Tasks: {context.ProjectTasks.Count()}");
    Console.WriteLine($"  Tags: {context.ProjectTags.Count()}");
    Console.WriteLine($"  Tag Values: {context.TagValues.Count()}");
    Console.WriteLine($"  Time Entries: {context.TimeEntries.Count()}");
    Console.WriteLine($"  Time Entry Tags: {context.TimeEntryTags.Count()}");

    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nERROR: {ex.Message}");
    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
    Console.ResetColor();
    return 1;
}

static string MaskConnectionString(string connectionString)
{
    // Mask password in connection string for security
    var parts = connectionString.Split(';');
    var masked = new List<string>();
    foreach (var part in parts)
    {
        if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
        {
            masked.Add("Password=***");
        }
        else
        {
            masked.Add(part);
        }
    }
    return string.Join(";", masked);
}
