
namespace TimeReportingMcp.Tests;

/// <summary>
/// Tests that validate GraphQL schema alignment between API and MCP.
/// This ensures the .graphql files in MCP stay in sync with the actual API schema.
/// </summary>
public class SchemaValidationTests
{
    [Fact]
    public async Task McpSchemaFile_MatchesGeneratedApiSchema()
    {
        // Get the API-generated schema from the build output
        var apiSchemaPath = Path.Combine(
            Path.GetDirectoryName(typeof(SchemaValidationTests).Assembly.Location)!,
            "..", "..", "..",  // Navigate up from bin/Debug/net10.0 to TimeReportingMcp.Tests
            "..",               // Navigate up to solution root
            "TimeReportingApi",
            "bin", "Debug", "net10.0",
            "schema.graphql"
        );

        apiSchemaPath = Path.GetFullPath(apiSchemaPath);

        if (!File.Exists(apiSchemaPath))
        {
            Assert.Fail($"❌ API schema file not found at: {apiSchemaPath}\n\n" +
                       $"The API project must be built first to export the schema.\n" +
                       $"Run: /build-api");
        }

        var generatedSchema = await File.ReadAllTextAsync(apiSchemaPath);

        // Read the MCP schema file
        var mcpSchemaPath = Path.Combine(
            Path.GetDirectoryName(typeof(SchemaValidationTests).Assembly.Location)!,
            "..", "..", "..",  // Navigate up from bin/Debug/net10.0 to TimeReportingMcp.Tests
            "..",               // Navigate up to solution root
            "TimeReportingMcp",
            "schema.graphql"
        );

        mcpSchemaPath = Path.GetFullPath(mcpSchemaPath);

        // Assert - Schemas must match exactly
        if (!File.Exists(mcpSchemaPath))
        {
            Assert.Fail($"❌ MCP schema file not found at: {mcpSchemaPath}\n\n" +
                       $"This file is required for StrawberryShake code generation.\n" +
                       $"Generated schema:\n\n{generatedSchema}");
        }

        var mcpSchema = await File.ReadAllTextAsync(mcpSchemaPath);

        // Normalize line endings and whitespace for comparison
        var normalizedGenerated = NormalizeSchema(generatedSchema);
        var normalizedMcp = NormalizeSchema(mcpSchema);

        if (normalizedGenerated != normalizedMcp)
        {
            // Debug: show length difference
            var lengthDiff = normalizedGenerated.Length - normalizedMcp.Length;
            var first100Generated = normalizedGenerated.Length > 100 ? normalizedGenerated.Substring(0, 100) : normalizedGenerated;
            var first100Mcp = normalizedMcp.Length > 100 ? normalizedMcp.Substring(0, 100) : normalizedMcp;

            // Provide helpful error message with fix instructions
            Assert.Fail(
                $"❌ MCP schema is out of sync with API!\n\n" +
                $"The schema file in TimeReportingMcp/schema.graphql doesn't match the current API schema.\n" +
                $"This will cause StrawberryShake to generate incorrect types.\n\n" +
                $"DEBUG INFO:\n" +
                $"Generated length: {normalizedGenerated.Length}\n" +
                $"MCP length: {normalizedMcp.Length}\n" +
                $"Difference: {lengthDiff} characters\n" +
                $"First 100 chars (generated): {first100Generated}\n" +
                $"First 100 chars (MCP): {first100Mcp}\n\n" +
                $"TO FIX:\n" +
                $"1. Build the API project first: /build-api\n" +
                $"2. Copy the generated schema:\n" +
                $"   cp TimeReportingApi/bin/Debug/net10.0/schema.graphql TimeReportingMcp/schema.graphql\n" +
                $"3. Rebuild MCP: /build-mcp\n" +
                $"4. Re-run tests: /test\n\n" +
                $"Expected schema location: {mcpSchemaPath}\n"
            );
        }

        // If we get here, schemas match!
        Assert.True(true, "✅ MCP schema is in sync with API schema");
    }

    private static string NormalizeSchema(string schema)
    {
        // Normalize line endings and trim whitespace for reliable comparison
        return schema
            .Replace("\r\n", "\n")  // Normalize Windows line endings
            .Replace("\r", "\n")    // Normalize old Mac line endings
            .Trim();                 // Remove leading/trailing whitespace
    }
}
