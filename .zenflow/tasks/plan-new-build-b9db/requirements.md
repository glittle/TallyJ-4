# Product Requirements Document: TallyJ 4 - Initial Build Phase

## 1. Overview

Build the initial implementation of TallyJ 4, an election management and ballot tallying system for Bahá'í communities, using .NET 9.0, Entity Framework Core, and SQL Express.

**Previous Work**: Comprehensive reverse engineering documentation (~55,000 lines) exists in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/` documenting the original ASP.NET Framework 4.8 system.

**Current State**: Basic project structure exists with:

- .NET 9.0 backend project
- Entity Framework Core models (16 entities)
- MainDbContext configured with ASP.NET Core Identity
- NuGet packages installed (EF Core, SQL Server, Identity, SignalR, etc.)

**Objective**: Create a functional database layer with migrations and seed data to enable development and testing.

---

## 2. Database Requirements

### 2.1 Database Technology

**Production**: SQL Server (any edition including Express)  
**Local Development**: SQL Express (SQL Server 2019 Express or later)

**Rationale**:

- SQL Express is free and sufficient for local development
- Compatible with production SQL Server deployments
- Supports all required features (computed columns, row versioning, indexes)

### 2.2 Connection Configuration

**Connection String Requirements**:

- Named connection: `TallyJ4` (matching existing Program.cs)
- Support for integrated security (Windows Authentication) for local development
- Support for SQL authentication for production deployments
- Password masking in logs (already implemented in Program.cs)

**Configuration Locations**:

- `appsettings.Development.json` - Local SQL Express connection
- `appsettings.json` - Production-ready template (without actual credentials)
- User Secrets - Sensitive credentials for local development

### 2.3 Database Schema

The database must support all 16 entity types defined in the existing models:

**Core Election Entities**:

1. **Election** - Election configuration and metadata
2. **Location** - Voting locations within an election
3. **Teller** - Election workers/administrators
4. **JoinElectionUser** - Links users to elections they can manage

**People & Voting Entities**: 5. **Person** - Voters and candidates (with computed full name fields) 6. **OnlineVoter** - Online voting registration records 7. **OnlineVotingInfo** - Verification codes and voting tokens 8. **Ballot** - Physical/digital ballots 9. **Vote** - Individual votes on ballots

**Results Entities**: 10. **Result** - Tally results per candidate 11. **ResultSummary** - Election result summaries 12. **ResultTie** - Tie-breaking records

**Supporting Entities**: 13. **ImportFile** - CSV import files with binary content 14. **Message** - System messages 15. **Log** - Audit/change log 16. **SmsLog** - SMS delivery logs (implied from documentation)

**Identity Tables** (managed by ASP.NET Core Identity):

- AspNetUsers (via AppUser entity)
- AspNetRoles
- AspNetUserClaims
- AspNetUserLogins (for OAuth)
- AspNetUserTokens
- AspNetUserRoles
- AspNetRoleClaims

### 2.4 Database Features

**Required Features** (already configured in MainDbContext):

- **Computed Columns**:

  - `Ballot.BallotCode` (ComputerCode + BallotNumAtComputer)
  - `Person.FullName` and `Person.FullNameFl` (name concatenation)
  - `ImportFile.FileSize` (DATALENGTH of Contents)
  - `ImportFile.HasContent` (boolean check)
  - `Person.RowVersionInt` (bigint conversion of RowVersion)

- **Row Versioning**: Concurrency tokens on Ballot, Election, Message, Person, Teller, Vote

- **Default Values**:

  - `Election.ElectionGuid` (GUID generation with timestamp)
  - `Election.OnlineCloseIsEstimate` (default true)
  - `OnlineVoter.VoterIdType` (default 'E')
  - `Log.AsOf` (default GETDATE())

- **Unique Indexes**:

  - Person.Email per election (filtered, excluding NULL/empty)
  - Person.Phone per election (filtered, excluding NULL/empty)

- **Foreign Key Relationships**: Fully configured in MainDbContext

---

## 3. Entity Framework Core Setup

### 3.1 Migration Strategy

**Approach**: Code-First migrations  
**Tool**: `dotnet ef migrations` CLI

**Initial Migration**:

- Create initial migration capturing all 16 entities
- Migration name: `InitialCreate`
- Verify all computed columns, defaults, and constraints are captured

**Database Creation**:

- Use `dotnet ef database update` to create/update local database
- Support for database recreation (drop/create) during development
- Migration scripts for production deployment

### 3.2 Migration Requirements

**Must Include**:

- All entity tables with proper column types
- All foreign key constraints
- All unique indexes (including filtered indexes)
- All computed column definitions (persisted and non-persisted)
- All default value constraints
- Row version columns with proper configuration
- ASP.NET Core Identity tables

**Must Exclude**:

- Sample/seed data (handled separately)
- Development-specific configurations

### 3.3 Database Rebuild Capability

**Requirement**: Ability to quickly rebuild database from scratch

**Implementation Options**:

1. Script: `rebuild-database.ps1` or `rebuild-database.sh`
2. Helper class: `DatabaseManager` with methods for drop/create/seed
3. CLI command integration in `Program.cs`

**Use Cases**:

- Developer onboarding (fresh database setup)
- Testing scenarios (clean slate)
- Schema changes during development

---

## 4. Database Seeding

### 4.1 Seed Data Requirements

Create realistic test data demonstrating all major system features.

**Minimum Seed Data**:

#### 4.1.1 Identity/Users (3-5 users)

- **Admin User**: Full system administrator

  - Email: `admin@tallyj.test`
  - Password: `Admin@123` (development only)
  - Can create elections, manage system

- **Teller User**: Election worker/coordinator

  - Email: `teller@tallyj.test`
  - Password: `Teller@123`
  - Can manage assigned elections

- **Voter User**: Regular voter (if voter authentication needed)
  - Email: `voter@tallyj.test`
  - Password: `Voter@123`

#### 4.1.2 Elections (2-3 test elections)

**Election 1: Active Local Assembly Election**

- **Name**: "Springfield Local Spiritual Assembly Election 2024"
- **Type**: Local Assembly
- **Mode**: In-Person with Online option
- **Status**: Active/Voting in progress
- **Number of Positions**: 9
- **Locations**: 2 voting locations (Main Hall, Community Center)
- **Online Voting**: Enabled
- **Dates**: Election date = today or recent past

**Election 2: Completed National Convention**

- **Name**: "National Convention 2024"
- **Type**: Convention
- **Mode**: Delegates only
- **Status**: Finalized
- **Number of Positions**: 9
- **Locations**: 1 location
- **Online Voting**: Disabled
- **Results**: Fully calculated with winners

**Election 3 (Optional): Future Election**

- **Name**: "Unit Convention 2025"
- **Status**: Setup/Planning
- **Not yet accepting ballots**

#### 4.1.3 People/Voters (20-50 per election)

For **Election 1** (Springfield LSA):

- **Eligible Voters**: 30 people

  - Mix of first/last names
  - Mix of with/without phone and email
  - Some with BahaiId (unique identifier)
  - Various eligibility statuses
  - Age 21+ (eligible voting age)

- **Potential Candidates**: Same pool (anyone can be voted for)

**Demographics Variety**:

- Mix of gender (if tracked)
- Mix of contact methods (email only, phone only, both, neither)
- Mix of names (first name variations, middle names, suffixes)

For **Election 2** (National Convention):

- **Delegates**: 15 people (smaller, delegate-only pool)
- All with full contact information
- All eligible to vote and be voted for

#### 4.1.4 Ballots (10-20 per election)

**Election 1 Ballots**:

- **In-Person Ballots**: 15 ballots

  - Various tellers/computers
  - Different locations
  - Mix of ballot statuses (Ok, Review, Spoiled)
  - Ballot codes (e.g., "A1", "A2", "B1", "B2")

- **Online Ballots**: 5 ballots
  - Linked to OnlineVoter records
  - Status: Ok
  - Recent timestamps

**Election 2 Ballots** (completed):

- 20 ballots, all processed
- All status: Ok
- From single location

#### 4.1.5 Votes (9-15 votes per ballot)

**Election 1 Votes**:

- Each ballot has 1-9 votes (people can vote for up to 9 candidates)
- Mix of:
  - Valid votes (person exists in system)
  - Write-in votes (PersonGuid links to Person record)
  - Single-name votes vs. full names
  - Votes at various ranks/positions on ballot

**Election 2 Votes** (for tally testing):

- Carefully crafted to produce known tally results
- Includes scenarios:
  - Clear winners (high vote counts)
  - Close races (ties or near-ties)
  - Vote distribution across many candidates

#### 4.1.6 Results (for completed elections)

**Election 2 Results**:

- Calculated results for all candidates who received votes
- Vote counts, ranks, sections
- Tie-breaking records (if applicable)
- Result summaries

#### 4.1.7 Supporting Data

**Locations**:

- 2-3 locations per active election
- Names, descriptions
- Contact information (optional)

**Tellers**:

- 2-3 tellers per active election
- Linked to users via email
- Access codes for teller login

**Import Files** (optional):

- 1-2 CSV import records showing voter list imports
- Small sample content or empty

**Messages**:

- 2-3 system messages
- Examples: welcome message, voting instructions, results notification

**Logs**:

- 5-10 audit log entries
- Examples: election created, person added, ballot submitted, results calculated

**OnlineVotingInfo**:

- Verification codes for online voters
- Mix of email and phone verification
- Some used, some expired

### 4.2 Seed Implementation

**Approach**: Database seeder class

**Location**: `backend/EF/Data/DbSeeder.cs` or `backend/Data/SeedData.cs`

**Method Signature**:

```csharp
public static class DbSeeder
{
    public static async Task SeedAsync(MainDbContext context, UserManager<AppUser> userManager)
    {
        // Seed logic
    }
}
```

**Integration Point**: Call from `Program.cs` during development startup

**Idempotency**:

- Check if data already exists before seeding
- Option to clear and reseed
- Use deterministic GUIDs or well-known IDs for lookups

**Data Relationships**:

- Ensure all foreign keys are valid
- Respect required relationships (Election → People, Ballot → Votes)
- Use navigation properties where possible

### 4.3 Seed Configuration

**Configuration Options** (in appsettings.Development.json):

```json
{
  "Database": {
    "SeedOnStartup": true,
    "ClearBeforeSeed": false,
    "SeedUsers": true,
    "SeedElections": true
  }
}
```

**Command-Line Options** (optional):

- `--seed` - Force seeding
- `--clear-and-seed` - Drop, recreate, and seed
- `--seed-minimal` - Minimal data only

---

## 5. Development Workflow

### 5.1 Initial Setup (New Developer)

**Steps**:

1. Clone repository
2. Install SQL Express (if not present)
3. Configure connection string in user secrets or appsettings.Development.json
4. Run `dotnet ef database update` to create database
5. Run application - automatic seeding on first startup (if configured)
6. Access system with test credentials

### 5.2 Schema Changes

**Workflow**:

1. Modify entity classes
2. Run `dotnet ef migrations add <MigrationName>`
3. Review generated migration code
4. Run `dotnet ef database update`
5. Test with seeded data

### 5.3 Database Reset

**When Needed**:

- Major schema changes
- Corrupted test data
- Fresh testing scenario

**Steps**:

1. Run `dotnet ef database drop` (or custom script)
2. Run `dotnet ef database update`
3. Restart application to trigger seeding

---

## 6. Connection String Examples

### 6.1 SQL Express (Local Development)

**Integrated Security** (recommended for local):

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=localhost\\SQLEXPRESS;Database=TallyJ4;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**SQL Authentication**:

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=localhost\\SQLEXPRESS;Database=TallyJ4;User Id=tallyj;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 6.2 SQL Server (Production)

**Azure SQL**:

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=tcp:<server>.database.windows.net,1433;Database=TallyJ4;User Id=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

**On-Premises SQL Server**:

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=<server>;Database=TallyJ4;User Id=<user>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

---

## 7. Testing & Validation

### 7.1 Database Creation Validation

**Verify**:

- All 16 entity tables created
- All ASP.NET Identity tables created
- All computed columns working correctly
- All foreign keys enforced
- All unique indexes created (including filtered indexes)
- Row version columns functioning
- Default values applied

**Test Method**:

- SQL Server Management Studio (SSMS) inspection
- Run test queries
- Check table schemas

### 7.2 Seed Data Validation

**Verify**:

- All entities have expected record counts
- Relationships are correctly linked (no orphaned records)
- Computed columns calculated correctly (e.g., Person.FullName)
- User authentication works with seeded credentials
- Elections have proper status values
- Ballots link to valid locations and have votes
- Results calculations are accurate (for completed election)

**Test Method**:

- Query database directly
- Use application endpoints (once API built)
- Unit tests on seeder logic

### 7.3 Migration Testing

**Scenarios**:

- Initial database creation (fresh install)
- Database update (existing database)
- Migration rollback (if needed)
- Database drop and recreate

**Success Criteria**:

- No errors during migration application
- All data constraints enforced
- Seed data loads successfully after migration

---

## 8. Documentation Requirements

### 8.1 Developer Setup Guide

**Content**:

- Prerequisites (SDK, SQL Express)
- Step-by-step database setup
- Connection string configuration
- Migration commands
- Seeding commands
- Troubleshooting common issues

**Format**: README.md or SETUP.md in repository root

### 8.2 Database Schema Documentation

**Content**:

- Entity relationship diagram (ERD)
- Table descriptions
- Column descriptions for complex fields
- Computed column formulas
- Index purposes
- Foreign key relationships

**Format**: Markdown files or inline code comments

### 8.3 Seed Data Documentation

**Content**:

- Description of each seeded election scenario
- Test user credentials
- How to customize seed data
- How to add/modify seed scenarios

**Format**: Comments in seeder code + README section

---

## 9. Out of Scope (This Phase)

This phase focuses on database setup. The following are **not included**:

- API endpoint implementation (beyond existing /auth and /protected)
- Business logic services
- Frontend application (Vue 3)
- SignalR hub implementation
- External integrations (OAuth, SMS, email)
- File upload/import processing
- Advanced authorization policies
- Report generation
- Production deployment scripts

These will be addressed in subsequent implementation phases.

---

## 10. Success Criteria

This phase is successful when:

1. ✅ SQL Express connection configured and tested
2. ✅ Connection string configuration documented and working
3. ✅ Initial EF Core migration created capturing all 16 entities
4. ✅ Database can be created from migrations (`dotnet ef database update`)
5. ✅ All computed columns, defaults, and constraints working correctly
6. ✅ Database seeder implemented with realistic test data
7. ✅ Seed data includes: 3+ users, 2+ elections, 30+ people, 20+ ballots, 100+ votes
8. ✅ Seed data demonstrates all major entity relationships
9. ✅ Database can be dropped and recreated easily
10. ✅ Developer setup documentation complete
11. ✅ Test credentials work for authentication (/auth/login endpoint)
12. ✅ Data validation confirmed (foreign keys, computed columns, etc.)

---

## 11. Assumptions & Constraints

**Assumptions**:

- .NET 9.0 SDK installed on development machines
- SQL Express can be installed (or SQL Server Developer Edition available)
- Windows or cross-platform development environment
- EF Core migrations are sufficient (no manual SQL scripts needed initially)
- Seed data passwords can be simple for development (not production-ready)

**Constraints**:

- Must support SQL Server (Express/Standard/Azure SQL)
- Must use Entity Framework Core (Code-First)
- Must integrate with existing MainDbContext and entity models
- Must work with ASP.NET Core Identity for user management
- Must maintain data integrity (foreign keys, constraints)

**Design Decisions**:

- Use Code-First EF migrations (not Database-First)
- Seed data in code (not SQL scripts)
- Development passwords stored in configuration (not production approach)
- Use GUID primary keys where already defined in entities
- Maintain compatibility with original TallyJ data model

---

## 12. References

**Documentation**:

- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/entities.md` - Full entity documentation
- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/security/authentication.md` - Authentication systems
- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/spec.md` - Overall technical specification

**Code**:

- `backend/EF/Context/MainDbContext.cs` - Current DbContext configuration
- `backend/EF/Models/*.cs` - All entity models
- `backend/Program.cs` - Application startup and configuration

**External**:

- Entity Framework Core documentation: https://learn.microsoft.com/en-us/ef/core/
- ASP.NET Core Identity documentation: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
- SQL Server Express: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
