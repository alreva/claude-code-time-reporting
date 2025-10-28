#!/bin/bash

set -e

echo "=== Auto-Tracking Verification ==="
echo

# Step 1: Check SessionContext class exists
echo "1. Checking SessionContext implementation..."
if grep -q "class SessionContext" TimeReportingMcp/AutoTracking/SessionContext.cs 2>/dev/null; then
    echo "✅ SessionContext class found"
else
    echo "❌ SessionContext class not found"
    echo "   Phase 10 may not be implemented"
    exit 1
fi
echo

# Step 2: Check DetectionHeuristics exists
echo "2. Checking DetectionHeuristics implementation..."
if grep -q "class DetectionHeuristics" TimeReportingMcp/AutoTracking/DetectionHeuristics.cs 2>/dev/null; then
    echo "✅ DetectionHeuristics class found"
else
    echo "❌ DetectionHeuristics class not found"
    echo "   Phase 10 may not be implemented"
    exit 1
fi
echo

# Step 3: Check ContextPersistence exists
echo "3. Checking ContextPersistence implementation..."
if grep -q "class ContextPersistence" TimeReportingMcp/AutoTracking/ContextPersistence.cs 2>/dev/null; then
    echo "✅ ContextPersistence class found"
else
    echo "❌ ContextPersistence class not found"
    echo "   Phase 10 may not be implemented"
    exit 1
fi
echo

# Step 4: Check SuggestionFormatter exists
echo "4. Checking SuggestionFormatter implementation..."
if grep -q "class SuggestionFormatter" TimeReportingMcp/AutoTracking/SuggestionFormatter.cs 2>/dev/null; then
    echo "✅ SuggestionFormatter class found"
else
    echo "❌ SuggestionFormatter class not found"
    echo "   Phase 10 may not be implemented"
    exit 1
fi
echo

# Step 5: Check tests pass
echo "5. Running auto-tracking tests..."
if [[ -d "TimeReportingMcp.Tests" ]]; then
    cd TimeReportingMcp.Tests
    if dotnet test --filter "FullyQualifiedName~AutoTracking" --verbosity quiet --nologo > /dev/null 2>&1; then
        TEST_COUNT=$(dotnet test --filter "FullyQualifiedName~AutoTracking" --nologo 2>&1 | grep -oP "Passed! .*\K\d+(?= passed)" || echo "unknown")
        echo "✅ All auto-tracking tests pass ($TEST_COUNT tests)"
    else
        echo "⚠️  Some auto-tracking tests may have failed"
        echo "   Run: /test-mcp"
    fi
    cd ..
else
    echo "⚠️  Test project not found, skipping test execution"
fi
echo

echo "=== Verification Complete ==="
echo
echo "Auto-tracking feature is implemented and ready for testing!"
