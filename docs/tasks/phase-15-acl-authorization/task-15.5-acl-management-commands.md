# Task 15.5: ACL Management Slash Commands

**Phase:** 15 - ACL-Based Authorization
**Estimated Time:** 60 minutes
**Prerequisites:** Task 15.1 complete (Azure Entra ID schema extension exists)
**Status:** Pending

---

## Objective

Create three slash commands (`/user-add-acl`, `/user-list-acl`, `/user-remove-acl`) to manage user ACL values via Azure CLI and Microsoft Graph API. These commands enable developers to assign, view, and remove project-level permissions without using the Azure Portal.

---

## Background

ACL values are stored as an extension attribute on user objects in Azure Entra ID. Managing them requires:
1. **Microsoft Graph API** calls to get/patch user objects
2. **Azure CLI** authentication with appropriate permissions
3. **Array manipulation** for add/remove operations

**Commands to Create:**
- `/user-add-acl` - Add new ACL entries to a user
- `/user-list-acl` - List current ACL entries for a user
- `/user-remove-acl` - Remove specific ACL entries from a user

---

## Acceptance Criteria

- [ ] `.claude/commands/user-add-acl.md` created
- [ ] `.claude/commands/user-list-acl.md` created
- [ ] `.claude/commands/user-remove-acl.md` created
- [ ] Commands added to `.claude/settings.local.json` permissions
- [ ] Commands use `az rest` with Microsoft Graph API
- [ ] Commands handle errors gracefully (user not found, permission denied, etc.)
- [ ] Commands verified to work with test users
- [ ] Documentation includes usage examples

---

## Implementation

### Command 1: Add ACL Entries

**File:** `.claude/commands/user-add-acl.md`

```markdown
# /user-add-acl Command

Add ACL entries to a user in Azure Entra ID.

## Usage

Syntax: `/user-add-acl --user <email> --entries <semicolon-separated>`

Example:
User: Can you add Project/INTERNAL=V,A,M to alex@example.com?
Claude: /user-add-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M"

Multiple entries:
Claude: /user-add-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M;Project/CLIENT-A=V,E,T"

## Implementation

```bash
#!/bin/bash
set -e

# Parse arguments
USER_EMAIL=""
ENTRIES=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --user)
            USER_EMAIL="$2"
            shift 2
            ;;
        --entries)
            ENTRIES="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1"
            exit 1
            ;;
    esac
done

# Validate arguments
if [[ -z "$USER_EMAIL" ]]; then
    echo "❌ Error: --user is required"
    exit 1
fi

if [[ -z "$ENTRIES" ]]; then
    echo "❌ Error: --entries is required"
    exit 1
fi

# Get user ID
echo "🔍 Looking up user: $USER_EMAIL"
USER_ID=$(az ad user show --id "$USER_EMAIL" --query id -o tsv 2>/dev/null)

if [[ -z "$USER_ID" ]]; then
    echo "❌ Error: User not found: $USER_EMAIL"
    exit 1
fi

echo "✅ Found user ID: $USER_ID"

# Get current ACL values
echo "📋 Fetching current ACL entries..."
CURRENT_ACL=$(az rest --method get \
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID?\$select=extension_TimeReporting_acl" \
    --query "extension_TimeReporting_acl" -o json 2>/dev/null || echo "[]")

# Parse new entries (semicolon-separated)
IFS=';' read -ra NEW_ENTRIES <<< "$ENTRIES"

# Merge current and new entries (avoid duplicates)
MERGED_ACL=$(echo "$CURRENT_ACL" | jq -r '.[]' 2>/dev/null || echo "")

for entry in "${NEW_ENTRIES[@]}"; do
    entry_trimmed=$(echo "$entry" | xargs)  # Trim whitespace
    if ! echo "$MERGED_ACL" | grep -Fxq "$entry_trimmed"; then
        MERGED_ACL+=$'\n'"$entry_trimmed"
    fi
done

# Build JSON array
ACL_JSON=$(echo "$MERGED_ACL" | grep -v '^$' | jq -R . | jq -s .)

# Update user
echo "💾 Updating ACL entries..."
az rest --method patch \
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID" \
    --body "{\"extension_TimeReporting_acl\": $ACL_JSON}" \
    --headers "Content-Type=application/json" > /dev/null

echo "✅ Successfully added ACL entries to $USER_EMAIL"
echo ""
echo "📄 New ACL entries:"
echo "$ACL_JSON" | jq -r '.[]' | sed 's/^/  - /'
echo ""
echo "⏱️  Note: Changes will appear in new tokens after 5-10 minutes"
```
```

---

### Command 2: List ACL Entries

**File:** `.claude/commands/user-list-acl.md`

```markdown
# /user-list-acl Command

List current ACL entries for a user.

## Usage

Syntax: `/user-list-acl --user <email>`

Example:
User: What ACL entries does alex@example.com have?
Claude: /user-list-acl --user alex@example.com

## Implementation

```bash
#!/bin/bash
set -e

# Parse arguments
USER_EMAIL=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --user)
            USER_EMAIL="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1"
            exit 1
            ;;
    esac
done

# Validate arguments
if [[ -z "$USER_EMAIL" ]]; then
    echo "❌ Error: --user is required"
    exit 1
fi

# Get user ID
echo "🔍 Looking up user: $USER_EMAIL"
USER_ID=$(az ad user show --id "$USER_EMAIL" --query id -o tsv 2>/dev/null)

if [[ -z "$USER_ID" ]]; then
    echo "❌ Error: User not found: $USER_EMAIL"
    exit 1
fi

echo "✅ Found user ID: $USER_ID"
echo ""

# Get ACL entries
echo "📋 ACL entries for $USER_EMAIL:"
ACL_ENTRIES=$(az rest --method get \
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID?\$select=extension_TimeReporting_acl" \
    --query "extension_TimeReporting_acl" -o json 2>/dev/null || echo "[]")

# Display entries
if [[ "$ACL_ENTRIES" == "[]" ]] || [[ "$ACL_ENTRIES" == "null" ]]; then
    echo "  (No ACL entries found)"
else
    echo "$ACL_ENTRIES" | jq -r '.[]' | sed 's/^/  - /'
fi

echo ""
echo "Legend:"
echo "  V = View (read access)"
echo "  E = Edit (modify entries)"
echo "  A = Approve (approve/decline entries)"
echo "  M = Manage (admin operations)"
echo "  T = Track (log time entries)"
```
```

---

### Command 3: Remove ACL Entries

**File:** `.claude/commands/user-remove-acl.md`

```markdown
# /user-remove-acl Command

Remove specific ACL entries from a user.

## Usage

Syntax: `/user-remove-acl --user <email> --entries <semicolon-separated>`

Example:
User: Remove Project/INTERNAL permissions from alex@example.com
Claude: /user-remove-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M"

## Implementation

```bash
#!/bin/bash
set -e

# Parse arguments
USER_EMAIL=""
ENTRIES=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --user)
            USER_EMAIL="$2"
            shift 2
            ;;
        --entries)
            ENTRIES="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1"
            exit 1
            ;;
    esac
