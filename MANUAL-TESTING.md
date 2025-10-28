# Manual Testing Guide - Phase 2

Everything is configured and ready to test! Here's what works:

## Quick Start

```bash
# 1. Database is already running
# (You already ran /db-start)

# 2. Start the API
/run-api

# 3. In another terminal, test endpoints below
```

## ✅ What's Working (Phase 2)

### 1. Health Check (No Auth Required)

```bash
curl http://localhost:5001/health
```

**Expected:** `Healthy`

---

### 2. GraphQL - Without Auth (Should Fail)

```bash
curl http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ hello }"}'
```

**Expected:** `401 Unauthorized - Missing Authorization header`

---

### 3. GraphQL - With Correct Token (Should Work!)

```bash
curl http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer dev-token-12345-for-local-testing" \
  -d '{"query":"{ hello }"}'
```

**Expected:**
```json
{
  "data": {
    "hello": "Hello, GraphQL!"
  }
}
```

---

### 4. GraphQL Playground (Browser)

1. Open: http://localhost:5001/graphql
2. You'll see **Banana Cake Pop** GraphQL IDE
3. Click **Settings** icon (⚙️) → **Request Headers**
4. Add:
   ```json
   {
     "Authorization": "Bearer dev-token-12345-for-local-testing"
   }
   ```
5. Run query:
   ```graphql
   query {
     hello
   }
   ```

**Expected:**
```json
{
  "data": {
    "hello": "Hello, GraphQL!"
  }
}
```

---

## 🔍 Verify Database Schema

```bash
/db-psql
```

Then in psql:

```sql
-- List tables
\dt

-- Expected: projects, project_tasks, tag_configurations, time_entries

-- Check time_entries structure
\d time_entries

-- You should see CHECK constraints:
-- ✅ chk_standard_hours_positive (standard_hours >= 0)
-- ✅ chk_overtime_hours_positive (overtime_hours >= 0)
-- ✅ chk_date_range (start_date <= completion_date)
-- ✅ chk_status_valid (status IN ('NOT_REPORTED', 'SUBMITTED', ...))

-- Exit psql
\q
```

---

## 🧪 Test Database Constraints

While in `/db-psql`:

```sql
-- Create a test project
INSERT INTO projects (code, name, is_active, created_at, updated_at)
VALUES ('TEST', 'Test Project', true, NOW(), NOW());

-- ❌ Try invalid hours (should FAIL)
INSERT INTO time_entries (
  id, project_code, task,
  standard_hours, overtime_hours,
  start_date, completion_date,
  status, tags, created_at, updated_at
)
VALUES (
  gen_random_uuid(), 'TEST', 'Development',
  -5, 0,  -- ❌ Negative hours!
  CURRENT_DATE, CURRENT_DATE,
  'NOT_REPORTED', '[]'::jsonb, NOW(), NOW()
);
-- Expected: ERROR: violates check constraint "chk_standard_hours_positive"

-- ✅ Try valid entry (should SUCCEED)
INSERT INTO time_entries (
  id, project_code, task,
  standard_hours, overtime_hours,
  start_date, completion_date,
  status, tags, created_at, updated_at
)
VALUES (
  gen_random_uuid(), 'TEST', 'Development',
  8.0, 0,  -- ✅ Valid hours
  CURRENT_DATE, CURRENT_DATE,
  'NOT_REPORTED', '[]'::jsonb, NOW(), NOW()
);
-- Expected: INSERT 0 1 (success!)

-- Verify it was inserted
SELECT * FROM time_entries;
```

---

## 🛑 Stop Testing

```bash
# Stop the API
/stop-api

# Optionally stop database
/db-stop
```

---

## 🚀 What's Next?

**Phase 2 Complete ✅**
- Database schema with constraints
- API with authentication
- Health check endpoint
- GraphQL endpoint (basic)

**Phase 3 Coming:**
- `timeEntries` query with filters
- `projects` query
- Full GraphQL schema
- Query resolvers

---

## 📋 Bearer Token Reference

**Development Token:** `dev-token-12345-for-local-testing`

(This is configured in `TimeReportingApi/appsettings.Development.json`)

**⚠️ Never use this token in production!**
