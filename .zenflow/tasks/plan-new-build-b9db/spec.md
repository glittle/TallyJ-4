# Technical Specification: TallyJ 4 Database Layer Implementation

## 1. Technical Context

### 1.1 Current State

**Project Structure**:

- **Backend**: `backend/` - .NET 9.0 ASP.NET Core Web API
- **Frontend**: `frontend/` - Vue 3 + Vite (boilerplate, not part of this phase)

**Technology Stack**:

- **.NET**: 9.0 (latest LTS)
- **ORM**: Entity Framework Core 9.0.10
- **Database**: SQL Server (via `Microsoft.EntityFrameworkCore.SqlServer`)
- **Identity**: ASP.NET Core Identity 9.0.10 (configured with JWT bearer auth)
- **Logging**: Serilog 4.3.0 with console and SQL Server sinks

**Existing Components**:

1. **Entities** (`backend/EF/Models/`):

   - 16 domain entities fully defined with data annotations
   - Ballot.cs, Election.cs, ImportFile.cs, JoinElectionUser.cs, Location.cs, Log.cs, Message.cs, OnlineVoter.cs, OnlineVotingInfo.cs, Person.cs, Result.cs, ResultSummary.cs, ResultTie.cs, SmsLog.cs, Teller.cs, Vote.cs

2. **DbContext** (`backend/EF/Context/MainDbContext.cs`):

   - Inherits from `IdentityDbContext<AppUser>`
   - DbSet properties for all 16 entities
   - `OnModelCreating` configured with:
     - Computed columns (Ballot.BallotCode, Person.FullName, Person.FullNameFl, ImportFile.FileSize, etc.)
     - Row versioning (concurrency tokens)
     - Foreign key relationships
     - Default values
     - Unique indexes (filtered for Person.Email, Person.Phone)

3. **Identity** (`backend/EF/Identity/AppUser.cs`):

   - Custom user class (currently minimal, extends `IdentityUser`)

4. **Configuration** (`backend/Program.cs`):
   - DbContext registered with connection string "TallyJ3"
   - ASP.NET Core Identity endpoints configured (`/auth/register`, `/auth/login`, etc.)
   - JWT bearer authentication configured
   - Logging configured (password masking in connection strings)

**NuGet Packages** (from `TallyJ4.csproj`):

- Microsoft.EntityFrameworkCore.SqlServer 9.0.10
- Microsoft.EntityFrameworkCore.Design 9.0.10
- Microsoft.EntityFrameworkCore.Tools 9.0.10
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.10
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0.10
- Serilog.\* packages (4.3.0+)
- Swashbuckle.AspNetCore 9.0.6 (OpenAPI/Swagger)

**Current appsettings.json**:

- ❌ No connection string configured yet
- Basic logging configuration
- AllowedHosts: "\*"

### 1.2 Reference Documentation

**Comprehensive Documentation** (~55,000 lines) exists in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`:

- Database schema (entities.md) - full entity documentation from original system
- Business logic (tally-algorithms.md) - tally calculation logic
- Authentication systems (authentication.md) - 3 auth systems
- API endpoints - all 12 controllers documented
- SignalR hubs - all 10 hubs documented

**Key Reference**:

- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/entities.md` - authoritative source for entity definitions

### 1.3 Target Environment

**Development**:

- Windows/Linux/macOS with .NET 9.0 SDK
- SQL Server Express (free edition) or SQL Server Developer Edition
- Visual Studio 2022, VS Code, or Rider

**Production** (future):

- Azure SQL Database or SQL Server (Standard/Enterprise)
- Docker containers (Linux or Windows)
- Azure App Service or Kubernetes

---

## 2. Implementation Approach

### 2.1 Overview

This specification covers **Phase 1: Database Foundation** only:

1. Configure connection strings for SQL Express
2. Create initial EF Core migration
3. Implement database seeding with realistic test data
4. Provide developer setup scripts/documentation

**Out of Scope** (future phases):

- API endpoint implementation (beyond existing `/auth/*` endpoints)
- Business logic services
- Frontend development
- SignalR hubs
- External integrations

