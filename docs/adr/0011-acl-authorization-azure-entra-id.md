# ADR 0011: ACL-Based Authorization with Azure Entra ID Extension Attributes

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
1. **Extension Attribute**: `extension_extcz1lst0i_TimeReporting_acl` stores array of ACL strings
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
   - Schema extension: `extcz1lst0i_TimeReporting` with `acl` property
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

- [PRD: Time Reporting GraphQL Authorization](../prd/README.md)
- [Azure AD Schema Extensions](https://learn.microsoft.com/en-us/graph/extensibility-schema-groups)
- [JWT Claims Customization](https://learn.microsoft.com/en-us/entra/identity-platform/jwt-claims-customization)
- [HotChocolate Authorization](https://chillicream.com/docs/hotchocolate/v15/security/authorization/)

---

## Notes

- **Token Lifetime**: 5-minute access tokens provide near-real-time permission updates
- **Hierarchical Depth**: No practical limit on path depth (e.g., `Project/A/Task/B/Subtask/C`)
- **Performance**: Authorization checks are O(n) where n = # of ACL entries (typically <100)
- **Security**: JWT signature ensures ACL claims cannot be tampered with
