# Product Requirements Document: TallyJ 4 Migration and Build

## 1. System Overview

**TallyJ** is an election management and ballot tallying system designed for Bahá'í communities of up to 50,000 members. It facilitates the complete election process including voter registration, ballot collection, online voting, and tally reporting.

**Current Version**: 3.5.28 (April 4, 2024)
**Current Location**: `C:\Dev\TallyJ\v3\Site`
**Deployment**: Available at https://tallyj.com and can be self-hosted with IIS + SQL Server
**Documentation**: https://docs.google.com/document/d/1mlxI_5HWyt-zdr0EyGPzrScInqUXhA5WbT7d0Mb7gJQ/view

---

## 2. Objectives

### 2.1 Reverse Engineering Objectives
1. **Reverse Engineer**: Fully document the current ASP.NET Framework 4.8 application
2. **Create Migration Specs**: Produce detailed specifications that an AI or development team can use to rebuild the system
3. **Ensure Completeness**: Capture all functionality, business logic, data structures, integrations, and UI/UX patterns
4. **Modernize Architecture**: Rebuild using .NET Core backend with Vue 3 Composition API frontend

### 2.2 Build Phase Objectives
Build the initial implementation of TallyJ 4, an election management and ballot tallying system for Bahá'í communities, using .NET 9.0, Entity Framework Core, and SQL Express.