### 2.2 Database Migration Strategy

#### 2.2.1 Code-First Approach

**Rationale**:

- Entity models already defined in code
- DbContext configuration complete
- Easier to version control and deploy
- Better for team collaboration

**Tools**:

- `dotnet ef migrations` CLI (part of EF Core Tools)
- PowerShell/Bash scripts for common operations

#### 2.2.2 Migration Creation

**Initial Migration**:

```bash
dotnet ef migrations add InitialCreate --project backend/TallyJ4.csproj --output-dir EF/Migrations
```

**Expected Output**:

- Migration file: `backend/EF/Migrations/YYYYMMDDHHMMSS_InitialCreate.cs`
- Designer file: `backend/EF/Migrations/YYYYMMDDHHMMSS_InitialCreate.Designer.cs`
- Model snapshot: `backend/EF/Migrations/MainDbContextModelSnapshot.cs`

**Migration Content Must Include**:

1. All 16 entity tables with correct column types, lengths, nullable constraints
2. All ASP.NET Identity tables (AspNetUsers, AspNetRoles, etc.)
3. Primary keys (RowId as identity, GUID alternate keys)
4. Foreign keys with proper ON DELETE behavior
5. Indexes (unique, filtered, composite)
6. Computed columns with `AS` formulas (persisted and non-persisted)
7. Default value constraints
8. Row version columns (timestamp/rowversion)

**Verification Steps**:

