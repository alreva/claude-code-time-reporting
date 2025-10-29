# Task 6.3: Environment Configuration

**Phase:** 6 - GraphQL API - Docker
**Estimated Time:** 30 minutes
**Prerequisites:** Task 6.2 complete (Docker Compose updated with API service)
**Status:** Pending

## Objective

Establish a robust environment configuration strategy that:
- Separates development and production configurations
- Protects sensitive credentials and secrets
- Follows 12-factor app methodology
- Provides clear documentation for required environment variables
- Supports local development and Docker deployment scenarios

## Acceptance Criteria

- [ ] `.env.example` created with all required variables (safe to commit)
- [ ] `.env` file documented in `.gitignore` (already done, verify)
- [ ] `appsettings.Production.json` created for production-specific settings
- [ ] Environment variable documentation created (`docs/ENVIRONMENT.md`)
- [ ] Bearer token generation script created (`scripts/generate-token.sh`)
- [ ] All secrets removed from committed files
- [ ] Local development still works with `.env` file
- [ ] Docker Compose can override settings via environment variables
- [ ] Clear separation between development and production configurations

## Implementation

### Step 1: Create .env.example (Safe Template)

Create `.env.example` at repository root with placeholder values:

```bash
# .env.example - Template for environment variables
# Copy this file to .env and fill in actual values
# DO NOT commit .env with real secrets!

#######################
# PostgreSQL Database
#######################
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password_here
POSTGRES_DB=time_reporting

#######################
# GraphQL API
#######################
# ASP.NET Core environment: Development, Staging, Production
ASPNETCORE_ENVIRONMENT=Production

# Bearer token for API authentication (generate with scripts/generate-token.sh)
BEARER_TOKEN=your_bearer_token_here

#######################
# Optional: Local Development Overrides
#######################
# Uncomment if running API locally (not in Docker)
# API_PORT=5001
# DB_HOST=localhost
# DB_PORT=5432
```

### Step 2: Create Production appsettings

Create `TimeReportingApi/appsettings.Production.json` for production-specific settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "TimeReportingDb": "Host=postgres;Port=5432;Database=time_reporting;Username=postgres;Password=postgres"
  },
  "Authentication": {
    "BearerToken": "will-be-overridden-by-env-var"
  }
}
```

**Note:** The actual bearer token will be provided via `Authentication__BearerToken` environment variable in Docker Compose.

### Step 3: Create Bearer Token Generation Script

Create `scripts/generate-token.sh` for easy token generation:

```bash
#!/usr/bin/env bash
# Generate a secure random bearer token for API authentication

set -euo pipefail

# Generate 32 bytes of random data, encode as base64
TOKEN=$(openssl rand -base64 32)

echo "Generated Bearer Token:"
echo "======================"
echo "$TOKEN"
echo ""
echo "Add this to your .env file:"
echo "BEARER_TOKEN=$TOKEN"
echo ""
echo "Use this in API requests:"
echo "Authorization: Bearer $TOKEN"
```

Make it executable:
```bash
chmod +x scripts/generate-token.sh
```

### Step 4: Create Environment Documentation

Create `docs/ENVIRONMENT.md` to document all environment variables:

```markdown
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
| `BEARER_TOKEN` | API authentication token | None | Yes |

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
BEARER_TOKEN=$(./scripts/generate-token.sh | tail -1 | cut -d'=' -f2)
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
BEARER_TOKEN=your_bearer_token
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
# Load token from .env
source .env

# Test API with bearer token
curl -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $BEARER_TOKEN" \
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
1. Verify `.env` has correct `BEARER_TOKEN` value
2. Restart API: `podman compose restart api`
3. Test with correct token: `curl -H "Authorization: Bearer $BEARER_TOKEN" ...`

### Issue: API can't connect to database

**Cause:** Wrong connection string
**Solution:**
1. Check `docker-compose.yml` has `ConnectionStrings__TimeReportingDb` with `Host=postgres`
2. Verify database is healthy: `podman compose ps postgres`
3. Check network: `podman network ls`

### Issue: Environment variables not loading

