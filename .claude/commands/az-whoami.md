---
description: Check current Azure CLI user identity
allowed-tools: Bash(az:*), Bash(jq:*)
---

Display the currently authenticated Azure CLI user account information, including access token and ACL claims.

### Execution

```bash
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
1. **Account Info**: Subscription, user, type, tenant ID
2. **Access Token**: Confirmation of token acquisition
3. **Token Claims**: User OID, email, name, expiration, roles, groups
4. **ACL Claims**: Custom `extn.TimeReportingACLv2` claim for permissions (format: Project/CODE=V,E,T,A,M)

### Notes

- Useful for verifying which identity the MCP server will use
- MCP server uses AzureCliCredential which inherits this identity
- ACL claims control user permissions (projects, approve/decline)
- Run `az login` if no user is authenticated
- Requires `jq` for JSON parsing
