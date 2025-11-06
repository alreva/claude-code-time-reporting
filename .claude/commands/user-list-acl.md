# /user-list-acl Command

List current ACL entries for a user.

## Usage

Syntax: `/user-list-acl --user <email>`

Example:
```
/user-list-acl --user alex@example.com
```

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
    --uri "https://graph.microsoft.com/v1.0/users/$USER_ID?\$select=extension_extcz1lst0i_TimeReporting_acl" \
    --query "extension_extcz1lst0i_TimeReporting_acl" -o json 2>/dev/null || echo "[]")

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