**Previous Work**: Comprehensive reverse engineering documentation (~55,000 lines) exists in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/` documenting the original ASP.NET Framework 4.8 system.

**Current State**: Basic project structure exists with:
- .NET 9.0 backend project
- Entity Framework Core models (16 entities)
- MainDbContext configured with ASP.NET Core Identity
- NuGet packages installed (EF Core, SQL Server, Identity, SignalR, etc.)

**Objective**: Create a functional database layer with migrations and seed data to enable development and testing.

---

## 3. Current Technology Stack

### 3.1 Backend (ASP.NET Framework 4.8)
- **Framework**: ASP.NET MVC on .NET Framework 4.8
- **ORM**: Entity Framework 6.4.4 (Code First with Migrations)
- **DI Container**: Unity 3.5
- **Real-time**: SignalR 2.4.3 (10 Hubs for different areas)
- **Authentication**: ASP.NET Identity 2.2.4 with OWIN 4.2.2
- **Session**: StateServer (tcpip=localhost:42424)
- **Security**: FluentSecurity 2.1.0, custom authorization attributes
- **Data Export**: CSV (LumenWorksCsvReader)
- **HTML Parsing**: CsQuery 1.3.4

### 3.2 Frontend (Legacy)
- **Views**: Razor (.cshtml) with associated .js and .less files per view
- **JavaScript**:
  - jQuery 3.7.1 + jQuery UI 1.13.2
  - Vue.js 2.x (no single-file components, embedded in views)
  - Element UI (Vue components)
  - SignalR client 2.4.3
  - Highcharts (for reporting)
  - Moment.js, Luxon (date handling)
  - CKEditor (rich text)
- **CSS**: LESS (compiled to CSS per view)

### 3.3 Database
- **Primary**: SQL Server (Entity Framework Code First)
- **Connection**: MainConnection3 (configured at machine level in IIS)
- **Session Storage**: StateServer

### 3.4 Key Architecture Patterns
- **MVC Pattern**: Controllers return Views or JsonResults
- **View-Specific Assets**: Each .cshtml has matching .js and .less files
- **Custom Attributes**: `[ForAuthenticatedTeller]`, `[AllowTellersInActiveElection]`
- **Hub Architecture**: 10 SignalR hubs for real-time communication
- **UserSession**: Session management class for current user/election/computer
- **Model Helpers**: "Helper" and "Model" classes in CoreModels for business logic

---

## 4. Main Application Areas

### 4.1 Controllers (12)
1. **AccountController** - Authentication, login, registration
2. **AfterController** - Post-election activities
3. **BallotsController** - Ballot entry and management
4. **BeforeController** - Pre-election setup
5. **DashboardController** - Main dashboard, election list
6. **ElectionsController** - Election CRUD operations
7. **Manage2Controller** - System management
8. **PeopleController** - Voter/person registration
9. **PublicController** - Public-facing pages, SMS status
10. **SetupController** - Election setup and configuration
11. **SysAdminController** - System administration
12. **VoteController** - Vote entry and tallying

### 4.2 SignalR Hubs (10)
1. **AllVotersHub** - All voters management
2. **AnalyzeHub** - Ballot analysis
3. **BallotImportHub** - Ballot import processing
4. **FrontDeskHub** - Front desk operations
5. **ImportHub** - General import operations
6. **MainHub** - Main real-time updates
7. **PublicHub** - Public displays
8. **RollCallHub** - Roll call functionality
9. **VoterCodeHub** - Voter code management
10. **VoterPersonalHub** - Individual voter portal

---

## 5. Core Data Models

### 5.1 Primary Entities (Entity Framework DbSets)
1. **Election** - Election configuration, type, mode, status
2. **Person** - Voters and candidates (BahaiId, name, eligibility, voting location)
3. **Ballot** - Physical/digital ballots with status
4. **Vote** - Individual votes on ballots
5. **Location** - Voting locations
6. **Teller** - Election workers/administrators
7. **User** - System users (ASP.NET Identity)
8. **Computer** - Registered computers for teller entry
9. **JoinElectionUser** - Links users to elections
10. **Result** - Tally results
11. **ResultSummary** - Result summaries
12. **ResultTie** - Tie-breaking records
13. **ImportFile** - Imported voter/person files
14. **Message** - System messages
15. **C_Log** - Change/audit logs

### 5.2 View Models (Database Views)
- vBallotInfo
- vElectionListInfo
- vImportFileInfo
- vLocationInfo
- vVoteInfo

### 5.3 Key Business Models (CoreModels)
- BallotAnalyzer, BallotHelper, BallotNormalModel, BallotSingleModel
- ComputerModel, ElectionHelper, LocationModel
- ImportCsvModel, ImportBallotsModel
- PeopleModel, ResultsModel, RollCallModel
- ElectionLoader, ElectionExporter, ElectionDeleter
- MonitorModel, PulseModel

---

## 6. Database Requirements

### 6.1 Database Technology
**Production**: SQL Server (any edition including Express)
**Local Development**: SQL Express (SQL Server 2019 Express or later)

**Rationale**:
- SQL Express is free and sufficient for local development
- Compatible with production SQL Server deployments
- Supports all required features (computed columns, row versioning, indexes)

### 6.2 Connection Configuration
**Connection String Requirements**:
- Named connection: `TallyJ4` (matching existing Program.cs)
- Support for integrated security (Windows Authentication) for local development
- Support for SQL authentication for production deployments
- Password masking in logs (already implemented in Program.cs)

**Configuration Locations**:
- `appsettings.Development.json` - Local SQL Express connection
- `appsettings.json` - Production-ready template (without actual credentials)
- User Secrets - Sensitive credentials for local development

### 6.3 Database Schema
The database must support all 16 entity types defined in the existing models:

**Core Election Entities**:
1. **Election** - Election configuration and metadata
2. **Location** - Voting locations within an election
3. **Teller** - Election workers/administrators
4. **JoinElectionUser** - Links users to elections they can manage

**People & Voting Entities**:
5. **Person** - Voters and candidates (with computed full name fields)
6. **OnlineVoter** - Online voting registration records
7. **OnlineVotingInfo** - Verification codes and voting tokens
8. **Ballot** - Physical/digital ballots
9. **Vote** - Individual votes on ballots

**Results Entities**:
10. **Result** - Tally results per candidate
11. **ResultSummary** - Election result summaries
12. **ResultTie** - Tie-breaking records

**Supporting Entities**:
13. **ImportFile** - CSV import files with binary content
14. **Message** - System messages
15. **Log** - Audit/change log
16. **SmsLog** - SMS delivery logs (implied from documentation)

**Identity Tables** (managed by ASP.NET Core Identity):
- AspNetUsers (via AppUser entity)
- AspNetRoles
- AspNetUserClaims
- AspNetUserLogins (for OAuth)
- AspNetUserTokens
- AspNetUserRoles
- AspNetRoleClaims

### 6.4 Database Features
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

## 7. Entity Framework Core Setup

### 7.1 Migration Strategy
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

### 7.2 Migration Requirements
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

### 7.3 Database Rebuild Capability
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

## 8. Database Seeding

### 8.1 Seed Data Requirements
Create realistic test data demonstrating all major system features.

**Minimum Seed Data**:

#### 8.1.1 Identity/Users (3-5 users)
- **Admin User**:
  - Email: `admin@tallyj.test`
  - Password: `Admin@123` (development only)
  - Can create elections, manage system

- **Teller User**:
  - Email: `teller@tallyj.test`
  - Password: `Teller@123`
  - Can manage assigned elections

- **Voter User**:
  - Email: `voter@tallyj.test`
  - Password: `Voter@123`

#### 8.1.2 Elections (2-3 test elections)
**Election 1: Active Local Assembly Election**
- Name: "Springfield Local Spiritual Assembly Election 2024"
- Type: Local Assembly
- Mode: In-Person with Online option
- Status: Active/Voting in progress
- Number of Positions: 9
- Locations: 2 voting locations
- Online Voting: Enabled

**Election 2: Completed National Convention**
- Name: "National Convention 2024"
- Type: Convention
- Mode: Delegates only
- Status: Finalized
- Number of Positions: 9
- Locations: 1 location
- Online Voting: Disabled
- Results: Fully calculated with winners

#### 8.1.3 People/Voters (20-50 per election)
For Election 1 (Springfield LSA):
- Eligible Voters: 30 people
- Mix of first/last names
- Various eligibility statuses
- Age 21+ (eligible voting age)
- Some with BahaiId
- Mix of contact methods

#### 8.1.4 Ballots (10-20 per election)
**Election 1 Ballots**:
- In-Person Ballots: 15 ballots
- Online Ballots: 5 ballots
- Various tellers/computers
- Different locations
- Mix of ballot statuses

**Election 2 Ballots** (completed):
- 20 ballots, all processed
- All status: Ok

#### 8.1.5 Votes (9-15 votes per ballot)
- Each ballot has 1-9 votes
- Mix of valid votes and write-in votes
- Various ranks/positions on ballot

#### 8.1.6 Results (for completed elections)
**Election 2 Results**:
- Calculated results for all candidates who received votes
- Vote counts, ranks, sections
- Tie-breaking records (if applicable)

### 8.2 Seed Implementation
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

### 8.3 Seed Configuration
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

---

## 9. Development Workflow

### 9.1 Initial Setup (New Developer)
**Steps**:
1. Clone repository
2. Install SQL Express (if not present)
3. Configure connection string in user secrets or appsettings.Development.json
4. Run `dotnet ef database update` to create database
5. Run application - automatic seeding on first startup (if configured)
6. Access system with test credentials

### 9.2 Schema Changes
**Workflow**:
1. Modify entity classes
2. Run `dotnet ef migrations add <MigrationName>`
3. Review generated migration code

---

## 10. Connection String Examples

### 10.1 SQL Express (Local Development)
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

### 10.2 SQL Server (Production)
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

## 11. Testing & Validation

### 11.1 Database Creation Validation
**Verify**:
- All 16 entity tables created
- All ASP.NET Identity tables created
- All computed columns working correctly
- All foreign keys enforced
- All unique indexes created
- Row version columns functioning

### 11.2 Seed Data Validation
**Verify**:
- All entities have expected record counts
- Relationships are correctly linked
- Computed columns calculated correctly
- User authentication works with seeded credentials

### 11.3 Migration Testing
**Scenarios**:
- Initial database creation
- Database update (existing database)
- Migration rollback
- Database drop and recreate

---

## 12. Documentation Requirements

### 12.1 Code & Architecture Analysis
**Backend**
- ✅ Solution structure identified
- ✅ Controllers catalogued (12 controllers)
- ✅ Entity models documented (15+ entities)
- ✅ SignalR hubs identified (10 hubs)
- ⚠️ Business logic in CoreModels needs detailed documentation
- ⚠️ Authorization rules and security model needs mapping
- ⚠️ API endpoints need cataloguing with request/response examples
- ⚠️ Database schema needs ERD generation
- ⚠️ Stored procedures need identification (if any)
- ⚠️ Configuration settings need documentation

**Frontend**
- ✅ View technology confirmed (Razor MVC)
- ✅ JavaScript frameworks identified (jQuery, Vue 2, SignalR)
- ✅ UI libraries identified (Element UI, jQuery UI)
- ⚠️ All views need screenshots and functional documentation
- ⚠️ Vue components and patterns need extraction
- ⚠️ Client-server communication patterns need mapping
- ⚠️ Form validations and business rules need documentation

### 12.2 Database Documentation
- Complete database schema (tables, columns, types, constraints)
- Relationships and foreign keys
- Indexes and performance optimizations
- Stored procedures, functions, triggers
- Views and materialized queries
- Sample data patterns
- Migration scripts history

### 12.3 Functional Requirements
- Complete feature inventory
- User roles and permissions
- Business rules and validation logic
- Workflows and process flows
- Data transformation and calculation logic
- Reporting requirements
- Email templates and notifications
- File upload/download capabilities
- Export/import functionality

### 12.4 UI/UX Documentation
- **Screenshots**: All pages, modals, and UI states
- User journeys and navigation flows
- Form layouts and field validations
- Responsive behavior and breakpoints
- Error states and validation messages
- Success messages and confirmations
- Loading states and progress indicators

### 12.5 Integration Points
- External APIs consumed
- External services (payment gateways, email services, etc.)
- Authentication providers (OAuth, SAML, etc.)
- File storage systems
- Caching mechanisms (Redis, MemoryCache)
- Message queues or event systems

### 12.6 Non-Functional Requirements
- Performance benchmarks
- Concurrent user capacity
- Security requirements and compliance
- Browser compatibility requirements
- Deployment architecture
- Logging and monitoring approach
- Backup and disaster recovery

---

## 13. Success Criteria

### 13.1 Reverse Engineering Success Criteria
The documentation will be considered complete when:
- An AI or development team can rebuild all functionality without access to the original developers
- All database schemas are fully documented with relationships
- All API endpoints are catalogued with examples
- All UI screens are captured with specifications
- All business rules and validations are explicitly documented
- All third-party integrations are identified with configuration requirements

### 13.2 Build Phase Success Criteria
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

## 14. Assumptions & Constraints

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

## 15. References

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