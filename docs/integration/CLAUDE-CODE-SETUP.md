# Claude Code Setup Guide

## Overview

This guide walks you through configuring Claude Code to work with the Time Reporting MCP Server, enabling natural language time tracking through Claude Code.

---

## Prerequisites

Before starting, ensure you have:

- [ ] **.NET 8 SDK** installed (`dotnet --version` shows 8.0.x)
- [ ] **GraphQL API** running (via Docker/Podman)
- [ ] **PostgreSQL database** seeded with projects
- [ ] **Claude Code** installed on your machine

**Verify Prerequisites:**

```bash
# Check .NET 8 SDK
dotnet --version

# Check GraphQL API
curl http://localhost:5001/health

# Check database
/db-psql
SELECT COUNT(*) FROM projects;
```

---

## Quick Start

### 1. Start the GraphQL API and Database

```bash
cd /path/to/time-reporting-system
/deploy  # Or: podman compose up -d
```

**Verify API is running:**

```bash
curl http://localhost:5001/health
# Expected: {"status":"healthy"}
```

### 2. Generate Bearer Token

Generate a secure random token for authentication:

```bash
openssl rand -base64 32
```

**Example output:**
```
Zq8X9vKpL2mN4wR7tY5uI3oP1aS6dF8hG0jK9lM2nB4=
```

**Save this token** - you'll need it for both the API and Claude Code configuration.

### 3. Update API Configuration

Edit `.env` file in the project root:

```env
Authentication__BearerToken=Zq8X9vKpL2mN4wR7tY5uI3oP1aS6dF8hG0jK9lM2nB4=
```

Restart the API to apply changes:

```bash
/deploy  # Or: podman compose restart graphql-api
```

### 4. Configure Claude Code

The configuration file location depends on your operating system:

#### macOS/Linux

**Configuration file location:**
```
~/.config/claude-code/config.json
```

**Create/edit the file:**

```bash
mkdir -p ~/.config/claude-code
nano ~/.config/claude-code/config.json
```

**Add the following configuration:**

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/yourusername/path/to/TimeReportingMcp/TimeReportingMcp.csproj"
      ],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Authentication__BearerToken": "Zq8X9vKpL2mN4wR7tY5uI3oP1aS6dF8hG0jK9lM2nB4="
      }
    }
  }
}
```

**Example with actual path (macOS):**

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/john/projects/time-reporting-system/TimeReportingMcp/TimeReportingMcp.csproj"
      ],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Authentication__BearerToken": "Zq8X9vKpL2mN4wR7tY5uI3oP1aS6dF8hG0jK9lM2nB4="
      }
    }
  }
}
```

#### Windows

**Configuration file location:**
```
%APPDATA%\claude-code\config.json
```

**Create/edit the file:**

```powershell
mkdir %APPDATA%\claude-code
notepad %APPDATA%\claude-code\config.json
```

**Add the following configuration:**

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Users\\YourUsername\\Projects\\time-reporting-system\\TimeReportingMcp\\TimeReportingMcp.csproj"
      ],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Authentication__BearerToken": "Zq8X9vKpL2mN4wR7tY5uI3oP1aS6dF8hG0jK9lM2nB4="
      }
    }
  }
}
```

**Note:** Use double backslashes `\\` in Windows paths.

### 5. Restart Claude Code

Close and reopen Claude Code to load the new configuration.

### 6. Verify MCP Server is Connected

In Claude Code, ask:

```
"What tools do you have access to?"
```

**Expected response should include:**

- `log_time` - Create time entries
- `query_time_entries` - Search and filter time entries
- `update_time_entry` - Update existing entries
- `move_task_to_project` - Move entries between projects
- `delete_time_entry` - Delete entries
- `get_available_projects` - List projects and their tasks
- `submit_time_entry` - Submit entries for approval

### 7. Test with a Simple Command

```
"Get available projects"
```

**Expected response:**

```
Available Projects:

1. INTERNAL - Internal Development
   Tasks: Development, Bug Fixing, Code Review, Documentation, Meetings

2. CLIENT-A - Client A Project
   Tasks: Development, Bug Fixing, Testing, Deployment

3. CLIENT-B - Client B Project
   Tasks: Development, Bug Fixing, Code Review, Testing
