Clear current Azure CLI session and re-authenticate, then display user info and ACL claims.

### Execution

```bash
echo "=== Clearing Current Azure Session ==="
az account clear
echo "✅ Session cleared"
echo ""

echo "=== Logging in to Azure ==="
az login --tenant 37bd55f6-c3fa-42a0-a659-30571b56457b --allow-no-subscriptions
echo ""

echo "=== Acquiring Access Token ==="
TOKEN=$(az account get-access-token --scope api://8b3f87d7-bc23-4932-88b5-f24056999600/.default --query accessToken -o tsv)
if [ -n "$TOKEN" ]; then
  echo "✅ Access token acquired successfully"
else
  echo "❌ Failed to acquire access token"
  exit 1
fi
echo ""

echo "=== Azure Account Info ==="
az account show --query "{Subscription:name, User:user.name, Type:user.type, TenantId:tenantId}" -o table

echo ""
echo "=== Access Token Info ==="
TOKEN=$(az account get-access-token --scope api://8b3f87d7-bc23-4932-88b5-f24056999600/.default --query accessToken -o tsv)
if [ -n "$TOKEN" ]; then
  echo "Token acquired successfully"
  echo ""

  # Decode JWT using Python (handles base64url properly)
  CLAIMS=$(python3 -c "
import base64
import json
import sys

token = '$TOKEN'
parts = token.split('.')
payload = parts[1]

# Add padding if needed
padding = len(payload) % 4
if padding:
    payload += '=' * (4 - padding)

decoded = base64.urlsafe_b64decode(payload)
claims = json.loads(decoded)
print(json.dumps(claims))
")

  echo "=== Token Claims ==="
  echo "$CLAIMS" | jq '{
    "User (oid)": .oid,
    "Email": (.email // .upn // .unique_name // "N/A"),
    "Name": (.name // "N/A"),
    "Issued": (.iat | strftime("%Y-%m-%d %H:%M:%S")),
    "Expires": (.exp | strftime("%Y-%m-%d %H:%M:%S")),
    "Audience": .aud,
    "Roles": (.roles // []),
    "Groups": (.groups // [])
  }'

  echo ""
  echo "=== ACL Claims (extn.TimeReportingACLv2) ==="
  ACL=$(echo "$CLAIMS" | jq -r '."extn.TimeReportingACLv2" // empty')
  if [ -z "$ACL" ]; then
    echo "No ACL claims found"
  else
    echo "$ACL" | jq -r '.[]'
  fi
else
  echo "Failed to acquire access token"
fi
```

### Expected Output

Shows:
1. **Clear Session**: Confirmation of session cleared
2. **Login**: Azure interactive login prompt (opens browser)
3. **Token Acquisition**: Confirmation of token acquired
4. **Account Info**: Subscription, user, type, tenant ID
5. **Token Claims**: User OID, email, name, expiration, roles, groups
6. **ACL Claims**: Custom `extn.TimeReportingACLv2` claim for permissions

### Notes

- Use this command to switch between Azure identities
- After login, the browser will open for authentication
- The MCP server will use the new identity for subsequent API calls
- Requires `jq` for JSON parsing
