# Task 15.3: Token Configuration

**Phase:** 15 - ACL-Based Authorization
**Estimated Time:** 15 minutes
**Prerequisites:** Task 15.1 and 15.2 complete
**Status:** Pending

---

## Objective

Configure ASP.NET Core JWT authentication to properly map and preserve Azure Entra ID extension attribute claims, ensuring the ACL claims are accessible in GraphQL resolvers via ClaimsPrincipal.

---

## Background

By default, ASP.NET Core's JWT handler may:
1. **Map claim names** to legacy SAML format (e.g., `roles` → `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`)
2. **Drop unknown claims** if not explicitly configured

To access `extension_TimeReporting_acl` in resolvers, we need to:
- Disable default claim type mapping
- Preserve original claim names from the JWT token
- Optionally add debug logging to verify claims

---

## Acceptance Criteria

- [ ] `Program.cs` updated to disable default claim mapping
- [ ] JWT token validation configured correctly
- [ ] Debug middleware added to log ACL claims (temporary)
- [ ] Verified that `extension_TimeReporting_acl` claim is accessible in ClaimsPrincipal
- [ ] Test endpoint confirms claims are present

---

## Implementation

### Step 1: Update Program.cs

**File:** `TimeReportingApi/Program.cs`

**Find this section (should be around line 20-23):**

```csharp
// Disable default claim type mapping to preserve original JWT claims
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
```

✅ **This is already configured** - No changes needed!

**Verify authentication configuration (around line 94-100):**

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
```

✅ **This is already configured** - No changes needed!

### Step 2: Add Debug Middleware (Temporary)

Add temporary middleware to log ACL claims during development. This helps verify that tokens contain the extension attribute.

**Add BEFORE `app.UseAuthorization()` in Program.cs:**

```csharp
// --- TEMPORARY DEBUG MIDDLEWARE ---
// Remove this after verifying ACL claims are working
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst("oid")?.Value ?? "unknown";
        var aclClaims = context.User.FindAll("extension_TimeReporting_acl")
            .Select(c => c.Value)
            .ToList();

        if (aclClaims.Any())
        {
            Console.WriteLine($"[ACL Debug] User {userId} has {aclClaims.Count} ACL entries:");
            foreach (var acl in aclClaims)
            {
                Console.WriteLine($"  - {acl}");
            }
        }
        else
        {
            Console.WriteLine($"[ACL Debug] User {userId} has NO ACL entries");
        }
    }

    await next();
});
// --- END TEMPORARY DEBUG MIDDLEWARE ---

app.UseAuthorization();
```

**Location in Program.cs:**

```csharp
app.UseHttpsRedirection();
app.UseAuthentication();

// ADD DEBUG MIDDLEWARE HERE (before UseAuthorization)
app.Use(async (context, next) => { /* ... debug code ... */ });

app.UseAuthorization();
app.MapGraphQL();
```

### Step 3: Create Test Query to Verify Claims

Add a simple GraphQL query to verify claims are accessible.

**File:** `TimeReportingApi/GraphQL/Query.cs`

**Add this method to the Query class:**

```csharp
/// <summary>
/// Debug query to test ACL claims (REMOVE IN PRODUCTION)
/// </summary>
[Authorize]
public DebugAclResponse DebugAcl(ClaimsPrincipal user)
{
    var userId = user.FindFirst("oid")?.Value;
    var email = user.FindFirst("email")?.Value;
    var acl = user.GetUserAcl(); // Uses AclExtensions from Task 15.2

    return new DebugAclResponse
    {
        UserId = userId,
        Email = email,
        AclEntries = acl.Select(e => $"{e.Path}={string.Join(",", e.Permissions)}").ToList()
    };
}

