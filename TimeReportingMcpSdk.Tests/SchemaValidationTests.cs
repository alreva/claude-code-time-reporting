using System.Diagnostics;

namespace TimeReportingMcpSdk.Tests;

/// <summary>
/// Tests that validate GraphQL schema alignment between API and MCP SDK.
/// This ensures the .graphql files in MCP SDK stay in sync with the actual API schema.
/// </summary>
public class SchemaValidationTests
{
    [Fact]
    public async Task McpSdkSchemaFile_MatchesGeneratedApiSchema()
    {
        // Execute the API schema export command
        var solutionRoot = Path.Combine(
            Path.GetDirectoryName(typeof(SchemaValidationTests).Assembly.Location)!,
            "..", "..", "..",  // Navigate up from bin/Debug/net10.0 to TimeReportingMcpSdk.Tests
            ".."                // Navigate up to solution root
        );
        solutionRoot = Path.GetFullPath(solutionRoot);

        var apiDllPath = Path.Combine(solutionRoot, "TimeReportingApi", "bin", "Debug", "net10.0", "TimeReportingApi.dll");

        if (!File.Exists(apiDllPath))
        {
            Assert.Fail($"❌ API DLL not found at: {apiDllPath}\n\n" +
                       $"The API project must be built first.\n" +
                       $"Run: /build-api");
        }

        // Run the schema export command and capture stdout
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{apiDllPath}\" export-schema",
                WorkingDirectory = solutionRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var generatedSchema = await process.StandardOutput.ReadToEndAsync();
        var errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Assert.Fail($"❌ Schema export failed with exit code {process.ExitCode}\n\n" +
                       $"Errors: {errors}");
        }

        // Read the MCP SDK schema file
        var mcpSchemaPath = Path.Combine(
            Path.GetDirectoryName(typeof(SchemaValidationTests).Assembly.Location)!,
            "..", "..", "..",  // Navigate up from bin/Debug/net10.0 to TimeReportingMcpSdk.Tests
            "..",               // Navigate up to solution root
            "TimeReportingMcpSdk",
            "schema.graphql"
        );

        mcpSchemaPath = Path.GetFullPath(mcpSchemaPath);

        // Assert - Schemas must match exactly
        if (!File.Exists(mcpSchemaPath))
        {
            Assert.Fail($"❌ MCP SDK schema file not found at: {mcpSchemaPath}\n\n" +
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
                $"❌ MCP SDK schema is out of sync with API!\n\n" +
                $"The schema file in TimeReportingMcpSdk/schema.graphql doesn't match the current API schema.\n" +
                $"This will cause StrawberryShake to generate incorrect types.\n\n" +
                $"DEBUG INFO:\n" +
                $"Generated length: {normalizedGenerated.Length}\n" +
                $"MCP length: {normalizedMcp.Length}\n" +
                $"Difference: {lengthDiff} characters\n" +
                $"First 100 chars (generated): {first100Generated}\n" +
                $"First 100 chars (MCP): {first100Mcp}\n\n" +
                $"TO FIX:\n" +
                $"1. Export the current API schema:\n" +
                $"   dotnet run --project TimeReportingApi -- export-schema > TimeReportingMcpSdk/schema.graphql\n" +
                $"2. Rebuild MCP SDK to regenerate StrawberryShake types:\n" +
                $"   dotnet build TimeReportingMcpSdk/TimeReportingMcpSdk.csproj\n" +
                $"3. Re-run tests:\n" +
                $"   /test\n\n" +
                $"MCP SDK schema location: {mcpSchemaPath}\n"
            );
        }

        // If we get here, schemas match!
        Assert.True(true, "✅ MCP SDK schema is in sync with API schema");
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