```

---

## Configuration Reference

### Required Fields

| Field | Description | Example Value |
|-------|-------------|---------------|
| `command` | Command to run MCP server | `"dotnet"` |
| `args` | Arguments for the command | `["run", "--project", "path/to/.csproj"]` |
| `GRAPHQL_API_URL` | GraphQL API endpoint | `"http://localhost:5001/graphql"` |
| `Authentication__BearerToken` | Authentication token | `"Zq8X9v..."` (32-byte base64) |

### Important Notes

- **Use Absolute Paths**: The project path must be absolute, not relative
- **Token Security**: Never commit the actual `Authentication__BearerToken` to version control
- **Token Matching**: The token in Claude Code config must match the token in API `.env` file
- **Path Format**:
  - macOS/Linux: Use forward slashes `/`
  - Windows: Use double backslashes `\\`

---

## Environment Variables

### GRAPHQL_API_URL

**Purpose:** Specifies the GraphQL API endpoint URL

**Default:** `http://localhost:5001/graphql`

**Production Example:**
```json
"GRAPHQL_API_URL": "https://api.yourcompany.com/time-reporting/graphql"
```

### Authentication__BearerToken

**Purpose:** Authentication token for API access

**Generation:**
```bash
openssl rand -base64 32
```

**Security:**
- Generate a unique token for each environment (dev, staging, production)
- Never commit tokens to version control
- Rotate tokens periodically
- Use environment variables in CI/CD pipelines

---

## Troubleshooting

### Problem: MCP server doesn't appear in Claude Code tools list

**Possible Causes & Solutions:**

**1. .NET 8 SDK not installed or not in PATH**

```bash
# Check installation
dotnet --version

# If not found, install:
# macOS:
brew install dotnet-sdk

# Windows: Download from https://dotnet.microsoft.com/download/dotnet/8.0
# Linux: Follow distribution-specific instructions
```

**2. MCP Server build fails**

```bash
# Test build manually
cd TimeReportingMcp
dotnet build

# If build fails, check for errors and fix them
# Run /build-mcp for guided build process
```

**3. Configuration file path is incorrect**

```bash
# macOS/Linux
ls -la ~/.config/claude-code/config.json

# Windows
dir %APPDATA%\claude-code\config.json

# If file doesn't exist, create it
```

**4. Invalid JSON in configuration file**

Use a JSON validator to check your config:
```bash
# macOS/Linux
cat ~/.config/claude-code/config.json | jq .

# If jq not installed:
# macOS: brew install jq
# Linux: apt-get install jq / yum install jq
```

**5. Claude Code not restarted after config changes**

- Completely close Claude Code (not just minimize)
- Reopen Claude Code
- Wait 10-15 seconds for MCP servers to initialize

---

### Problem: MCP tools return authentication errors (401 Unauthorized)

**Possible Causes & Solutions:**

**1. Bearer token mismatch**

```bash
# Check API .env file
cat .env | grep Authentication__BearerToken

# Check Claude Code config
cat ~/.config/claude-code/config.json | jq '.mcpServers."time-reporting".env.Authentication__BearerToken'

# Tokens MUST match exactly
```

**2. API not running or not restarted after token change**

```bash
# Check API health
curl http://localhost:5001/health

# If not responding, restart API
/deploy
```

**3. API logs show authentication errors**

```bash
# Check API logs
podman compose logs graphql-api

# Look for authentication-related errors
podman compose logs graphql-api | grep -i "auth\|401\|unauthorized"
```

**Solution:**
1. Verify tokens match in both `.env` and Claude Code config
2. Restart API: `/deploy`
3. Restart Claude Code
4. Test again

---

### Problem: MCP server connects but tools fail with "Connection refused"

**Possible Causes & Solutions:**

**1. GraphQL API URL is incorrect**

```bash
# Verify API is accessible
curl http://localhost:5001/graphql

# Should return GraphQL schema or error message (not "connection refused")
```

**2. API running on different port**

```bash
# Check what port API is actually using
podman ps | grep graphql-api

# Update Claude Code config if port is different
```

**3. Firewall blocking connection**

```bash
# Test connectivity
telnet localhost 5000

# macOS firewall:
# System Preferences → Security & Privacy → Firewall

# Windows firewall:
# Control Panel → Windows Defender Firewall → Allow an app
```

