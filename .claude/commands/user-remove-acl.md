# /user-remove-acl Command

Remove specific ACL entries from a user.

## Usage

Syntax: `/user-remove-acl --user <email> --entries <semicolon-separated>`

Example:
```
/user-remove-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M"
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
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID?\$select=extension_8b3f87d7bc23493288b5f24056999600_TimeReportingACL" \
    --query "extension_8b3f87d7bc23493288b5f24056999600_TimeReportingACL" -o json 2>/dev/null || echo "[]")

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
    --body "{\"extension_8b3f87d7bc23493288b5f24056999600_TimeReportingACL\": $ACL_JSON}" \
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