public class DebugAclResponse
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<string> AclEntries { get; set; } = new();
}
```

---

## Testing

### Manual Testing with GraphQL Playground

1. **Start the API:**
   ```bash
   /deploy
   ```

2. **Get an access token:**
   ```bash
   TOKEN=$(az account get-access-token \
     --resource api://8b3f87d7-bc23-4932-88b5-f24056999600 \
     --query accessToken -o tsv)
   echo $TOKEN
   ```

3. **Open GraphQL Playground:** http://localhost:5001/graphql

4. **Set Authorization header:**
   ```json
   {
     "Authorization": "Bearer YOUR_TOKEN_HERE"
   }
   ```

5. **Run test query:**
   ```graphql
   query {
     debugAcl {
       userId
       email
       aclEntries
     }
   }
   ```

6. **Expected response:**
   ```json
   {
     "data": {
       "debugAcl": {
         "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
         "email": "your.email@example.com",
         "aclEntries": [
           "Project/INTERNAL=V,A,M",
           "Project/CLIENT-A=V,E,T"
         ]
       }
     }
   }
   ```

7. **Check console output:**
   ```
   [ACL Debug] User a1b2c3d4-e5f6-7890-abcd-ef1234567890 has 2 ACL entries:
     - Project/INTERNAL=V,A,M
     - Project/CLIENT-A=V,E,T
   ```

### Troubleshooting

**Issue: ACL claims are empty**

**Solution 1: Verify token contains claims**
```bash
# Decode token at jwt.ms
TOKEN=$(az account get-access-token --resource api://8b3f87d7-bc23-4932-88b5-f24056999600 --query accessToken -o tsv)
echo $TOKEN | pbcopy  # macOS - copies to clipboard
# Paste at https://jwt.ms
```

**Solution 2: Check claim name**

The full extension attribute name may have a prefix. Check your token at jwt.ms:
- If claim is named `extension_abc123_TimeReporting_acl`, update `AclExtensions.cs`:
  ```csharp
  private const string AclClaimType = "extension_abc123_TimeReporting_acl";
  ```

**Solution 3: Verify token audience**

Ensure you're requesting token for the correct resource:
```bash
# ✅ Correct - uses API app ID
az account get-access-token --resource api://8b3f87d7-bc23-4932-88b5-f24056999600

# ❌ Wrong - uses Microsoft Graph (won't include custom claims)
az account get-access-token --resource https://graph.microsoft.com
```

**Issue: Authentication fails**

**Solution: Check Azure AD configuration**

Verify `appsettings.json`:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR-TENANT-ID",
    "ClientId": "YOUR-CLIENT-ID",
    "Audience": "api://YOUR-CLIENT-ID"
  }
}
```

---

## Integration Points

The token configuration in this task enables:
- **Task 15.2**: `AclExtensions` to read claims from ClaimsPrincipal
- **Task 15.4**: Mutations to perform authorization checks
- **All future resolvers**: Access to user ACL via `ClaimsPrincipal`

---

## Related Files

**Modified:**
- `TimeReportingApi/Program.cs` - Added debug middleware
- `TimeReportingApi/GraphQL/Query.cs` - Added `DebugAcl` query

**Configuration:**
- `appsettings.json` - Azure AD settings (already configured)

---

## Validation

After completing this task:

1. ✅ `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()` is set
2. ✅ Debug middleware logs ACL claims in console
3. ✅ `debugAcl` query returns ACL entries
4. ✅ `extension_TimeReporting_acl` claim is accessible via `ClaimsPrincipal`

---

## Cleanup

**After Phase 15 is complete:**

1. **Remove debug middleware** from `Program.cs`
2. **Remove `DebugAcl` query** from `Query.cs`
3. **Remove console logging** statements

Mark these with `// TODO: Remove after Phase 15 complete` comments.

---

## Next Steps

After completing Task 15.3:
- **Task 15.4:** Update approve/decline mutations with ACL authorization checks

---

## Notes

- **Claim Mapping**: `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()` is **critical** - without it, custom claims may not be accessible
- **Debug Middleware**: Only logs when user is authenticated - unauthenticated requests are skipped
- **Production**: Remove all debug code before deploying to production
- **Performance**: Debug middleware runs on every request - remove it to avoid unnecessary logging overhead

---

## Reference

- [JWT Claims in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims)
- [Microsoft.Identity.Web Configuration](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [JwtSecurityTokenHandler Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler)
