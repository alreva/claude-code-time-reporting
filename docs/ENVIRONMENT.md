# Environment Configuration

This document describes all environment variables used by the Time Reporting System.

## Quick Start

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Generate a secure bearer token:
   ```bash
   ./scripts/generate-token.sh
   ```

3. Edit `.env` and replace placeholder values with real credentials

4. **IMPORTANT:** Never commit `.env` file with real secrets!

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
| `Authentication__BearerToken` | API authentication token | None | Yes |

**Bearer Token Generation:**
```bash
./scripts/generate-token.sh
```

The bearer token must be:
- At least 32 bytes (base64 encoded = 44 characters)
- Randomly generated (use `openssl rand -base64 32`)
- Kept secret (never commit to version control)

## Environment-Specific Configuration

### Development (Local)

For local development outside Docker:

```bash
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=time_reporting
Authentication__BearerToken=$(./scripts/generate-token.sh | tail -1 | cut -d'=' -f2)
```

Connection string uses `Host=localhost`:
- Defined in `appsettings.json`
- API runs on port 5001 (avoid macOS AirPlay port 5001)

### Docker Compose

For Docker Compose deployment:

```bash
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
POSTGRES_DB=time_reporting
Authentication__BearerToken=your_bearer_token
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

### Test Bearer Token

```bash
# Load token from env.sh
source env.sh

# Test API with bearer token
curl -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $Authentication__BearerToken" \
  -d '{"query":"{ projects { code name } }"}'
```

Expected: Valid GraphQL response

### Test Database Connection

```bash
# Check API logs for database connection
podman compose logs api | grep -i "database\|postgres"
```

Expected: No connection errors

## Troubleshooting

### Issue: API returns 401 Unauthorized

**Cause:** Bearer token mismatch
**Solution:**
1. Verify `env.sh` has correct `Authentication__BearerToken` value
2. Restart API: `podman compose restart api`
3. Test with correct token: `curl -H "Authorization: Bearer $Authentication__BearerToken" ...`

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
3. Check variables are in environment: `echo $Authentication__BearerToken`
4. Restart Docker Compose: `podman compose down && podman compose up -d`

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [12-Factor App: Config](https://12factor.net/config)
- [Docker Compose Environment Variables](https://docs.docker.com/compose/environment-variables/)
- [OpenSSL Random Number Generation](https://www.openssl.org/docs/man1.1.1/man1/rand.html)
