# Task 15.7: ACL Documentation

**Phase:** 15 - ACL-Based Authorization
**Estimated Time:** 30 minutes
**Prerequisites:** Tasks 15.1-15.6 complete
**Status:** Pending

---

## Objective

Create comprehensive documentation for the ACL authorization system, including an Architecture Decision Record (ADR), setup guide, permission reference, and troubleshooting guide. Ensure future developers can understand and maintain the system.

---

## Background

Good documentation is critical for:
- **Onboarding**: New team members need to understand the authorization model
- **Maintenance**: Future changes require understanding design decisions
- **Operations**: DevOps needs setup and troubleshooting guides
- **Compliance**: Security audits require clear authorization documentation

---

## Acceptance Criteria

- [ ] ADR created documenting the ACL architecture decision
- [ ] Setup guide created with step-by-step instructions
- [ ] Permission reference document created
- [ ] Troubleshooting guide created
- [ ] README.md updated with Phase 15 information
- [ ] CLAUDE.md updated with authorization guidelines
- [ ] All documentation reviewed for accuracy

---

## Implementation

### Document 1: Architecture Decision Record

**File:** `docs/adr/0010-acl-authorization-azure-entra-id.md`

```markdown
# ADR 0010: ACL-Based Authorization with Azure Entra ID Extension Attributes

**Status:** Accepted
**Date:** 2025-01-06
**Deciders:** Development Team
**Context:** Phase 15 - ACL-Based Authorization

---

## Context

The Time Reporting system requires fine-grained, project-level authorization for approve/decline operations. Users should only be able to approve or decline time entries for projects they administer.

**Requirements:**
- Per-project authorization (users can approve Project A but not Project B)
- Hierarchical permissions (parent resources grant permissions to children)
- Token-based authorization (no database lookups on every request)
- Short-lived tokens (5 minutes) with refresh token flow
- Fully managed via Azure Entra ID (no custom auth service)
- Extensible to other resource types (Tasks, Reports, etc.)

---

## Decision

We will use **Azure Entra ID schema extensions** to store hierarchical ACL entries in JWT tokens as a multi-value string array claim.

**Architecture:**
1. **Extension Attribute**: `extension_TimeReporting_acl` stores array of ACL strings
2. **ACL Format**: `"ResourcePath=Permission1,Permission2"` (e.g., `"Project/INTERNAL=V,A,M"`)
3. **Hierarchical Paths**: Use forward slashes (e.g., `Project/INTERNAL/Task/17`)
4. **Permission Abbreviations**: Single letters for compactness (V, E, A, M, T)
5. **Token Inclusion**: Claims included in JWT access token via optional claims configuration
6. **Authorization Logic**: ClaimsPrincipal extension methods parse claims and check permissions
7. **Fallback Strategy**: If child path not found, check parent paths up to root

---

## Rationale

### Why Azure Entra ID Extension Attributes?

**Alternatives Considered:**

1. **Database Table for Project Admins**
   - ❌ Requires database lookup on every authorization check
   - ❌ Additional latency (~10-50ms per check)
   - ❌ More complex code (service layer, caching, etc.)
   - ✅ Easier to manage via UI

2. **Azure Entra ID App Roles**
   - ❌ Roles are coarse-grained (e.g., "ProjectAdmin" role applies to ALL projects)
   - ❌ Cannot represent per-project permissions
   - ❌ Would need one role per project (not scalable)
   - ✅ Native to Azure Entra ID

3. **Azure Entra ID Security Groups**
   - ❌ Group names appear in token but cannot encode permissions
   - ❌ Would need separate groups for "INTERNAL-Viewer", "INTERNAL-Approver", etc.
   - ❌ Explosion of groups (projects × permissions)
   - ✅ Familiar to IT admins

4. **External Authorization Service (e.g., OPA, AuthZ)**
   - ❌ Additional infrastructure to maintain
   - ❌ Network latency on every request
   - ❌ Complexity of token enrichment
   - ✅ Highly flexible and powerful

**Why We Chose Extension Attributes:**

- ✅ **Token-Based**: Authorization data in JWT, no database/API calls
- ✅ **Performance**: Constant-time permission checks (<5ms)
- ✅ **Hierarchical**: Supports parent-child resource trees
- ✅ **Flexible**: Can represent any resource type and permission
- ✅ **Scalable**: Up to ~100 ACL entries per user (~3-5 KB token size)
- ✅ **Entra-Native**: Fully managed within Microsoft identity platform
- ✅ **Secure**: Cryptographically signed by Azure Entra ID

---

## Consequences

### Positive

- **Fast Authorization**: In-memory checks, no I/O
- **Offline Capable**: Can validate tokens without network calls
- **Audit Trail**: ACL values logged in Azure AD audit logs
- **Centralized Management**: All permissions managed via Azure Entra ID
- **Token Refresh**: Changes propagate quickly (5-10 minutes)

### Negative

- **Token Size**: Large ACL arrays increase token size (limit: 24 KB)
- **Propagation Delay**: Changes take 5-10 minutes to appear in new tokens
- **Management Complexity**: Requires Azure CLI/Graph API (no built-in UI)
- **Cache Invalidation**: Cannot revoke permissions immediately (must wait for token expiry)

### Mitigation Strategies

- **Token Size**: Use permission abbreviations (single letters)
- **Propagation Delay**: Short token lifetime (5 minutes) minimizes delay
- **Management**: Provide slash commands for common operations
- **Revocation**: Use 5-minute token lifetime for near-real-time revocation

---

## Implementation

**Key Components:**

1. **Azure Entra ID Setup**
   - Schema extension: `extension_TimeReporting_acl`
   - Optional claim configuration
   - User ACL assignment via Graph API

2. **Authorization Helper**
   - `AclExtensions.cs`: ClaimsPrincipal extension methods
   - `ParseAclClaims()`: Parse ACL array from token
   - `HasPermission(path, perm)`: Check hierarchical permissions

3. **Mutation Authorization**
   - `ApproveTimeEntry`: Check `Project/{code}` + "Approve" permission
   - `DeclineTimeEntry`: Check `Project/{code}` + "Approve" permission

4. **Management Tools**
   - `/user-add-acl`: Add ACL entries
   - `/user-list-acl`: List user ACLs
   - `/user-remove-acl`: Remove ACL entries

---

## References

- [PRD: Time Reporting GraphQL Authorization](../../TimeReportingGraphQL_PRD.md)
- [Azure AD Schema Extensions](https://learn.microsoft.com/en-us/graph/extensibility-schema-groups)
- [JWT Claims Customization](https://learn.microsoft.com/en-us/entra/identity-platform/jwt-claims-customization)
- [HotChocolate Authorization](https://chillicream.com/docs/hotchocolate/v15/security/authorization/)

---

## Notes

- **Token Lifetime**: 5-minute access tokens provide near-real-time permission updates
- **Hierarchical Depth**: No practical limit on path depth (e.g., `Project/A/Task/B/Subtask/C`)
- **Performance**: Authorization checks are O(n) where n = # of ACL entries (typically <100)
- **Security**: JWT signature ensures ACL claims cannot be tampered with
```

