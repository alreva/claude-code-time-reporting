# TimeReportingAnalyzers

Custom Roslyn analyzers for the Time Reporting System project.

## Analyzers

### TIME001: Redundant Using Directive

**Category:** Code Quality
**Severity:** Error

Detects and reports `using` directives that are redundant because they are already defined as global usings.

**Example:**

```csharp
// ❌ BAD - If System is already a global using
using System;

namespace MyNamespace
{
    public class MyClass { }
}
```

```csharp
// ✅ GOOD - No redundant using directive
namespace MyNamespace
{
    public class MyClass { }
}
```

**Rationale:** Keeping code clean by removing redundant imports that are already globally available via `Directory.Build.props` or `<Using>` elements in the project file.

## Installation

This analyzer is automatically applied to all projects in the solution via:

```xml
<ProjectReference Include="..\TimeReportingAnalyzers\TimeReportingAnalyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## Configuration

No configuration needed. The analyzer runs automatically during compilation and treats violations as errors (configured via `.editorconfig` or project settings).

## Development

- Built with .NET Standard 2.0 for maximum compatibility
- Uses Roslyn APIs for C# syntax analysis
- Tested with `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`
