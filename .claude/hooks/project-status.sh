#!/bin/bash
# Project Status Report - Fits terminal screen
set -euo pipefail

# Colors for terminal output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Box drawing characters
H_LINE="━"
V_LINE="┃"
TL_CORNER="┏"
TR_CORNER="┓"
BL_CORNER="┗"
BR_CORNER="┛"
T_JOIN="┳"
B_JOIN="┻"
L_JOIN="┣"
R_JOIN="┫"

# Print header
print_header() {
    local width=78
    echo -e "${BOLD}${CYAN}"
    printf "${TL_CORNER}%${width}s${TR_CORNER}\n" | tr ' ' "${H_LINE}"
    printf "${V_LINE}%-${width}s${V_LINE}\n" "  Time Reporting System - Project Status"
    printf "${BL_CORNER}%${width}s${BR_CORNER}\n" | tr ' ' "${H_LINE}"
    echo -e "${NC}"
}

# Section divider
print_section() {
    echo -e "${BOLD}${BLUE}▶ $1${NC}"
}

# Git status
show_git_status() {
    print_section "Git Status"
    local branch=$(git branch --show-current 2>/dev/null || echo "unknown")
    local last_commit=$(git log -1 --oneline 2>/dev/null || echo "No commits")
    local changes=$(git status --short 2>/dev/null | wc -l | tr -d ' ')

    echo -e "  Branch:      ${GREEN}${branch}${NC}"
    echo -e "  Last commit: ${last_commit}"

    if [ "$changes" -eq 0 ]; then
        echo -e "  Changes:     ${GREEN}✓ Clean working tree${NC}"
    else
        echo -e "  Changes:     ${YELLOW}${changes} file(s) modified${NC}"
    fi
    echo
}

# Build status
show_build_status() {
    print_section "Build Status"

    # Check if build artifacts exist
    if [ -f "TimeReportingApi/bin/Debug/net8.0/TimeReportingApi.dll" ]; then
        local build_date=$(stat -f "%Sm" -t "%Y-%m-%d %H:%M" "TimeReportingApi/bin/Debug/net8.0/TimeReportingApi.dll" 2>/dev/null || echo "unknown")
        echo -e "  API Build:   ${GREEN}✓ Success${NC} (${build_date})"
    else
        echo -e "  API Build:   ${RED}✗ Not built${NC}"
    fi

    if [ -f "TimeReportingApi.Tests/bin/Debug/net8.0/TimeReportingApi.Tests.dll" ]; then
        echo -e "  Tests Build: ${GREEN}✓ Success${NC}"
    else
        echo -e "  Tests Build: ${RED}✗ Not built${NC}"
    fi
    echo
}