1. Inspect generated migration SQL (use `dotnet ef migrations script`)
2. Verify computed columns use SQL formulas (not C# expressions)
3. Verify filtered indexes include `WHERE` clauses
4. Check foreign key relationships match MainDbContext configuration

#### 2.2.3 Database Creation/Update

**Development Workflow**:

```bash
# Create/update database
dotnet ef database update --project backend/TallyJ4.csproj

# Check current migration status
dotnet ef migrations list --project backend/TallyJ4.csproj

# Generate SQL script (for manual review or production deployment)
dotnet ef migrations script --project backend/TallyJ4.csproj --output db-script.sql
```

**Database Naming**:

- Development: `TallyJ4Dev` or `TallyJ4`
- Testing: `TallyJ4Test`
- Production: `TallyJ4` or client-specific name

#### 2.2.4 Database Reset Capability

**PowerShell Script** (`backend/scripts/reset-database.ps1`):

```powershell
# Drop and recreate database
dotnet ef database drop --project backend/TallyJ4.csproj --force
dotnet ef database update --project backend/TallyJ4.csproj
Write-Host "Database reset complete. Run the application to seed data."
```

**Bash Script** (`backend/scripts/reset-database.sh`):

```bash
#!/bin/bash
dotnet ef database drop --project backend/TallyJ4.csproj --force
dotnet ef database update --project backend/TallyJ4.csproj
echo "Database reset complete. Run the application to seed data."
```

### 2.3 Database Seeding Strategy

#### 2.3.1 Seeder Architecture

**Location**: `backend/EF/Data/DbSeeder.cs`

**Class Structure**:

```csharp
namespace TallyJ4.EF.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        MainDbContext context,
        UserManager<AppUser> userManager,
        ILogger logger)
    {
        // Check if already seeded
        if (await context.Elections.AnyAsync())
        {
            logger.LogInformation("Database already seeded");
            return;
        }

        await SeedUsersAsync(userManager, logger);
        await SeedElection1Async(context, logger); // Springfield LSA
        await SeedElection2Async(context, logger); // National Convention (completed)

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeding complete");
    }

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager, ILogger logger) { }
    private static async Task SeedElection1Async(MainDbContext context, ILogger logger) { }
    private static async Task SeedElection2Async(MainDbContext context, ILogger logger) { }
}
```

**Integration** (`backend/Program.cs`):

```csharp
var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await context.Database.MigrateAsync(); // Apply pending migrations
    await DbSeeder.SeedAsync(context, userManager, logger);
}
```

#### 2.3.2 Seed Data Specification

**Users** (3 users via UserManager):

1. **Admin User**:

   - Email: `admin@tallyj.test`
   - Username: `admin@tallyj.test`
   - Password: `Admin@123`
   - EmailConfirmed: true
   - Id: Deterministic GUID (e.g., from email hash)

2. **Teller User**:

   - Email: `teller@tallyj.test`
   - Password: `Teller@123`
   - EmailConfirmed: true

3. **Voter User**:
   - Email: `voter@tallyj.test`
   - Password: `Voter@123`
   - EmailConfirmed: true

**Election 1: Springfield LSA Election 2024** (Active):

- ElectionGuid: Deterministic GUID
- Name: "Springfield Local Spiritual Assembly Election 2024"
- ElectionType: "LSA" (Local Spiritual Assembly)
- ElectionMode: "I" (In-Person)
- NumberToElect: 9
- DateOfElection: Today or recent past
- TallyStatus: "Tallying" or "Voting"
- OnlineWhenOpen: 7 days ago
- OnlineWhenClose: 3 days from now
- VotingMethods: "IP,OL" (In-Person, Online)
- OwnerLoginId: admin@tallyj.test

**Locations for Election 1** (2 locations):

1. Main Hall (LocationGuid, Name, ElectionGuid)
2. Community Center

**People for Election 1** (30 people):

- Mix of names: Smith, Johnson, Williams, Jones, Brown, Davis, Miller, Wilson, Moore, Taylor, Anderson, Thomas, etc.
- FirstName + LastName (some with OtherNames, OtherLastNames)
- Age 21+ (AgeGroup: "A" for adult)
- CanVote: true for most (28), false for 2
- CanReceiveVotes: true for all
- Some with Email (20/30)
- Some with Phone (15/30)
- Some with both (10/30)
- VotingMethod: mix of "I" (in-person), "O" (online), "K" (kiosk)
- BahaiId: random 9-digit numbers for some

**Ballots for Election 1** (20 ballots):

- 15 in-person ballots:

  - LocationGuid: split between two locations
  - StatusCode: "Ok" (12), "Review" (2), "Spoiled" (1)
  - ComputerCode: "A", "B"
  - BallotNumAtComputer: 1-10 per computer
  - BallotCode: computed (e.g., "A1", "A2", "B1")
  - Teller1/Teller2: names from teller pool

- 5 online ballots:
  - LocationGuid: first location (or special online location)
  - StatusCode: "Ok"
  - ComputerCode: "OL"
  - BallotNumAtComputer: 1-5

**Votes for Election 1** (varies per ballot):

- Each ballot: 1-9 votes
- PositionOnBallot: 1, 2, 3, ..., 9
- PersonGuid: references Person records
- StatusCode: "Ok" (valid vote)
- Distribution: most candidates receive 3-7 votes, a few receive 8-12 votes

**Election 2: National Convention 2024** (Completed):

- ElectionGuid: Deterministic GUID
- Name: "National Convention 2024"
- ElectionType: "Conv" (Convention)
- ElectionMode: "D" (Delegates)
- NumberToElect: 9
- DateOfElection: 30 days ago
- TallyStatus: "Finalized"
- ShowFullReport: true
- VotingMethods: "IP" (in-person only)

**People for Election 2** (15 delegates):

- All eligible voters (CanVote: true)
- All eligible to receive votes
- All with Email and Phone
- Different names from Election 1

**Ballots for Election 2** (15 ballots, all processed):

- StatusCode: "Ok" for all
- ComputerCode: "A"
- BallotNumAtComputer: 1-15

**Votes for Election 2** (crafted for known results):

- Each ballot: exactly 9 votes
- Vote distribution creates clear winners:
  - Top 9 candidates: 12-15 votes each (elected)
  - Next 3 candidates: 5-8 votes each (not elected)
  - Remaining: 0-4 votes
- Intentionally include one tie (10th-11th place with 5 votes each)

**Results for Election 2**:

- Result records for top 15 candidates
- Vote counts matching ballot data
- Rank: 1-15
- Section: "F" (final), "T" (top 9 elected)
- ResultSummary record with stats

**ResultTie for Election 2**:

- Record tie between 10th and 11th place candidates
- TieBreakRequired: true

**Supporting Data**:

- **Tellers** (2-3 per election):
  - Name, Email (matching user emails)
  - AccessCode: random 6-digit codes
- **JoinElectionUser** (link users to elections):

  - Admin user linked to both elections
  - Teller user linked to Election 1

- **Messages** (2-3 per election):

  - Welcome message
  - Voting instructions
  - Results announcement (for completed election)

- **Logs** (10 entries):

  - "Election created", "Person added", "Ballot submitted", "Results finalized"
  - AsOf: timestamps over past 30 days

- **OnlineVotingInfo** (5 records for Election 1 online voters):
  - VoterCode: 6-digit random codes
  - Email or Phone: matching Person records
  - VerificationCodeAsOf: recent timestamps
  - Status: "Used" for those with ballots, "Sent" for pending

#### 2.3.3 Seeding Utilities

**Helper Methods**:

```csharp
// Deterministic GUID generation (for idempotency)
private static Guid CreateGuid(string seed)
{
    using var md5 = MD5.Create();
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
    return new Guid(hash);
}

// Random name generator
private static string[] FirstNames = { "John", "Mary", "Robert", "Patricia", "Michael", ... };
private static string[] LastNames = { "Smith", "Johnson", "Williams", "Jones", "Brown", ... };

private static Person CreatePerson(Election election, int index, Random rng)
{
    // Generate person with varied properties
}
```

**Idempotency**:

- Use deterministic GUIDs for predictable data
- Check if data exists before seeding
- Allow multiple runs without duplicates

---

## 3. Source Code Structure Changes

### 3.1 New Files

```
backend/
├── EF/
│   ├── Data/
│   │   └── DbSeeder.cs                    # NEW: Database seeding logic
│   ├── Migrations/
│   │   ├── YYYYMMDDHHMMSS_InitialCreate.cs          # NEW: Initial migration
│   │   ├── YYYYMMDDHHMMSS_InitialCreate.Designer.cs # NEW: Migration metadata
│   │   └── MainDbContextModelSnapshot.cs             # NEW: Current model snapshot
├── scripts/
│   ├── reset-database.ps1                 # NEW: PowerShell reset script
│   └── reset-database.sh                  # NEW: Bash reset script
└── SETUP.md                               # NEW: Developer setup guide
```

### 3.2 Modified Files

```
backend/
├── appsettings.json                       # MODIFIED: Add connection string template
├── appsettings.Development.json           # MODIFIED: Add SQL Express connection string
└── Program.cs                             # MODIFIED: Add seeding call in Development mode
```

### 3.3 Configuration Structure

**appsettings.json** (production template):

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=<SERVER>;Database=TallyJ4;User Id=<USER>;Password=<PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "TallyJ4API",
    "Audience": "TallyJ4Client",
    "Key": "<256-bit-key-minimum-32-characters>"
  },
  "Database": {
    "SeedOnStartup": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json**:

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=localhost\\SQLEXPRESS;Database=TallyJ4Dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "TallyJ4API",
    "Audience": "TallyJ4Client",
    "Key": "dev-secret-key-minimum-32-characters-for-jwt-signing"
  },
  "Database": {
    "SeedOnStartup": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**User Secrets** (for local development, optional):

```bash
dotnet user-secrets init --project backend/TallyJ4.csproj
dotnet user-secrets set "ConnectionStrings:TallyJ4" "Server=localhost\\SQLEXPRESS;Database=TallyJ4Dev;Trusted_Connection=True;TrustServerCertificate=True" --project backend/TallyJ4.csproj
```

---

## 4. Data Model / API / Interface Changes

### 4.1 Entity Model Changes

**No Changes Required** - All entity models are already defined and complete in `backend/EF/Models/`.

**Computed Columns Verification**:

- Ensure `Person.FullName` formula matches specification
- Ensure `Ballot.BallotCode` formula is correct
- Verify `ImportFile.FileSize` and `HasContent` computed columns

### 4.2 DbContext Changes

**No Changes Required** - `MainDbContext.cs` already has complete `OnModelCreating` configuration.

**Optional Enhancement** (for base entity tracking):

- Consider adding `ISoftDeletable` interface for future soft-delete support
- Consider adding `IAuditable` interface for CreatedBy/ModifiedBy tracking
- **Decision**: Defer to later phase, not needed for initial database setup

### 4.3 API Endpoints

**Existing Endpoints** (already configured):

- `POST /auth/register` - User registration
- `POST /auth/login` - User login (returns JWT token)
- `POST /auth/refresh` - Refresh token
- `GET /protected` - Test endpoint requiring authorization

**No New Endpoints in This Phase** - API development deferred to later phases

**Testing API**:

```bash
# Register user
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123"}'

# Login (should work with seeded users)
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tallyj.test","password":"Admin@123"}'

# Access protected endpoint with token
curl -X GET http://localhost:5000/protected \
  -H "Authorization: Bearer <token>"
```

### 4.4 Configuration Interface

**New Configuration Section**: `Database` settings

**Interface** (optional, for strong typing):

```csharp
public class DatabaseSettings
{
    public bool SeedOnStartup { get; set; }
    public bool ClearBeforeSeed { get; set; }
}
```

**Registration** (in `Program.cs`):

```csharp
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));
```

---

## 5. Delivery Phases (Incremental Milestones)

### Phase 1.1: Configuration & Migration (Day 1)

**Deliverables**:

- ✅ Connection string configured in appsettings.Development.json
- ✅ Initial EF Core migration created
- ✅ Migration tested (database created successfully)
- ✅ All tables, indexes, computed columns verified in SQL Server

**Verification**:

- Run `dotnet ef database update`
- Inspect database schema in SSMS or Azure Data Studio
- Verify 16 entity tables + Identity tables exist
- Check computed columns, indexes, foreign keys

**Exit Criteria**:

- Database schema matches entity model exactly
- No migration warnings or errors
- Developer can create database from scratch

---

### Phase 1.2: User Seeding (Day 1-2)

**Deliverables**:

- ✅ DbSeeder class created
- ✅ User seeding implemented (3 test users)
- ✅ Seeding integrated into Program.cs
- ✅ Users can authenticate via /auth/login

**Verification**:

- Run application
- Check AspNetUsers table has 3 records
- Test login with `admin@tallyj.test` / `Admin@123`
- Verify JWT token returned

**Exit Criteria**:

- All 3 users created successfully
- Passwords work for authentication
- EmailConfirmed = true for all users

---

### Phase 1.3: Election 1 Seeding (Day 2)

**Deliverables**:

- ✅ Election 1 (Springfield LSA) created
- ✅ 2 Locations seeded
- ✅ 30 People seeded with varied properties
- ✅ 20 Ballots seeded (15 in-person, 5 online)
- ✅ Votes seeded for all ballots
- ✅ Tellers, JoinElectionUser, Messages seeded

**Verification**:

- Query database: `SELECT * FROM Election WHERE Name LIKE '%Springfield%'`
- Verify Person.FullName computed column populated
- Verify Ballot.BallotCode computed column (e.g., "A1", "B5")
- Check Vote counts per ballot (1-9 votes each)

**Exit Criteria**:

- Complete election with voters and ballots
- Relationships intact (no orphaned records)
- Computed columns calculated correctly

---

### Phase 1.4: Election 2 Seeding (Day 2-3)

**Deliverables**:

- ✅ Election 2 (National Convention) created
- ✅ 15 People (delegates) seeded
- ✅ 15 Ballots seeded (all processed)
- ✅ Votes seeded with known distribution
- ✅ Results calculated and seeded
- ✅ ResultSummary and ResultTie seeded

**Verification**:

- Query: `SELECT * FROM Result WHERE ElectionGuid = <election2guid> ORDER BY Rank`
- Verify top 9 candidates have highest vote counts
- Verify tie record exists for 10th/11th place
- Check ResultSummary stats

**Exit Criteria**:

- Completed election with finalized results
- Vote tallies match Result records
- Tie-breaking scenario demonstrated

---

### Phase 1.5: Supporting Data & Documentation (Day 3)

**Deliverables**:

- ✅ OnlineVotingInfo seeded (verification codes)
- ✅ Logs seeded (audit trail examples)
- ✅ SmsLog seeded (if applicable)
- ✅ Database reset scripts created (PowerShell, Bash)
- ✅ SETUP.md documentation written
- ✅ README updated with database setup instructions

**Verification**:

- Run reset script: `./backend/scripts/reset-database.ps1`
- Verify database drops and recreates
- Verify seeding runs automatically on app startup
- Read SETUP.md - ensure steps are clear

**Exit Criteria**:

- All seed data complete and verified
- Reset scripts work on Windows and Linux
- Documentation allows new developer to set up database in < 30 minutes

---

## 6. Verification Approach

### 6.1 Manual Testing

**Database Schema Verification**:

```sql
-- Check all tables exist
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Verify computed columns
SELECT COLUMN_NAME, DATA_TYPE, COLUMN_DEFAULT, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Person' AND COLUMN_NAME LIKE '%Full%';

-- Check foreign keys
SELECT
    fk.name AS ForeignKey,
    tp.name AS ParentTable,
    cp.name AS ParentColumn,
    tr.name AS ReferencedTable,
    cr.name AS ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
ORDER BY tp.name, fk.name;

-- Verify unique indexes
SELECT
    i.name AS IndexName,
    t.name AS TableName,
    i.is_unique,
    i.filter_definition
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.is_unique = 1 AND t.name = 'Person';
```

**Seed Data Verification**:

```sql
-- Check record counts
SELECT 'Users' AS Entity, COUNT(*) AS Count FROM AspNetUsers
UNION ALL SELECT 'Elections', COUNT(*) FROM Election
UNION ALL SELECT 'People', COUNT(*) FROM Person
UNION ALL SELECT 'Ballots', COUNT(*) FROM Ballot
UNION ALL SELECT 'Votes', COUNT(*) FROM Vote
UNION ALL SELECT 'Results', COUNT(*) FROM Result
UNION ALL SELECT 'Locations', COUNT(*) FROM Location
UNION ALL SELECT 'Tellers', COUNT(*) FROM Teller
UNION ALL SELECT 'Messages', COUNT(*) FROM Message
UNION ALL SELECT 'Logs', COUNT(*) FROM Log;

-- Verify computed columns work
SELECT
    FirstName,
    LastName,
    FullName,           -- Computed: LastName, FirstName format
    FullNameFl          -- Computed: FirstName LastName format
FROM Person
WHERE ElectionGuid = (SELECT TOP 1 ElectionGuid FROM Election WHERE Name LIKE '%Springfield%');

-- Verify ballot codes
SELECT ComputerCode, BallotNumAtComputer, BallotCode
FROM Ballot
WHERE LocationGuid IN (SELECT LocationGuid FROM Location WHERE ElectionGuid = (SELECT TOP 1 ElectionGuid FROM Election WHERE Name LIKE '%Springfield%'));

-- Check vote distribution for Election 2
SELECT
    p.FullName,
    COUNT(v.RowId) AS VoteCount
FROM Vote v
INNER JOIN Person p ON v.PersonGuid = p.PersonGuid
INNER JOIN Ballot b ON v.BallotGuid = b.BallotGuid
INNER JOIN Location l ON b.LocationGuid = l.LocationGuid
WHERE l.ElectionGuid = (SELECT ElectionGuid FROM Election WHERE Name LIKE '%Convention%')
GROUP BY p.PersonGuid, p.FullName
ORDER BY VoteCount DESC;
```

### 6.2 API Testing

**Test Seeded User Login**:

```bash
# Test admin login
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tallyj.test","password":"Admin@123"}' \
  | jq

# Expected response:
# {
#   "tokenType": "Bearer",
#   "accessToken": "<jwt-token>",
#   "expiresIn": 3600,
#   "refreshToken": "<refresh-token>"
# }
```

**Test Protected Endpoint**:

```bash
TOKEN="<jwt-token-from-login>"

curl -X GET http://localhost:5000/protected \
  -H "Authorization: Bearer $TOKEN"

# Expected: "This is protected!"
```

### 6.3 Automated Testing (Optional for Future)

**Unit Tests** (future):

- Test DbSeeder idempotency
- Test GUID generation determinism
- Test person/ballot/vote creation logic

**Integration Tests** (future):

- Test full seeding workflow
- Test database reset and reseed
- Test migration application

### 6.4 Linting & Build Verification

**Commands to Run**:

```bash
# Restore packages
dotnet restore backend/TallyJ4.csproj

# Build solution
dotnet build backend/TallyJ4.csproj --configuration Debug

# Run application (triggers seeding in Development)
dotnet run --project backend/TallyJ4.csproj

# Check EF migrations status
dotnet ef migrations list --project backend/TallyJ4.csproj

# Generate migration script (review output)
dotnet ef migrations script --project backend/TallyJ4.csproj
```

**Success Criteria**:

- ✅ No build errors
- ✅ No build warnings (or only acceptable warnings)
- ✅ Application starts successfully
- ✅ Database seeding completes without errors
- ✅ Logs show "Database seeding complete"

### 6.5 Cross-Platform Testing

**Test on Windows**:

- SQL Server Express or LocalDB
- PowerShell reset script

**Test on Linux/macOS** (optional):

- SQL Server in Docker or Azure SQL
- Bash reset script

---

## 7. Risk Mitigation

### 7.1 Identified Risks

**Risk 1: Computed Column Migration Issues**

- **Likelihood**: Medium
- **Impact**: High
- **Mitigation**:
  - Manually inspect migration SQL before applying
  - Test computed columns after migration
  - Have SQL scripts ready to add missing computed columns

**Risk 2: Seed Data Referential Integrity Violations**

- **Likelihood**: Medium
- **Impact**: Medium
- **Mitigation**:
  - Create entities in correct order (Election → Location → Person → Ballot → Vote)
  - Use deterministic GUIDs for predictable foreign keys
  - Add extensive error logging in seeder

**Risk 3: SQL Express Not Installed**

- **Likelihood**: High (for new developers)
- **Impact**: Medium
- **Mitigation**:
  - Document SQL Express installation in SETUP.md
  - Provide alternative: Docker SQL Server container
  - Provide alternative: Azure SQL free tier

**Risk 4: Connection String Configuration Errors**

- **Likelihood**: High
- **Impact**: Medium
- **Mitigation**:
  - Provide multiple connection string examples
  - Add detailed error messages for connection failures
  - Document user secrets approach

**Risk 5: Migration Applied to Wrong Database**

- **Likelihood**: Low
- **Impact**: High
- **Mitigation**:
  - Use different database names per environment (TallyJ4Dev, TallyJ4Test, TallyJ4)
  - Prompt for confirmation in reset scripts
  - Document connection string best practices

---

## 8. Dependencies

### 8.1 External Dependencies

**Required Software**:

- .NET 9.0 SDK (installed)
- SQL Server Express 2019+ or SQL Server Developer Edition
- Optional: SQL Server Management Studio (SSMS) or Azure Data Studio
- Optional: Docker Desktop (for SQL Server in container)

**NuGet Packages** (already installed):

- No additional packages needed for this phase

### 8.2 Documentation Dependencies

**Reference Documentation**:

- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/entities.md` - entity specifications
- `.zenflow/tasks/plan-new-build-b9db/requirements.md` - this phase requirements
- Official EF Core documentation: https://learn.microsoft.com/en-us/ef/core/

### 8.3 Team Dependencies

**Roles**:

- **Developer**: Implement migration and seeding
- **Reviewer**: Verify migration SQL and seed data logic
- **QA**: Test database creation and seeding on clean environment

**No blockers** - single developer can complete this phase independently

---

## 9. Success Criteria Summary

This specification is successfully implemented when:

1. ✅ **Configuration Complete**:

   - Connection strings configured in appsettings files
   - JWT configuration present
   - Logging configured

2. ✅ **Migration Created**:

   - Initial migration captures all 16 entities + Identity
   - Computed columns, indexes, foreign keys included
   - Migration applies without errors

3. ✅ **Database Created**:

   - `dotnet ef database update` succeeds
   - All tables, constraints, indexes verified in SQL Server
   - Computed columns function correctly

4. ✅ **Seeding Implemented**:

   - DbSeeder class complete with 3 users, 2 elections, 30+ people, 20+ ballots, 100+ votes
   - Seeding integrated into application startup (Development mode)
   - Idempotent (can run multiple times)

5. ✅ **Data Validated**:

   - Computed columns (FullName, BallotCode) populated correctly
   - Foreign key relationships intact
   - Vote distributions realistic
   - Results data accurate for completed election

6. ✅ **Developer Experience**:

   - Database reset scripts work (PowerShell and Bash)
   - SETUP.md documentation clear and complete
   - New developer can set up database in < 30 minutes

7. ✅ **API Tested**:

   - Seeded users can authenticate via `/auth/login`
   - JWT tokens returned correctly
   - Protected endpoints accessible with token

8. ✅ **Build Verified**:
   - `dotnet build` succeeds with no errors
   - `dotnet run` starts application successfully
   - Seeding completes without exceptions

---

## 10. Future Phases (Out of Scope)

The following will be addressed in subsequent phases:

**Phase 2: Core API Development**

- Controllers for Elections, People, Ballots, Votes
- DTOs and AutoMapper configuration
- FluentValidation for requests
- Repository pattern (optional)

**Phase 3: Business Logic**

- Tally calculation engine
- Ballot analysis logic
- Eligibility checking
- Vote validation

**Phase 4: SignalR Hubs**

- Real-time updates for ballot entry
- Roll call display
- Multi-teller coordination

**Phase 5: Authentication & Authorization**

- Role-based authorization
- Election-specific permissions
- Teller access codes
- Online voter verification

**Phase 6: Frontend Development**

- Vue 3 application
- Component library
- State management (Pinia)
- API integration

**Phase 7: External Integrations**

- OAuth (Google, Facebook)
- SMS (Twilio)
- Email service
- File imports (CSV)

**Phase 8: Deployment**

- Docker containerization
- Azure deployment
- CI/CD pipeline
- Production configuration

---

## 11. Open Questions

**Q1**: Should we use SQL Server LocalDB instead of SQL Express for development?

- **Decision**: Support both, document both options, recommend SQL Express (more production-like)

**Q2**: Should seeding be configurable via appsettings or always automatic in Development?

- **Decision**: Configurable via `Database:SeedOnStartup` setting, default to true in Development

**Q3**: Should we implement soft-delete pattern now or later?

- **Decision**: Later phase, not needed for initial setup

**Q4**: Should we add CreatedDate/ModifiedDate audit fields to all entities?

- **Decision**: Later phase, not in original system design

**Q5**: Should reset scripts prompt for confirmation?

- **Decision**: Yes, add confirmation prompt to prevent accidental data loss

---

## 12. Glossary

- **EF Core**: Entity Framework Core - Microsoft's ORM for .NET
- **Code-First**: EF development approach where entities are defined in code, database schema generated from code
- **Migration**: Versioned database schema change script generated by EF Core
- **Seeding**: Populating database with initial/test data
- **DbContext**: EF Core class representing a database session
- **GUID**: Globally Unique Identifier (128-bit value)
- **Row Versioning**: SQL Server concurrency control mechanism using timestamp/rowversion columns
- **Computed Column**: Database column whose value is calculated from other columns
- **Filtered Index**: Index with a WHERE clause, indexing only rows that match the filter
- **JWT**: JSON Web Token - standard for securely transmitting information as a JSON object

---

## 13. Revision History

| Version | Date       | Author | Changes                       |
| ------- | ---------- | ------ | ----------------------------- |
| 1.0     | 2026-01-04 | AI     | Initial specification created |