### Document 2: Setup Guide

**File:** `docs/ACL-SETUP-GUIDE.md`

```markdown
# ACL Authorization Setup Guide

Complete guide to setting up hierarchical ACL-based authorization for the Time Reporting system.

---

## Prerequisites

- Azure Entra ID tenant
- App registration for Time Reporting API
- Azure CLI installed and authenticated
- `User.ReadWrite.All` permission in Microsoft Graph

---

## Step 1: Create Schema Extension

```bash
# Set your app registration client ID
export APP_ID="8b3f87d7-bc23-4932-88b5-f24056999600"

# Create schema extension
az rest --method post \
  --uri https://graph.microsoft.com/v1.0/schemaExtensions \
  --body "{
    \"id\": \"TimeReporting\",
    \"description\": \"Per-user ACL entries for Time Reporting\",
    \"targetTypes\": [\"User\"],
    \"properties\": [{\"name\": \"acl\", \"type\": \"Collection(String)\"}],
    \"owner\": \"$APP_ID\"
  }"
```

**Save the full extension ID** from the response (e.g., `extABCDEF_TimeReporting`).

---

## Step 2: Configure Token Claims

1. Go to **Azure Portal** → **Azure Entra ID** → **App registrations**
2. Select **TimeReporting API**
3. Navigate to **Token configuration**
4. Click **Add optional claim** → **Access token**
5. Select `extension_TimeReporting_acl`
6. Check **Turn on Microsoft Graph profile permission**
7. Click **Add**

---

## Step 3: Assign ACL Values to Users

```bash
# Example: Give user admin access to INTERNAL project
/user-add-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M"

# Multiple projects
/user-add-acl --user alex@example.com --entries "Project/INTERNAL=V,A,M;Project/CLIENT-A=V,E,T"