# Database status
show_database_status() {
    print_section "Database Status"

    # Check if PostgreSQL container is running
    if podman ps --format "{{.Names}}" 2>/dev/null | grep -q "time-reporting-db"; then
        echo -e "  PostgreSQL:  ${GREEN}✓ Running${NC} (Container: time-reporting-db)"

        # Try to connect and get table count
        local table_count=$(podman exec time-reporting-db psql -U postgres -d time_reporting -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public';" 2>/dev/null | tr -d ' ' || echo "?")
        echo -e "  Tables:      ${table_count} tables in 'time_reporting' database"
    else
        echo -e "  PostgreSQL:  ${YELLOW}○ Not running${NC}"
        echo -e "  Hint:        Run /db-start to start the database"
    fi
    echo
}

# Task progress
show_task_progress() {
    print_section "Task Progress"

    if [ -f "docs/TASK-INDEX.md" ]; then
        # Count completed vs total tasks
        local completed=$(grep -c "✅" docs/TASK-INDEX.md 2>/dev/null)
        local total=$(grep -E "^\- \[" docs/TASK-INDEX.md 2>/dev/null | wc -l)

        # Trim whitespace using parameter expansion
        completed=$(echo $completed | xargs)
        total=$(echo $total | xargs)

        # Default to 0 if empty
        completed=${completed:-0}
        total=${total:-0}

        if [ "$total" -gt 0 ] 2>/dev/null; then
            local percent=$((completed * 100 / total))
            echo -e "  Progress:    ${completed}/${total} tasks completed (${percent}%)"

            # Show progress bar
            local bar_width=40
            local filled=$((percent * bar_width / 100))
            local empty=$((bar_width - filled))
            printf "  ["
            printf "%${filled}s" | tr ' ' '█'
            printf "%${empty}s" | tr ' ' '░'
            printf "]\n"

            # Check git sync status for TASK-INDEX.md (bidirectional)
            local sync_issue=""

            # Check 1: TASK-INDEX.md has uncommitted changes
            if ! git diff --quiet docs/TASK-INDEX.md 2>/dev/null || ! git diff --cached --quiet docs/TASK-INDEX.md 2>/dev/null; then
                sync_issue="Task list has uncommitted changes"
            fi

            # Check 2: Recent commits mention completed tasks not marked in TASK-INDEX.md
            local recent_commits=$(git log -10 --oneline --grep="Complete Task" 2>/dev/null || echo "")
            if [ -n "$recent_commits" ]; then
                # Extract task numbers from commit messages (e.g., "Complete Task 1.2" -> "1.2")
                local committed_tasks=$(echo "$recent_commits" | grep -oE "Task [0-9]+\.[0-9]+" | sed 's/Task //' | sort -u)

                if [ -n "$committed_tasks" ]; then
                    local unmarked_tasks=""
                    while IFS= read -r task_num; do
                        # Check if this task is marked with ✅ in TASK-INDEX.md
                        if ! grep -q "Task ${task_num}.*✅" docs/TASK-INDEX.md 2>/dev/null; then
                            unmarked_tasks="${unmarked_tasks}${task_num} "
                        fi
                    done <<< "$committed_tasks"

                    if [ -n "$unmarked_tasks" ]; then
                        if [ -n "$sync_issue" ]; then
                            sync_issue="${sync_issue}; Tasks committed but not marked: ${unmarked_tasks}"
                        else
                            sync_issue="Tasks committed but not marked complete: ${unmarked_tasks}"
                        fi
                    fi
                fi
            fi

            # Display sync status
            if [ -z "$sync_issue" ]; then
                echo -e "  Git Sync:    ${GREEN}✓ Task list in sync with git${NC}"
            else
                echo -e "  Git Sync:    ${YELLOW}⚠ ${sync_issue}${NC}"
            fi
        else
            echo -e "  Progress:    ${YELLOW}Task tracking not initialized${NC}"
        fi
    else
        echo -e "  Status:      ${YELLOW}Task index not found${NC}"
    fi
    echo
}

# Project statistics
show_project_stats() {
    print_section "Project Statistics"

    # Count C# files
    local cs_files=$(find TimeReportingApi* -name "*.cs" 2>/dev/null | wc -l | tr -d ' ')

    # Count lines of code (excluding bin/obj)
    local loc=$(find TimeReportingApi* -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" 2>/dev/null | xargs wc -l 2>/dev/null | tail -1 | awk '{print $1}' || echo "0")

    # Count test files
    local test_files=$(find TimeReportingApi.Tests -name "*Tests.cs" 2>/dev/null | wc -l | tr -d ' ')

    echo -e "  C# Files:    ${cs_files} files"
    echo -e "  Lines:       ${loc} lines of code"
    echo -e "  Tests:       ${test_files} test files"
    echo
}

# Central package management
show_package_management() {
    print_section "Configuration"

    if [ -f "global.json" ]; then
        local sdk_version=$(grep '"version"' global.json | sed 's/.*: *"\(.*\)".*/\1/')
        echo -e "  SDK:         ${sdk_version}"
    fi

    if [ -f "Directory.Packages.props" ]; then
        local pkg_count=$(grep -c "PackageVersion Include" Directory.Packages.props 2>/dev/null || echo "0")
        echo -e "  Packages:    ${pkg_count} centrally managed"
    fi

    echo -e "  Framework:   net8.0"
    echo -e "  Warnings:    ${GREEN}Treated as errors${NC} (zero-warning policy)"
    echo
}

# Main execution
main() {
    clear
    print_header
    show_git_status
    show_build_status
    show_database_status
    show_task_progress
    show_project_stats
    show_package_management
    echo -e "${CYAN}Run /help for available commands${NC}"
}

main
