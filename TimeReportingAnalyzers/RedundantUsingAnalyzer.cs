using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TimeReportingAnalyzers;

/// <summary>
/// Analyzer that detects redundant explicit using directives that are already defined as global usings.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RedundantUsingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TIME001";
    private const string Category = "Usage";

    private static readonly LocalizableString Title = "Redundant using directive";
    private static readonly LocalizableString MessageFormat = "Using directive '{0}' is redundant because it is already defined as a global using";
    private static readonly LocalizableString Description = "Remove explicit using directives that are already defined as global usings in the project file.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for using directive syntax
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;

        // Skip if this is already a global using directive (no need to analyze those)
        if (usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
        {
            return;
        }

        // Skip if this is a static using or an alias
        if (usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) || usingDirective.Alias != null)
        {
            return;
        }

        // Get the namespace name
        var namespaceName = usingDirective.Name?.ToString();
        if (string.IsNullOrEmpty(namespaceName))
        {
            return;
        }

        // Get the compilation to search for global usings
        var compilation = context.Compilation;
        if (compilation == null)
        {
            return;
        }

        // Search all syntax trees for global using directives
        var globalUsings = GetGlobalUsings(compilation);

        // Check if this namespace is in the global usings
        // (namespaceName is guaranteed non-null here due to the check above)
        if (globalUsings.Contains(namespaceName!))
        {
            var diagnostic = Diagnostic.Create(Rule, usingDirective.GetLocation(), namespaceName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Extracts all global using namespaces from the compilation.
    /// Global usings can come from:
    /// 1. Explicit 'global using' directives in any file
    /// 2. Project file '<Using Include="..." />' entries (added as syntax trees)
    /// 3. ImplicitUsings feature (added as syntax trees)
    /// </summary>
    private static ImmutableHashSet<string> GetGlobalUsings(Compilation compilation)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            var globalUsingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Where(u => u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                            && !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                            && u.Alias == null);

            foreach (var globalUsing in globalUsingDirectives)
            {
                var namespaceName = globalUsing.Name?.ToString();
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    // namespaceName is guaranteed non-null here
                    builder.Add(namespaceName!);
                }
            }
        }

        return builder.ToImmutable();
    }
}