# Hierarchical (all projects)
/user-add-acl --user alex@example.com --entries "Project=A"
```

---

## Step 4: Verify Token

```bash
# Get token
TOKEN=$(az account get-access-token \
  --resource api://8b3f87d7-bc23-4932-88b5-f24056999600 \
  --query accessToken -o tsv)

# Decode at jwt.ms
echo $TOKEN
# Paste at https://jwt.ms
```

**Verify claim exists:**
```json
{
  "extension_TimeReporting_acl": [
    "Project/INTERNAL=V,A,M",
    "Project/CLIENT-A=V,E,T"
  ]
}
```

---

## Step 5: Test Authorization

```graphql
# Create and submit a time entry
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    standardHours: 2.0
    startDate: "2025-01-06"
    completionDate: "2025-01-06"
  }) {
    id
  }
}

mutation {
  submitTimeEntry(id: "ENTRY_ID") {
    id
    status
  }
}

# Try to approve (requires "A" permission)
mutation {
  approveTimeEntry(id: "ENTRY_ID") {
    id
    status
  }
}
```

**Expected:** ✅ Succeeds if user has `Project/INTERNAL=A`

---

## Permission Reference

| Abbreviation | Name | Description |
|--------------|------|-------------|
| `V` | View | Read access to resources |
| `E` | Edit | Modify time entries |
| `A` | Approve | Approve or decline time entries |
| `M` | Manage | Administrative operations |
| `T` | Track | Log new time entries |

---

## Common Patterns

### Pattern 1: Project Admin
```
Project/INTERNAL=V,A,M
```
Can view, approve, and manage INTERNAL project.

### Pattern 2: All Projects Admin
```
Project=A,M
```
Can approve and manage ALL projects (hierarchical).

### Pattern 3: Multiple Projects
```
Project/INTERNAL=V,A,M;Project/CLIENT-A=V,E,T;Project/MAINT=V
```
Different permissions for different projects.

### Pattern 4: Task-Specific
```
Project/INTERNAL/Task/Development=V,E
```
Only edit Development tasks (not other tasks).

---

## Troubleshooting

See [ACL-TROUBLESHOOTING.md](./ACL-TROUBLESHOOTING.md)

---

## Management Commands

- `/user-add-acl --user <email> --entries <ACLs>` - Add ACL entries
- `/user-list-acl --user <email>` - List current ACLs
- `/user-remove-acl --user <email> --entries <ACLs>` - Remove ACL entries

---

## Security Considerations

- **Token Lifetime**: 5 minutes (near-real-time revocation)
- **Token Signature**: Cryptographically signed (cannot tamper)
- **Propagation**: Changes take 5-10 minutes to appear
- **Token Size**: ~100 ACL entries = ~3-5 KB (well under 24 KB limit)
```

### Document 3: Troubleshooting Guide

**File:** `docs/ACL-TROUBLESHOOTING.md`

