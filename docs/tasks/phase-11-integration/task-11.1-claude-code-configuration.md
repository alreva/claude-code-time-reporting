# Task 11.1: Claude Code Configuration

**Phase:** 11 - Integration & Testing
**Estimated Time:** 30 minutes
**Prerequisites:** Phase 10 complete (Auto-tracking implemented), MCP Server built
**Status:** Pending

---

## Objective

Create MCP server configuration for Claude Code, enabling users to connect Claude Code to the Time Reporting System via the MCP protocol.

---

## Acceptance Criteria

- [ ] Example `claude_desktop_config.json` file created with all required settings
- [ ] Configuration includes correct paths for .NET 8 and MCP Server project
- [ ] Environment variables documented for `GRAPHQL_API_URL` and `Authentication__BearerToken`
- [ ] Instructions provided for different operating systems (macOS, Windows, Linux)
- [ ] Configuration tested with Claude Code (MCP server appears in tools list)
- [ ] Troubleshooting section added for common configuration issues

---

## Implementation Steps

### Step 1: Create Example Configuration File

Create `docs/integration/claude_desktop_config.json.example`:

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/TimeReportingMcp/TimeReportingMcp.csproj"
      ],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Authentication__BearerToken": "your-bearer-token-here"
      }
    }
  }
}
```

**Key Points:**
- Use absolute paths (not relative) for project path
- `Authentication__BearerToken` must match the token configured in the GraphQL API
- `GRAPHQL_API_URL` should point to the running GraphQL API (default: `http://localhost:5001/graphql`)

### Step 2: Document Platform-Specific Paths

Create `docs/integration/CLAUDE-CODE-SETUP.md`:

#### macOS/Linux Configuration

**Location:** `~/.config/claude-code/config.json`

**Example path:**
```json
"/Users/yourusername/projects/time-reporting-system/TimeReportingMcp/TimeReportingMcp.csproj"
```

#### Windows Configuration

**Location:** `%APPDATA%\claude-code\config.json`

**Example path:**
```json
"C:\\Users\\YourUsername\\Projects\\time-reporting-system\\TimeReportingMcp\\TimeReportingMcp.csproj"
```

**Note:** Use double backslashes `\\` in Windows paths.

### Step 3: Document Environment Variables

**Required Variables:**

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `GRAPHQL_API_URL` | GraphQL API endpoint URL | `http://localhost:5001/graphql` |
| `Authentication__BearerToken` | Authentication token for API | `your-generated-token-here` |

**Generating a Bearer Token:**

```bash
# Generate a secure random token (32 bytes base64 encoded)
openssl rand -base64 32
```

**Example output:**
```
Zq8X9vKpL2mN4wR7tY5uI3oP1aS6dF8hG0jK9lM2nB4=
```

**⚠️ Security Note:** Never commit the actual `Authentication__BearerToken` to version control. Use this token in:
1. Claude Code config file (`.config/claude-code/config.json`)
2. GraphQL API configuration (`.env` file or `appsettings.json`)

### Step 4: Create Quick Start Guide

Add to `docs/integration/CLAUDE-CODE-SETUP.md`:

#### Quick Start

**1. Start the GraphQL API and Database:**

```bash
cd /path/to/time-reporting-system
/deploy  # Or: docker-compose up -d
```

**2. Verify API is running:**

```bash
curl http://localhost:5001/health
# Expected: {"status":"healthy"}
```

**3. Generate Bearer Token:**

```bash
openssl rand -base64 32
```

**4. Update API Configuration:**

Edit `.env` file:
```env
Authentication__BearerToken=<your-generated-token>
```

Restart API:
```bash
/deploy  # Or: docker-compose restart graphql-api
```

**5. Configure Claude Code:**

Edit `~/.config/claude-code/config.json` (macOS/Linux) or `%APPDATA%\claude-code\config.json` (Windows):

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/TimeReportingMcp/TimeReportingMcp.csproj"
      ],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Authentication__BearerToken": "<same-token-from-step-3>"
      }
    }
  }
}
```

**6. Restart Claude Code:**

Close and reopen Claude Code to load the new configuration.

**7. Verify MCP Server is Connected:**

In Claude Code, check for available tools:
```
"What tools do you have access to?"
```

Expected tools:
- `log_time`
- `query_time_entries`
- `update_time_entry`
- `move_task_to_project`
- `delete_time_entry`
- `get_available_projects`
- `submit_time_entry`

### Step 5: Troubleshooting Guide

Add troubleshooting section to `docs/integration/CLAUDE-CODE-SETUP.md`:

#### Troubleshooting

**Problem:** MCP server doesn't appear in Claude Code tools list

**Solutions:**
1. Verify .NET 8 SDK is installed:
   ```bash
   dotnet --version
   # Expected: 8.0.x
   ```

2. Check MCP Server builds successfully:
   ```bash
   cd TimeReportingMcp
   dotnet build
   # Should complete without errors
   ```

3. Verify configuration file path is correct:
   - macOS/Linux: `~/.config/claude-code/config.json`
   - Windows: `%APPDATA%\claude-code\config.json`

4. Check Claude Code logs for errors (location varies by platform)

---

**Problem:** MCP tools return authentication errors

**Solutions:**
1. Verify `Authentication__BearerToken` in Claude Code config matches the token in API `.env` file
2. Verify GraphQL API is running:
   ```bash
   curl http://localhost:5001/health
   ```
3. Check API logs for authentication errors:
   ```bash
   docker-compose logs graphql-api
   ```

---

**Problem:** MCP server connects but tools fail with "Connection refused"

**Solutions:**
1. Verify `GRAPHQL_API_URL` is correct (default: `http://localhost:5001/graphql`)
2. Verify GraphQL API is accessible from your machine:
   ```bash
   curl http://localhost:5001/graphql
   ```
