# Environment Configuration

This document describes all environment variables used by the Time Reporting System.

## Quick Start

1. Authenticate with Azure:
   ```bash
   az login
   ```

2. Run setup script to create environment file:
   ```bash
   ./setup.sh
   source env.sh
   ```

3. **IMPORTANT:** Authentication uses Azure Entra ID - no bearer tokens needed!

## Required Variables

### PostgreSQL Database

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `POSTGRES_USER` | PostgreSQL username | `postgres` | Yes |
| `POSTGRES_PASSWORD` | PostgreSQL password | `postgres` | Yes |
| `POSTGRES_DB` | Database name | `time_reporting` | Yes |

**Production Recommendation:** Use strong passwords (16+ characters, mixed case, numbers, symbols)

### GraphQL API

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` | No |

**Authentication:**
- Uses Azure Entra ID with JWT token validation
- API validates tokens using Microsoft.Identity.Web
- No bearer token configuration needed
- Azure AD tenant and client ID configured in `appsettings.json`

## Environment-Specific Configuration

### Development (Local)

For local development outside Docker:

```bash
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=time_reporting
GRAPHQL_API_URL=http://localhost:5001/graphql
```

Connection string uses `Host=localhost`:
- Defined in `appsettings.json`
- API runs on port 5001 (avoid macOS AirPlay port 5001)

**Authentication:**
- Run `az login` before starting development
- MCP Server uses AzureCliCredential to acquire tokens
- API validates tokens with Azure Entra ID

### Docker Compose

For Docker Compose deployment:

```bash
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
POSTGRES_DB=time_reporting
```

Connection string uses `Host=postgres` (service name):
- Overridden via `ConnectionStrings__TimeReportingDb` in docker-compose.yml
- Both services on same Docker network

### Production (Future: Kubernetes/Cloud)

For production deployments, use secret management:
- **Kubernetes:** Use Secrets and ConfigMaps
- **Azure:** Use Azure Key Vault
- **AWS:** Use AWS Secrets Manager
- **Google Cloud:** Use Secret Manager

Never store production secrets in `.env` files or environment variables directly.

## Configuration Hierarchy

ASP.NET Core loads configuration in this order (later overrides earlier):

1. `appsettings.json` (base settings)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables (highest priority)

**Example:**

`appsettings.json`:
```json
{
  "ConnectionStrings": {
    "TimeReportingDb": "Host=localhost;..."
  }
}
```

Environment variable override:
```bash
ConnectionStrings__TimeReportingDb="Host=postgres;..."
```

Result: Environment variable wins, API connects to `postgres` host.

## Security Best Practices

### DO ✅

- ✅ Use strong, randomly-generated passwords
- ✅ Generate bearer tokens with `openssl rand -base64 32`
- ✅ Keep `.env` in `.gitignore`
- ✅ Use `.env.example` for documenting required variables
- ✅ Rotate credentials regularly
- ✅ Use secret management in production (Key Vault, Secrets Manager)
- ✅ Limit environment variable exposure (principle of least privilege)

### DON'T ❌

- ❌ Commit `.env` file with real secrets
- ❌ Use weak passwords like "password123"
- ❌ Share bearer tokens in Slack/email
- ❌ Hardcode secrets in source code
- ❌ Use development credentials in production
- ❌ Log sensitive environment variables

## Verifying Configuration

### Check Loaded Configuration

```bash
# Start API and check logs for configuration source
podman compose logs api | grep -i "appsettings\|environment"
```

### Test Azure AD Authentication

```bash
# Ensure you're logged in to Azure
az login

# Get Azure AD token
TOKEN=$(az account get-access-token --resource api://8b3f87d7-bc23-4932-88b5-f24056999600 --query accessToken -o tsv)

# Test API with Azure AD token
curl -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"query":"{ projects { code name } }"}'
```

Expected: Valid GraphQL response with project data

### Test Database Connection

```bash
# Check API logs for database connection
podman compose logs api | grep -i "database\|postgres"
```

Expected: No connection errors

## Troubleshooting

### Issue: API returns 401 Unauthorized

**Cause:** Azure AD authentication issue
**Solution:**
1. Verify you're logged in: `az login`
2. Check Azure AD configuration in `appsettings.json` (tenant ID, client ID)
3. Get fresh token: `az account get-access-token --resource api://8b3f87d7-bc23-4932-88b5-f24056999600`
4. Verify API Azure AD middleware is configured in `Program.cs`

### Issue: API can't connect to database

**Cause:** Wrong connection string
**Solution:**
1. Check `docker-compose.yml` has `ConnectionStrings__TimeReportingDb` with `Host=postgres`
2. Verify database is healthy: `podman compose ps postgres`
3. Check network: `podman network ls`

### Issue: Environment variables not loading

**Cause:** Environment variables not sourced into shell
**Solution:**
1. Ensure you run `source env.sh` before starting Docker Compose
2. Verify `env.sh` exists in repository root: `ls -la env.sh`
3. Check variables are in environment: `echo $GRAPHQL_API_URL`
4. Restart Docker Compose: `podman compose down && podman compose up -d`

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [12-Factor App: Config](https://12factor.net/config)
- [Docker Compose Environment Variables](https://docs.docker.com/compose/environment-variables/)
- [Azure Identity Library](https://learn.microsoft.com/en-us/dotnet/api/azure.identity)
- [Microsoft.Identity.Web](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