done

# Validate arguments
if [[ -z "$USER_EMAIL" ]]; then
    echo "❌ Error: --user is required"
    exit 1
fi

if [[ -z "$ENTRIES" ]]; then
    echo "❌ Error: --entries is required"
    exit 1
fi

# Get user ID
echo "🔍 Looking up user: $USER_EMAIL"
USER_ID=$(az ad user show --id "$USER_EMAIL" --query id -o tsv 2>/dev/null)

if [[ -z "$USER_ID" ]]; then
    echo "❌ Error: User not found: $USER_EMAIL"
    exit 1
fi

echo "✅ Found user ID: $USER_ID"

# Get current ACL values
echo "📋 Fetching current ACL entries..."
CURRENT_ACL=$(az rest --method get \
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID?\$select=extension_TimeReporting_acl" \
    --query "extension_TimeReporting_acl" -o json 2>/dev/null || echo "[]")

# Parse entries to remove (semicolon-separated)
IFS=';' read -ra REMOVE_ENTRIES <<< "$ENTRIES"

# Filter out entries to remove
FILTERED_ACL=""
while IFS= read -r line; do
    should_keep=true
    for remove_entry in "${REMOVE_ENTRIES[@]}"; do
        remove_trimmed=$(echo "$remove_entry" | xargs)
        if [[ "$line" == "$remove_trimmed" ]]; then
            should_keep=false
            break
        fi
    done
    if $should_keep && [[ -n "$line" ]]; then
        FILTERED_ACL+=$'\n'"$line"
    fi
done < <(echo "$CURRENT_ACL" | jq -r '.[]' 2>/dev/null)

# Build JSON array
ACL_JSON=$(echo "$FILTERED_ACL" | grep -v '^$' | jq -R . | jq -s .)

# Update user
echo "💾 Updating ACL entries..."
az rest --method patch \
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID" \
    --body "{\"extension_TimeReporting_acl\": $ACL_JSON}" \
    --headers "Content-Type=application/json" > /dev/null

echo "✅ Successfully removed ACL entries from $USER_EMAIL"
echo ""
echo "📄 Remaining ACL entries:"
if [[ "$ACL_JSON" == "[]" ]]; then
    echo "  (No ACL entries remaining)"
else
    echo "$ACL_JSON" | jq -r '.[]' | sed 's/^/  - /'
fi
echo ""
echo "⏱️  Note: Changes will appear in new tokens after 5-10 minutes"
```
```

---

### Step 2: Update Permissions in settings.local.json

**File:** `.claude/settings.local.json`

**Find the `autoApprove` section and add the new commands:**

```json
{
  "autoApprove": {
    "tool:SlashCommand": [
      "/build",
      "/build-api",
      "/build-mcp",
      "/test",
      "/test-api",
      "/test-mcp",
      "/run-api",
      "/run-mcp",
      "/stop-api",
      "/stop-mcp",
      "/ef-migration",
      "/db-start",
      "/db-stop",
      "/db-restart",
      "/db-logs",
      "/db-psql",
      "/status",
      "/deploy",
      "/seed-db",
      "/user-add-acl",
      "/user-list-acl",
      "/user-remove-acl"
    ]
  }
}
```

---

## Testing

### Test Command 1: Add ACL Entries

```bash
# Test adding single entry
/user-add-acl --user your.email@example.com --entries "Project/INTERNAL=V,A,M"

# Test adding multiple entries
/user-add-acl --user your.email@example.com --entries "Project/INTERNAL=V,A,M;Project/CLIENT-A=V,E,T"

# Verify with list command
/user-list-acl --user your.email@example.com
```

**Expected Output:**
```
🔍 Looking up user: your.email@example.com
✅ Found user ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
📋 Fetching current ACL entries...
💾 Updating ACL entries...
✅ Successfully added ACL entries to your.email@example.com

📄 New ACL entries:
  - Project/INTERNAL=V,A,M
  - Project/CLIENT-A=V,E,T

⏱️  Note: Changes will appear in new tokens after 5-10 minutes
```

### Test Command 2: List ACL Entries

```bash
/user-list-acl --user your.email@example.com
```

**Expected Output:**
```
🔍 Looking up user: your.email@example.com
✅ Found user ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890

📋 ACL entries for your.email@example.com:
  - Project/INTERNAL=V,A,M
  - Project/CLIENT-A=V,E,T

Legend:
  V = View (read access)
  E = Edit (modify entries)
  A = Approve (approve/decline entries)
  M = Manage (admin operations)
  T = Track (log time entries)
```

### Test Command 3: Remove ACL Entries

```bash
/user-remove-acl --user your.email@example.com --entries "Project/CLIENT-A=V,E,T"

# Verify removal
/user-list-acl --user your.email@example.com
```

**Expected Output:**
```
🔍 Looking up user: your.email@example.com
✅ Found user ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
📋 Fetching current ACL entries...
💾 Updating ACL entries...
✅ Successfully removed ACL entries from your.email@example.com

📄 Remaining ACL entries:
  - Project/INTERNAL=V,A,M

⏱️  Note: Changes will appear in new tokens after 5-10 minutes
```

### Verify Token Contains Changes

```bash
# Wait 5-10 minutes for propagation

# Get new token
TOKEN=$(az account get-access-token --resource api://8b3f87d7-bc23-4932-88b5-f24056999600 --query accessToken -o tsv)

# Verify at jwt.ms
echo $TOKEN
# Paste at https://jwt.ms and check extension_TimeReporting_acl claim
```

---

## Common Issues

**Issue 1: Permission denied**

```
Error: Insufficient privileges to complete the operation
```

**Solution:** Ensure your Azure CLI user has `User.ReadWrite.All` permission in Microsoft Graph.

```bash
# Check current permissions
az ad app permission list --id YOUR_APP_ID

# Grant permission (requires admin consent)
az ad app permission add --id YOUR_APP_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions 741f803b-c850-494e-b5df-cde7c675a1ca=Role
```

**Issue 2: Extension attribute not found**

```
Error: Property 'extension_TimeReporting_acl' does not exist
```

**Solution:** Verify schema extension was created in Task 15.1. The full extension name may have a prefix (e.g., `extABCDEF_TimeReporting_acl`).

**Issue 3: jq command not found**

```
bash: jq: command not found
```

**Solution:** Install jq:
```bash
# macOS
brew install jq

# Linux
sudo apt-get install jq
```

---

## Integration Points

These slash commands enable:
- **Task 15.6**: Assign test users different ACL configurations for testing
- **Day-to-day operations**: Manage project-level permissions without Azure Portal
- **Automation**: Script ACL assignments for new team members

---

## Related Files

**Created:**
- `.claude/commands/user-add-acl.md`
- `.claude/commands/user-list-acl.md`
- `.claude/commands/user-remove-acl.md`

**Modified:**
- `.claude/settings.local.json` - Added command permissions

---

## Validation

After completing this task:

1. ✅ All three commands exist and are executable
2. ✅ Commands added to `.claude/settings.local.json`
3. ✅ `/user-add-acl` successfully adds ACL entries
4. ✅ `/user-list-acl` displays current entries
5. ✅ `/user-remove-acl` removes specified entries
6. ✅ Changes propagate to JWT tokens within 5-10 minutes

---

## Next Steps

After completing Task 15.5:
- **Task 15.6:** Comprehensive authorization testing with different ACL configurations

---

## Notes

- **Propagation Time**: ACL changes take 5-10 minutes to appear in new tokens
- **Merge Strategy**: `/user-add-acl` avoids duplicates by checking existing entries
- **Case Sensitivity**: ACL entries are case-sensitive (use exact format: `Project/CODE=P,E,R,M,S`)
- **Azure CLI**: Commands require `az login` with appropriate permissions
- **jq Dependency**: All commands use `jq` for JSON parsing

---

## Reference

- [Microsoft Graph API - Update User](https://learn.microsoft.com/en-us/graph/api/user-update)
- [Azure CLI - REST Command](https://learn.microsoft.com/en-us/cli/azure/reference-index#az-rest)
- [Schema Extensions](https://learn.microsoft.com/en-us/graph/extensibility-schema-groups)
