# Task 1.3: Docker Compose PostgreSQL Setup

**Phase:** 1 - Database & Infrastructure
**Estimated Time:** 1 hour
**Prerequisites:** Task 1.1, Task 1.2
**Status:** Pending

---

## Objective

Set up PostgreSQL in Docker using Docker Compose with persistent volume and automatic schema initialization.

---

## Acceptance Criteria

- [ ] Docker Compose file created
- [ ] PostgreSQL service configured with persistent volume
- [ ] Schema and seed scripts run automatically on first start
- [ ] Health check configured
- [ ] Database accessible from host machine
- [ ] `docker-compose up` successfully starts PostgreSQL with data

---

## Implementation

Create file: `docker-compose.yml` (in project root)

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: time-reporting-postgres
    environment:
      POSTGRES_DB: time_reporting
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD:-postgres}
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./db/schema:/docker-entrypoint-initdb.d
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d time_reporting"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
    restart: unless-stopped

volumes:
  pgdata:
    driver: local
```

Create file: `.env`

```bash
# PostgreSQL Configuration
DB_PASSWORD=your_secure_password_here

# API Configuration (for future tasks)
Azure AD via AzureCliCredential=
```

Create file: `.env.example`

```bash
# PostgreSQL Configuration
DB_PASSWORD=postgres

# API Configuration
Azure AD via AzureCliCredential=your_Azure AD token_here
```

---

## Testing

```bash
# Start PostgreSQL
docker-compose up -d postgres

# Check status
docker-compose ps

# Check logs
docker-compose logs postgres

# Connect to database
docker-compose exec postgres psql -U postgres -d time_reporting

# Inside psql, verify schema and data
\dt
SELECT * FROM projects;
\q

# Stop and remove (keeps volume)
docker-compose down

# Remove everything including data
docker-compose down -v
```

---

## Related Files

- **Docker Compose:** `docker-compose.yml`
- **Environment:** `.env`, `.env.example`
- **Schema Scripts:** `db/schema/01-create-tables.sql`, `db/schema/02-seed-data.sql`

---

## Next Steps

- ✅ Proceed to **Phase 2** - GraphQL API Core Setup
