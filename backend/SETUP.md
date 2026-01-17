# TallyJ 4 - Developer Setup Guide

This guide will help you set up the TallyJ 4 backend for local development.

## Prerequisites

### Required Software

1. **.NET SDK 9.0 or later**
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **SQL Server Express** (or SQL Server Developer Edition)
   - Download SQL Server Express: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
   - Or use Docker: `docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`

3. **Optional: SQL Server Management Studio (SSMS)** or **Azure Data Studio**
   - For database inspection and querying
   - SSMS: https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms
   - Azure Data Studio: https://learn.microsoft.com/en-us/azure-data-studio/download-azure-data-studio

## Quick Start

### 1. Install SQL Server Express

If you don't have SQL Server installed:

**Windows:**
1. Download SQL Server Express from the link above
2. Run the installer and choose "Basic" installation
3. Note the instance name (usually `SQLEXPRESS`)
4. The default connection will be: `localhost\SQLEXPRESS`

**Docker (Cross-platform):**
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

Connection string for Docker: `Server=localhost,1433;Database=TallyJ4Dev;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True`

### 2. Configure Connection String

The application uses `appsettings.Development.json` for local development.

**Default configuration** (works with SQL Express):
```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=localhost\\SQLEXPRESS;Database=TallyJ4Dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

If using Docker or SQL authentication, update to:
```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=localhost,1433;Database=TallyJ4Dev;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Alternative: Use User Secrets** (recommended for sensitive data):
```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:TallyJ4" "Server=localhost\\SQLEXPRESS;Database=TallyJ4Dev;Trusted_Connection=True;TrustServerCertificate=True"
```

### 3. Create Database

```bash
cd backend
dotnet ef database update
```

This command:
- Creates the `TallyJ4Dev` database
- Applies all migrations
- Creates all tables, indexes, and constraints

### 4. Run Application (Auto-seeds data)

```bash
cd backend
dotnet run
```

On first startup, the application will automatically:
- Create 3 test users (admin, teller, voter)
- Create 2 test elections with voters, ballots, and votes
- Seed supporting data (messages, logs, etc.)

You should see logs like:
```
[INFO] Starting database seeding...
[INFO] Seeding users...
[INFO] Created user: admin@tallyj.test
[INFO] Seeding Election 1: Springfield LSA...
[INFO] Seeding Election 2: National Convention...
[INFO] Database seeding complete
```

### 5. Verify Setup

**Test Authentication:**
```bash
# Test login
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tallyj.test","password":"TestPass123!"}'
```

Expected response includes `accessToken` and `refreshToken`.

**Test Protected Endpoint:**
```bash
# Replace <TOKEN> with the accessToken from login response
curl -X GET http://localhost:5000/protected \
  -H "Authorization: Bearer <TOKEN>"
```

Expected response: `"This is protected!"`

## Database Management

### View Database Schema

**Using SSMS or Azure Data Studio:**
1. Connect to: `localhost\SQLEXPRESS` (Windows Authentication)
2. Database: `TallyJ4Dev`
3. Explore tables under "Tables" node

**Using Command Line:**
```bash
dotnet ef dbcontext info
```

### Reset Database

If you need to start fresh:

**PowerShell (Windows):**
```powershell
cd backend\scripts
.\reset-database.ps1
```

**Bash (Linux/macOS):**
```bash
cd backend/scripts
chmod +x reset-database.sh
./reset-database.sh
```

Or manually:
```bash
cd backend
dotnet ef database drop --force
dotnet ef database update
dotnet run  # Re-seed data
```

### Disable Auto-Seeding

Edit `appsettings.Development.json`:
```json
{
  "Database": {
    "SeedOnStartup": false
  }
}
```

## Test User Credentials

The seeder creates these users:

| Email | Password | Purpose |
|-------|----------|---------|
| `admin@tallyj.test` | `TestPass123!` | System administrator |
| `teller@tallyj.test` | `TestPass123!` | Election teller/coordinator |
| `voter@tallyj.test` | `TestPass123!` | Regular voter |

