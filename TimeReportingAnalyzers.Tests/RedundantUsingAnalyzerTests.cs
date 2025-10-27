using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace TimeReportingAnalyzers.Tests;

public class RedundantUsingAnalyzerTests
{
    [Fact]
    public async Task WhenExplicitUsingMatchesGlobalUsing_ShouldReportDiagnostic()
    {
        // Arrange - Code with explicit using that matches global using
        const string testCode = @"
using System.Net;

namespace TestNamespace
{
    public class TestClass
    {
    }
}";

        // Expected diagnostic
        var expected = new DiagnosticResult(RedundantUsingAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .WithSpan(2, 1, 2, 18) // Line 2, columns 1-18 ("using System.Net;")
            .WithArguments("System.Net");

        // Act & Assert
        await VerifyAnalyzerAsync(testCode, new[] { "System.Net" }, expected);
    }

    [Fact]
    public async Task WhenExplicitUsingDoesNotMatchGlobalUsing_ShouldNotReportDiagnostic()
    {
        // Arrange - Code with explicit using that does NOT match global using
        const string testCode = @"
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestClass
    {
    }
}";

        // Act & Assert - No diagnostics expected
        await VerifyAnalyzerAsync(testCode, new[] { "System.Net" });
    }

    [Fact]
    public async Task WhenNoExplicitUsings_ShouldNotReportDiagnostic()
    {
        // Arrange - Code with no explicit usings
        const string testCode = @"
namespace TestNamespace
{
    public class TestClass
    {
    }
}";

        // Act & Assert - No diagnostics expected
        await VerifyAnalyzerAsync(testCode, new[] { "System.Net" });
    }

    [Fact]
    public async Task WhenStaticUsing_ShouldNotReportDiagnostic()
    {
        // Arrange - Code with static using (should be ignored)
        const string testCode = @"
using static System.Math;

namespace TestNamespace
{
    public class TestClass
    {
    }
}";

        // Act & Assert - No diagnostics expected (static usings are ignored)
        await VerifyAnalyzerAsync(testCode, new[] { "System.Math" });
    }

    [Fact]
    public async Task WhenUsingAlias_ShouldNotReportDiagnostic()
    {
        // Arrange - Code with using alias (should be ignored)
        const string testCode = @"
using Net = System.Net;

namespace TestNamespace
{
    public class TestClass
    {
    }
}";

        // Act & Assert - No diagnostics expected (aliases are ignored)
        await VerifyAnalyzerAsync(testCode, new[] { "System.Net" });
    }

    [Fact]
    public async Task WhenMultipleRedundantUsings_ShouldReportMultipleDiagnostics()
    {
        // Arrange - Code with multiple redundant usings
        const string testCode = @"
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace TestNamespace
{
    public class TestClass
    {
    }
}";

        // Expected diagnostics for both redundant usings
        var expected = new[]
        {
            new DiagnosticResult(RedundantUsingAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithSpan(2, 1, 2, 18)
                .WithArguments("System.Net"),
            new DiagnosticResult(RedundantUsingAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithSpan(3, 1, 3, 37)
                .WithArguments("Microsoft.EntityFrameworkCore")
        };

        // Act & Assert
        await VerifyAnalyzerAsync(testCode, new[] { "System.Net", "Microsoft.EntityFrameworkCore" }, expected);
    }

    /// <summary>
    /// Helper method to verify analyzer behavior
    /// </summary>
    private static async Task VerifyAnalyzerAsync(string source, string[] globalUsings, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<RedundantUsingAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            CompilerDiagnostics = Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None
        };

        // Add global usings as a separate syntax tree (mimics how MSBuild adds them)
        if (globalUsings.Length > 0)
        {
            var globalUsingStatements = string.Join("\n", globalUsings.Select(ns => $"global using {ns};"));
            test.TestState.Sources.Add(("GlobalUsings.cs", globalUsingStatements));
        }

        // Add expected diagnostics
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
