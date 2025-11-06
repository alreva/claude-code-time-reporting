# /user-add-acl Command

Add ACL entries to a user in Azure Entra ID.

## Usage

Syntax: `/user-add-acl --user <email> --entries <semicolon-separated>`

Example:
```
/user-add-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M"
```

Multiple entries:
```
/user-add-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M;Project/CLIENT-A=V,E,T"
```

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
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID?\$select=extension_extcz1lst0i_TimeReporting_acl" \
    --query "extension_extcz1lst0i_TimeReporting_acl" -o json 2>/dev/null || echo "[]")

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
    --body "{\"extension_extcz1lst0i_TimeReporting_acl\": $ACL_JSON}" \
    --headers "Content-Type=application/json" > /dev/null

echo "✅ Successfully added ACL entries to $USER_EMAIL"
echo ""
echo "📄 New ACL entries:"
echo "$ACL_JSON" | jq -r '.[]' | sed 's/^/  - /'
echo ""
echo "⏱️  Note: Changes will appear in new tokens after 5-10 minutes"
```