3. If running Docker with Podman, check port forwarding:
   ```bash
   podman ps
   # Verify port 5001:8080 mapping
   ```

---

**Problem:** "dotnet: command not found" error

**Solutions:**
1. Install .NET 8 SDK:
   - macOS: `brew install dotnet-sdk`
   - Windows: Download from https://dotnet.microsoft.com/download
   - Linux: Follow distribution-specific instructions

2. Verify installation:
   ```bash
   dotnet --version
   ```

3. Restart terminal/IDE after installation

---

## Testing

### Manual Test

**Prerequisites:**
- GraphQL API running (`/deploy` or `docker-compose up -d`)
- Bearer token generated and configured
- Claude Code configuration updated

**Test Steps:**

1. **Open Claude Code**

2. **Ask Claude Code about available tools:**
   ```
   "What MCP tools do you have for time reporting?"
   ```

   **Expected response:** Claude Code should list all 7 time reporting tools.

3. **Test a simple tool call:**
   ```
   "Get available projects"
   ```

   **Expected response:** List of projects (INTERNAL, CLIENT-A, etc.)

4. **Test time logging:**
   ```
   "Log 8 hours of development on INTERNAL for today"
   ```

   **Expected response:** Confirmation message with created entry ID

5. **Verify in database:**
   ```bash
   /db-psql
   SELECT * FROM time_entries ORDER BY created_at DESC LIMIT 1;
   ```

   **Expected:** The entry you just created should appear

### Automated Verification Script

Create `tests/integration/verify-mcp-connection.sh`:

```bash
#!/bin/bash

set -e

echo "=== MCP Connection Verification ==="
echo

# Step 1: Check .NET SDK
echo "1. Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ FAIL: dotnet command not found"
    exit 1
fi
DOTNET_VERSION=$(dotnet --version)
echo "✅ PASS: .NET SDK version $DOTNET_VERSION installed"
echo

# Step 2: Check GraphQL API
echo "2. Checking GraphQL API..."
if ! curl -s http://localhost:5001/health > /dev/null; then
    echo "❌ FAIL: GraphQL API not responding at http://localhost:5001/health"
    exit 1
fi
echo "✅ PASS: GraphQL API is running"
echo

# Step 3: Check MCP Server builds
echo "3. Checking MCP Server build..."
cd TimeReportingMcp
if ! dotnet build --verbosity quiet > /dev/null 2>&1; then
    echo "❌ FAIL: MCP Server build failed"
    exit 1
fi
echo "✅ PASS: MCP Server builds successfully"
cd ..
echo

# Step 4: Check configuration file exists
echo "4. Checking Claude Code configuration..."
CONFIG_PATH=""
if [[ "$OSTYPE" == "darwin"* ]] || [[ "$OSTYPE" == "linux-gnu"* ]]; then
    CONFIG_PATH="$HOME/.config/claude-code/config.json"
elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    CONFIG_PATH="$APPDATA/claude-code/config.json"
fi

if [[ -f "$CONFIG_PATH" ]]; then
    echo "✅ PASS: Configuration file exists at $CONFIG_PATH"
else
    echo "⚠️  WARN: Configuration file not found at $CONFIG_PATH"
    echo "         You need to create it before using Claude Code"
fi
echo

# Step 5: Check bearer token is set
echo "5. Checking bearer token..."
if grep -q "Authentication__BearerToken" .env 2>/dev/null; then
    echo "✅ PASS: Authentication__BearerToken found in .env file"
else
    echo "⚠️  WARN: Authentication__BearerToken not found in .env file"
    echo "         Generate one with: openssl rand -base64 32"
fi
echo

echo "=== Verification Complete ==="
echo
echo "Next steps:"
echo "1. Configure Claude Code (see docs/integration/CLAUDE-CODE-SETUP.md)"
echo "2. Restart Claude Code"
echo "3. Test with: 'Get available projects'"
```

Make it executable:
```bash
chmod +x tests/integration/verify-mcp-connection.sh
```

---

## Related Files

**Created:**
- `docs/integration/claude_desktop_config.json.example` - Example configuration
- `docs/integration/CLAUDE-CODE-SETUP.md` - Setup and troubleshooting guide
- `tests/integration/verify-mcp-connection.sh` - Verification script

**Referenced:**
- `TimeReportingMcp/TimeReportingMcp.csproj` - MCP Server project
- `.env` - API configuration (Authentication__BearerToken)
- `~/.config/claude-code/config.json` - Claude Code config (user's machine)

---

## Definition of Done

- [ ] Example configuration file created and documented
- [ ] Setup guide written with platform-specific instructions
- [ ] Environment variables documented
- [ ] Bearer token generation process documented
- [ ] Troubleshooting section complete with common issues
- [ ] Verification script created and tested
- [ ] Manual test completed successfully
- [ ] Configuration tested on at least one platform (macOS, Windows, or Linux)

---

## Next Steps

After completing this task:
1. Proceed to **Task 11.2: E2E Test Scenarios** to document end-to-end testing workflows
2. Test the configuration with actual Claude Code to verify all 7 tools are accessible
3. Document any additional troubleshooting steps discovered during testing

---

## Resources

- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [Claude Code Documentation](https://docs.claude.com/claude-code)
- [.NET 8 SDK Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [OpenSSL Documentation](https://www.openssl.org/docs/)
