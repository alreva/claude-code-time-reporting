---
description: Connect to PostgreSQL database
allowed-tools: Bash(psql:*)
---

Connect to the PostgreSQL database using psql client.

### Execution

```bash
psql -h localhost -U postgres -d time_reporting
```

### Interactive Session

Once connected, useful commands:
- `\dt` - List tables
- `\d time_entries` - Describe time_entries table
- `\q` - Quit
- `SELECT * FROM projects;` - Query projects

### Expected Output

- Opens interactive psql session
- Shows PostgreSQL prompt: `time_reporting=#`

### Notes

- Requires psql client installed
- Password may be required (check .env file)
- Press Ctrl+D or type `\q` to exit
