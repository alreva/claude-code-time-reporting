using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace TimeReportingMcpSdk.Tools;

/// <summary>
/// Tool to auto-generate GraphQL fragments from schema.
/// Usage: dotnet run -- generate-fragments --schema schema.graphql --output Fragments.graphql
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        var app = new CommandApp<GenerateFragmentsCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("fragment-generator");
            config.AddCommand<GenerateFragmentsCommand>("generate-fragments")
                .WithDescription("Generate GraphQL fragments from schema");
        });

        return app.Run(args);
    }
}

/// <summary>
/// Command to generate GraphQL fragments from schema file.
/// </summary>
class GenerateFragmentsCommand : Command<GenerateFragmentsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--schema <PATH>")]
        [Description("Path to the GraphQL schema file")]
        public string? SchemaPath { get; init; }

        [CommandOption("--output <PATH>")]
        [Description("Path for the generated fragments file")]
        public string? OutputPath { get; init; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(SchemaPath))
                return ValidationResult.Error("Schema path is required (--schema)");

            if (string.IsNullOrWhiteSpace(OutputPath))
                return ValidationResult.Error("Output path is required (--output)");

            if (!File.Exists(SchemaPath))
                return ValidationResult.Error($"Schema file not found: {SchemaPath}");

            return ValidationResult.Success();
        }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.MarkupLine("[blue]📄 Reading schema...[/]");
            var schemaText = File.ReadAllText(settings.SchemaPath!);

            AnsiConsole.MarkupLine("[blue]🎨 Generating fragments...[/]");
            var generator = new FragmentGenerator();
            var fragments = generator.GenerateFragments(schemaText);

            AnsiConsole.MarkupLine("[blue]💾 Writing output...[/]");
            File.WriteAllText(settings.OutputPath!, fragments);

            AnsiConsole.MarkupLine($"[green]✅ Generated fragments in {settings.OutputPath}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
    }
}

/// <summary>
/// Generates GraphQL fragments from schema using text-based parsing.
/// </summary>
class FragmentGenerator
{
    private const int MaxDepth = 2;
    private readonly HashSet<string> _scalarTypes = new()
    {
        "String", "Int", "Float", "Boolean", "ID",
        "UUID", "Decimal", "LocalDate", "DateTime", "DateTimeOffset"
    };

    private readonly Dictionary<string, TypeDefinition> _types = new();

    public string GenerateFragments(string schemaText)
    {
        ParseSchema(schemaText);

        var sb = new StringBuilder();
        sb.AppendLine("# Auto-generated fragments - DO NOT EDIT MANUALLY");
        sb.AppendLine("# To regenerate: /generate-fragments");
        sb.AppendLine();

        // Generate fragment for TimeEntry only (the main type used in all queries)
        if (_types.TryGetValue("TimeEntry", out var timeEntryDef))
        {
            GenerateFragment(sb, timeEntryDef);
        }

        return sb.ToString();
    }

    private void ParseSchema(string schemaText)
    {
        // Parse type definitions using regex
        var typePattern = @"type\s+(\w+)\s*\{([^}]+)\}";
        var fieldPattern = @"(\w+):\s*(\[?[\w!]+\]?!?)";

        var typeMatches = Regex.Matches(schemaText, typePattern, RegexOptions.Multiline);

        foreach (Match typeMatch in typeMatches)
        {
            var typeName = typeMatch.Groups[1].Value;
            var fieldsText = typeMatch.Groups[2].Value;

            var typeDef = new TypeDefinition { Name = typeName };

            var fieldMatches = Regex.Matches(fieldsText, fieldPattern);
            foreach (Match fieldMatch in fieldMatches)
            {
                var fieldName = fieldMatch.Groups[1].Value;
                var fieldType = fieldMatch.Groups[2].Value;

                // Skip meta fields
                if (fieldName.StartsWith("__"))
                    continue;

                // Clean up type (remove ! and [])
                var cleanType = fieldType.Replace("!", "").Replace("[", "").Replace("]", "");

                typeDef.Fields.Add(new FieldDefinition
                {
                    Name = fieldName,
                    Type = cleanType,
                    IsList = fieldType.Contains("[")
                });
            }

            _types[typeName] = typeDef;
        }

        // Also parse enum types
        var enumPattern = @"enum\s+(\w+)\s*\{";
        var enumMatches = Regex.Matches(schemaText, enumPattern);
        foreach (Match enumMatch in enumMatches)
        {
            var enumName = enumMatch.Groups[1].Value;
            _scalarTypes.Add(enumName);
        }
    }

    private void GenerateFragment(StringBuilder sb, TypeDefinition type)
    {
        sb.AppendLine($"fragment {type.Name}Fields on {type.Name} {{");

        var visitedTypes = new HashSet<string>();
        GenerateFields(sb, type, 1, MaxDepth, visitedTypes);

        sb.AppendLine("}");
    }

    private void GenerateFields(StringBuilder sb, TypeDefinition type, int currentDepth, int maxDepth, HashSet<string> visitedTypes)
    {
        var indent = new string(' ', currentDepth * 2);

        foreach (var field in type.Fields.OrderBy(f => f.Name))
        {
            // Skip back-references (timeEntry field in TimeEntryTag)
            if (field.Name == "timeEntry" && type.Name == "TimeEntryTag")
                continue;

            // Scalar or enum - just include the field
            if (_scalarTypes.Contains(field.Type))
            {
                sb.AppendLine($"{indent}{field.Name}");
            }
            // Object type - recurse if depth allows
            else if (_types.TryGetValue(field.Type, out var fieldType))
            {
                // Special case: For TimeEntryTag, TagValue, and ProjectTag types,
                // always expand regardless of depth (for full tag structure)
                var isTagType = field.Type == "TimeEntryTag" || field.Type == "TagValue" || field.Type == "ProjectTag";
                var shouldExpand = isTagType || currentDepth < maxDepth;

                // Prevent infinite recursion
                if (shouldExpand && !visitedTypes.Contains(field.Type))
                {
                    visitedTypes.Add(field.Type);

                    sb.AppendLine($"{indent}{field.Name} {{");
                    GenerateFields(sb, fieldType, currentDepth + 1, maxDepth, visitedTypes);
                    sb.AppendLine($"{indent}}}");

                    visitedTypes.Remove(field.Type);
                }
            }
        }
    }
}

class TypeDefinition
{
    public string Name { get; set; } = "";
    public List<FieldDefinition> Fields { get; set; } = new();
}

class FieldDefinition
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsList { get; set; }
}