**4. Podman/Docker networking issues**

```bash
# Check container status
podman ps

# Check port forwarding
podman port time-reporting-api

# Restart containers
/deploy
```

---

### Problem: "dotnet: command not found" error

**Solution:**

**macOS:**
```bash
brew install dotnet-sdk
```

**Windows:**
1. Download .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
2. Run installer
3. Restart terminal/IDE

**Linux (Ubuntu/Debian):**
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

**Verify installation:**
```bash
dotnet --version
# Should output: 8.0.x
```

**Note:** Restart terminal and Claude Code after installing .NET SDK

---

### Problem: Tools work but return empty/incorrect data

**Possible Causes & Solutions:**

**1. Database not seeded**

```bash
# Seed test projects
/seed-db

# Verify projects exist
/db-psql
SELECT code, name FROM projects;
```

**2. Database connection issues**

```bash
# Check database is running
/db-start

# Test connection
/db-psql
SELECT 1;
```

**3. API not connected to database**

```bash
# Check API logs for database connection errors
podman compose logs graphql-api | grep -i "database\|postgres\|connection"

# Check .env file has correct connection string
cat .env | grep DATABASE_URL
```

---

## Advanced Configuration

### Custom API Port

If your API runs on a different port (e.g., 8080):

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/TimeReportingMcp.csproj"],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:8080/graphql",
        "Authentication__BearerToken": "your-token-here"
      }
    }
  }
}
```

### Remote API (Production)

For connecting to a remote production API:

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/TimeReportingMcp.csproj"],
      "env": {
        "GRAPHQL_API_URL": "https://api.yourcompany.com/time-reporting/graphql",
        "Authentication__BearerToken": "production-token-here",
        "SSL_VERIFY": "true"
      }
    }
  }
}
```

### Multiple Environments

You can configure multiple MCP servers for different environments:

```json
{
  "mcpServers": {
    "time-reporting-dev": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/TimeReportingMcp.csproj"],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Authentication__BearerToken": "dev-token"
      }
    },
    "time-reporting-prod": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/TimeReportingMcp.csproj"],
      "env": {
        "GRAPHQL_API_URL": "https://api.yourcompany.com/graphql",
        "Authentication__BearerToken": "prod-token"
      }
    }
  }
}
```

---

## Verification Checklist

Use this checklist to verify your setup:

- [ ] .NET 8 SDK installed and in PATH
- [ ] GraphQL API running and healthy
- [ ] Database seeded with projects
- [ ] Bearer token generated (32-byte base64)
- [ ] Bearer token configured in API `.env` file
- [ ] API restarted after token configuration
- [ ] Claude Code configuration file created
- [ ] Project path in config is absolute (not relative)
- [ ] Bearer token in Claude Code config matches API token
- [ ] Claude Code restarted after configuration
- [ ] MCP tools appear in Claude Code
- [ ] Test command works (e.g., "Get available projects")

---

## Next Steps

After successful setup:

1. **Test Basic Operations:**
   - Get available projects
   - Log time entry
   - Query entries
   - Update entry

2. **Read Workflow Guides:**
   - [Manual Time Logging Workflow](../workflows/MANUAL-TIME-LOGGING.md)
   - [Auto-tracking Test Guide](../workflows/AUTO-TRACKING-TEST.md)

3. **Run E2E Tests:**
   - Follow [E2E Test Scenarios](../../tests/e2e/README.md)

4. **Explore Features:**
   - Project migration
   - Approval workflow
   - Auto-tracking suggestions

---

## Support

**Documentation:**
- [MCP Tools Reference](../prd/mcp-tools.md)
- [API Specification](../prd/api-specification.md)
- [Project README](../../README.md)

**Troubleshooting:**
- Check API logs: `podman compose logs graphql-api`
- Check database logs: `/db-logs`
- Run verification script: `./tests/integration/verify-mcp-connection.sh`

**Common Issues:**
- [Troubleshooting section](#troubleshooting) above
- [GitHub Issues](https://github.com/your-repo/time-reporting/issues)

---

**Congratulations!** You've successfully configured Claude Code with the Time Reporting MCP Server. Start tracking your time with natural language commands!