```markdown
# ACL Authorization Troubleshooting Guide

Common issues and solutions for ACL-based authorization.

---

## Issue 1: Claim Not Appearing in Token

**Symptoms:**
- `extension_TimeReporting_acl` claim missing from JWT
- Authorization always fails

**Solutions:**

1. **Check token configuration:**
   - Azure Portal → App registrations → Token configuration
   - Ensure `extension_TimeReporting_acl` is added as optional claim
   - Ensure "Turn on Microsoft Graph profile permission" is checked

2. **Verify token audience:**
   ```bash
   # ✅ Correct
   az account get-access-token --resource api://YOUR_APP_ID

   # ❌ Wrong (uses Microsoft Graph, no custom claims)
   az account get-access-token --resource https://graph.microsoft.com
   ```

3. **Wait for propagation:**
   - Changes take 5-10 minutes
   - Get a NEW token (don't reuse old one)

4. **Check extension attribute name:**
   - Full name may have prefix: `extABCDEF_TimeReporting_acl`
   - Update `AclExtensions.cs` if needed

---

## Issue 2: Permission Denied Errors

**Symptoms:**
- `AUTH_FORBIDDEN` errors when trying to approve/decline

**Solutions:**

1. **Verify ACL entries:**
   ```bash
   /user-list-acl --user your.email@example.com
   ```

2. **Check permission format:**
   - ✅ Correct: `Project/INTERNAL=V,A,M`
   - ❌ Wrong: `Project/INTERNAL=View,Approve,Manage`
   - Use single-letter abbreviations only

3. **Check project code:**
   - Must match EXACTLY (case-sensitive in DB, case-insensitive in ACL)
   - `INTERNAL` ≠ `Internal`

4. **Test hierarchical fallback:**
   ```bash
   # If you have Project=A, you should have permission for all projects
   /user-add-acl --user your.email@example.com --entries "Project=A"
   ```

---

## Issue 3: Changes Not Taking Effect

**Symptoms:**
- Added ACL entries but still getting denied

**Solutions:**

1. **Get a NEW token:**
   ```bash
   az account clear  # Clear cached tokens
   az login
   TOKEN=$(az account get-access-token --resource api://YOUR_APP_ID --query accessToken -o tsv)
   ```

2. **Wait 5-10 minutes:**
   - Azure Entra ID needs time to propagate changes
   - Tokens are cached for 5 minutes

3. **Verify token contains changes:**
   - Decode at https://jwt.ms
   - Check `extension_TimeReporting_acl` array

---

## Issue 4: Azure CLI Permission Errors

**Symptoms:**
```
Error: Insufficient privileges to complete the operation
```

**Solutions:**

1. **Grant Microsoft Graph permissions:**
   ```bash
   az ad app permission add --id YOUR_APP_ID \
     --api 00000003-0000-0000-c000-000000000000 \
     --api-permissions 741f803b-c850-494e-b5df-cde7c675a1ca=Role
   ```

2. **Request admin consent:**
   - Azure Portal → App registrations → API permissions
   - Click **Grant admin consent**

3. **Use correct account:**
   - Ensure you're logged in as user with admin rights
   - `az account show` to verify

---

## Issue 5: Token Size Too Large

**Symptoms:**
- 401 Unauthorized with many ACL entries
- Token exceeds 24 KB limit

**Solutions:**

1. **Count ACL entries:**
   ```bash
   /user-list-acl --user your.email@example.com | wc -l
   ```

2. **Use hierarchical permissions:**
   - ❌ Bad: 50 entries for 50 projects
   - ✅ Good: `Project=A` for all projects

3. **Remove unused entries:**
   ```bash
   /user-remove-acl --user your.email@example.com --entries "..."
   ```

4. **Check token size:**
   ```bash
   echo $TOKEN | wc -c  # Should be <24000
   ```

---

## Debug Commands

```bash
# Check extension attribute exists
az rest --method get --uri https://graph.microsoft.com/v1.0/schemaExtensions

# Get user's ACL values directly
USER_ID=$(az ad user show --id your.email@example.com --query id -o tsv)
az rest --method get --uri "https://graph.microsoft.com/v1.0/users/$USER_ID?\$select=extension_TimeReporting_acl"

# Decode token
echo $TOKEN | cut -d'.' -f2 | base64 -d | jq .
```

---

## Contact Support

If issues persist:
1. Check [ACL-SETUP-GUIDE.md](./ACL-SETUP-GUIDE.md)
2. Review [ADR 0010](./adr/0010-acl-authorization-azure-entra-id.md)
3. Contact DevOps team
```

---

## Integration Points

This documentation ties together:
- All Phase 15 tasks
- Azure Entra ID configuration
- API implementation
- Management tools
- Testing procedures

---

## Related Files

**Created:**
- `docs/adr/0010-acl-authorization-azure-entra-id.md`
- `docs/ACL-SETUP-GUIDE.md`
- `docs/ACL-TROUBLESHOOTING.md`

**Modified:**
- `README.md` - Add Phase 15 overview
- `CLAUDE.md` - Add authorization guidelines
- `docs/adr/README.md` - Add ADR 0010 to index

---

## Validation

After completing this task:

1. ✅ ADR 0010 created and indexed
2. ✅ Setup guide complete with step-by-step instructions
3. ✅ Troubleshooting guide covers common issues
4. ✅ README.md updated
5. ✅ CLAUDE.md updated
6. ✅ All documentation reviewed for accuracy

---

## Next Steps

Phase 15 is complete! Next:
- Implement Phase 13 (StrawberryShake Migration) if not done
- Consider future enhancements (other resource types, audit logging, etc.)

---

## Notes

- **Living Documentation**: Update as system evolves
- **Examples**: Include real-world examples from your tenant
- **Screenshots**: Consider adding Azure Portal screenshots
- **Video**: Consider recording setup walkthrough

---

## Reference

- [ADR Template](./adr/TEMPLATE.md)
- [Documentation Best Practices](https://www.writethedocs.org/guide/writing/beginners-guide-to-docs/)