**Cause:** `.env` file not in correct location
**Solution:**
1. Ensure `.env` is in repository root (same directory as `docker-compose.yml`)
2. Verify `.env` syntax: no spaces around `=`, no quotes for values
3. Restart Docker Compose: `podman compose down && podman compose up -d`

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [12-Factor App: Config](https://12factor.net/config)
- [Docker Compose Environment Variables](https://docs.docker.com/compose/environment-variables/)
- [OpenSSL Random Number Generation](https://www.openssl.org/docs/man1.1.1/man1/rand.html)
```

### Step 5: Verify .gitignore Includes .env

Check that `.gitignore` includes `.env`:

```bash
# Check if .env is ignored
grep "^\.env$" .gitignore || echo ".env" >> .gitignore

# Verify .env is not tracked
git ls-files | grep "^\.env$" && echo "WARNING: .env is tracked!" || echo "OK: .env is ignored"
```

If `.env` is already committed, remove it:
```bash
git rm --cached .env
git commit -m "Remove .env from version control"
```

### Step 6: Create scripts directory

```bash
mkdir -p scripts
```

## Testing

### Test Cases

#### 1. .env.example is Safe to Commit
```bash
# Check for real secrets in .env.example
grep -i "C5ZoARiAp\|postgres" .env.example
```
**Expected:** Only placeholder values, no real secrets

#### 2. .env is Ignored by Git
```bash
git status .env
```
**Expected:** "No such file or directory" or not listed as untracked

#### 3. Token Generation Works
```bash
./scripts/generate-token.sh
```
**Expected:** Outputs 44-character base64 string

#### 4. Token is Cryptographically Random
```bash
# Generate 3 tokens, should all be different
./scripts/generate-token.sh | grep "^BEARER_TOKEN=" | cut -d'=' -f2
./scripts/generate-token.sh | grep "^BEARER_TOKEN=" | cut -d'=' -f2
./scripts/generate-token.sh | grep "^BEARER_TOKEN=" | cut -d'=' -f2
```
**Expected:** Three different tokens

#### 5. Environment Variables Override appsettings
```bash
# Start API with custom token
export BEARER_TOKEN="test_token_123"
podman compose up -d api

# Check logs for environment override
podman compose logs api | grep -i "environment"
```
**Expected:** API uses `BEARER_TOKEN` from environment, not appsettings.json

#### 6. Production appsettings Loaded
```bash
# Set production environment
export ASPNETCORE_ENVIRONMENT=Production
podman compose restart api

# Check logs
podman compose logs api | grep -i "appsettings.Production.json"
```
**Expected:** Logs show loading `appsettings.Production.json`

#### 7. Documentation is Clear
```bash
# Verify docs/ENVIRONMENT.md is readable
cat docs/ENVIRONMENT.md
```
**Expected:** Clear, well-formatted documentation

### TDD Workflow for Environment Configuration

```
1. Create .env.example (safe template)
2. Verify .env.example has no secrets (grep test)
3. Create token generation script
4. Test token generation (run script, check output)
5. Create documentation
6. Verify documentation clarity (manual review)
7. Test environment variable loading (docker-compose test)
```

## Security Considerations

### Secrets in Version Control

**What to commit:**
- ✅ `.env.example` (template with placeholders)
- ✅ `appsettings.json` (default/development settings)
- ✅ `appsettings.Production.json` (structure, no secrets)
- ✅ Documentation (`docs/ENVIRONMENT.md`)
- ✅ Scripts (`scripts/generate-token.sh`)

**What NOT to commit:**
- ❌ `.env` (contains real secrets)
- ❌ `appsettings.Development.json` with real database passwords
- ❌ Any file with real bearer tokens

### Token Strength

A secure bearer token should have:
- **Entropy:** At least 128 bits (16 bytes)
- **Encoding:** Base64 for URL safety (32 bytes → 44 characters)
- **Generation:** Cryptographically secure random (OpenSSL, not `Math.random()`)

**Good token:**
```
YOUR_BEARER_TOKEN_HERE
```

**Bad token:**
```
test123
mytoken
admin_token_2024
```

### Rotation Strategy

**Development tokens:** Rotate every 90 days or when:
- Developer leaves team
- Token accidentally exposed
- Security audit recommends

**Production tokens:** Rotate every 30 days or when:
- Suspected compromise
- Compliance requirement
- Staff changes

## Related Files

**Created:**
- `.env.example` - Safe template for environment variables
- `scripts/generate-token.sh` - Token generation script
- `docs/ENVIRONMENT.md` - Environment configuration documentation
- `TimeReportingApi/appsettings.Production.json` - Production settings

**Modified:**
- `.gitignore` - Ensure `.env` is ignored (verify only)

**Referenced:**
- `.env` - Actual environment variables (not committed)
- `docker-compose.yml` - Reads environment variables
- `TimeReportingApi/appsettings.json` - Base configuration
- `TimeReportingApi/Program.cs` - Configuration loading

## Next Steps

After completing this task:
1. Proceed to **Task 6.4: Integration Test** to verify the full Docker stack
2. Test end-to-end workflow with GraphQL queries and mutations
3. Validate environment variable overrides work correctly

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [12-Factor App Methodology](https://12factor.net/)
- [OWASP Secret Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
- [Docker Compose Environment Variables](https://docs.docker.com/compose/environment-variables/)

---

**Time Estimate Breakdown:**
- Create .env.example: 5 min
- Create token generation script: 5 min
- Create appsettings.Production.json: 5 min
- Create ENVIRONMENT.md documentation: 10 min
- Testing and verification: 5 min
- **Total: 30 minutes**