## Seeded Elections

### Election 1: Springfield LSA Election 2024
- **Type**: Local Spiritual Assembly
- **Status**: Active (voting in progress)
- **Voters**: 30 people
- **Ballots**: 20 (15 in-person, 5 online)
- **Locations**: Main Hall, Community Center

### Election 2: National Convention 2024  
- **Type**: Convention (delegates only)
- **Status**: Finalized (results calculated)
- **Delegates**: 15 people
- **Ballots**: 15 (all processed)
- **Results**: Top 9 elected, includes tie scenario

## SQL Queries for Verification

```sql
-- Check record counts
SELECT 'Users' AS Entity, COUNT(*) AS Count FROM AspNetUsers
UNION ALL SELECT 'Elections', COUNT(*) FROM Election
UNION ALL SELECT 'People', COUNT(*) FROM Person
UNION ALL SELECT 'Ballots', COUNT(*) FROM Ballot
UNION ALL SELECT 'Votes', COUNT(*) FROM Vote
UNION ALL SELECT 'Results', COUNT(*) FROM Result;

-- View Springfield LSA data
SELECT * FROM Election WHERE Name LIKE '%Springfield%';
SELECT * FROM Person WHERE ElectionGuid IN (SELECT ElectionGuid FROM Election WHERE Name LIKE '%Springfield%');

-- View computed columns
SELECT FirstName, LastName, _FullName, _FullNameFL FROM Person WHERE ElectionGuid IN (SELECT TOP 1 ElectionGuid FROM Election);

-- View ballot codes
SELECT ComputerCode, BallotNumAtComputer, _BallotCode FROM Ballot;

-- View National Convention results
SELECT p._FullName, r.VoteCount, r.Rank, r.Section
FROM Result r
JOIN Person p ON r.PersonGuid = p.PersonGuid
WHERE r.ElectionGuid IN (SELECT ElectionGuid FROM Election WHERE Name LIKE '%Convention%')
ORDER BY r.Rank;
```

## Troubleshooting

### Connection Errors

**Error**: `Cannot open database "TallyJ4Dev" requested by the login. The login failed.`

**Solution**: Verify SQL Server is running:
```bash
# Windows
Get-Service MSSQL*

# Check connection
dotnet ef dbcontext info
```

### Migration Errors

**Error**: `Unable to create a 'DbContext' of type 'MainDbContext'`

**Solution**: Ensure connection string is correct in `appsettings.Development.json`

### Seeding Errors

**Error**: `Database already seeded`

This is normal - seeding is idempotent. To reseed, drop and recreate the database using the reset script.

### Port Already in Use

**Error**: `Address already in use` or `Failed to bind to address`

**Solution**: Another process is using port 5000. Either stop that process or change the port in `launchSettings.json`.

## EF Core Commands Reference

```bash
# List migrations
dotnet ef migrations list

# Add new migration
dotnet ef migrations add <MigrationName>

# Remove last migration (not applied)
dotnet ef migrations remove

# Update database to latest migration
dotnet ef database update

# Update database to specific migration
dotnet ef database update <MigrationName>

# Drop database
dotnet ef database drop

# Generate SQL script
dotnet ef migrations script --output schema.sql

# Get DbContext info
dotnet ef dbcontext info
```

## Next Steps

1. **Explore the API**: Check `/auth/register`, `/auth/login` endpoints
2. **Inspect Database**: Use SSMS to view tables and data
3. **Run Tests**: (when implemented) `dotnet test`
4. **Build Frontend**: See `../frontend/README.md`

## Additional Resources

- [.NET EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [TallyJ Reverse Engineering Docs](../.zenflow/tasks/reverse-engineer-and-design-new-cd6a/)

## Support

For issues or questions, see the main [README.md](../README.md).
