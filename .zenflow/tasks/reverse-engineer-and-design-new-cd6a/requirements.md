# Product Requirements Document: TallyJ ASP.NET Framework to .NET Core + Vue 3 Migration

## System Overview

**TallyJ** is an election management and ballot tallying system designed for Bahá'í communities of up to 50,000 members. It facilitates the complete election process including voter registration, ballot collection, online voting, and tally reporting.

**Current Version**: 3.5.28 (April 4, 2024)  
**Current Location**: `C:\Dev\TallyJ\v3\Site`  
**Deployment**: Available at https://tallyj.com and can be self-hosted with IIS + SQL Server  
**Documentation**: https://docs.google.com/document/d/1mlxI_5HWyt-zdr0EyGPzrScInqUXhA5WbT7d0Mb7gJQ/view

## Objectives

1. **Reverse Engineer**: Fully document the current ASP.NET Framework 4.8 application
2. **Create Migration Specs**: Produce detailed specifications that an AI or development team can use to rebuild the system
3. **Ensure Completeness**: Capture all functionality, business logic, data structures, integrations, and UI/UX patterns
4. **Modernize Architecture**: Rebuild using .NET Core backend with Vue 3 Composition API frontend

## Current Technology Stack

### Backend
- **Framework**: ASP.NET MVC on .NET Framework 4.8
- **ORM**: Entity Framework 6.4.4 (Code First with Migrations)
- **DI Container**: Unity 3.5
- **Real-time**: SignalR 2.4.3 (10 Hubs for different areas)
- **Authentication**: ASP.NET Identity 2.2.4 with OWIN 4.2.2
- **Session**: StateServer (tcpip=localhost:42424)
- **Security**: FluentSecurity 2.1.0, custom authorization attributes
- **Data Export**: CSV (LumenWorksCsvReader)
- **HTML Parsing**: CsQuery 1.3.4

### Frontend
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

### Database
- **Primary**: SQL Server (Entity Framework Code First)
- **Connection**: MainConnection3 (configured at machine level in IIS)
- **Session Storage**: StateServer

### Key Architecture Patterns
- **MVC Pattern**: Controllers return Views or JsonResults
- **View-Specific Assets**: Each .cshtml has matching .js and .less files
- **Custom Attributes**: `[ForAuthenticatedTeller]`, `[AllowTellersInActiveElection]`
- **Hub Architecture**: 10 SignalR hubs for real-time communication
- **UserSession**: Session management class for current user/election/computer
- **Model Helpers**: "Helper" and "Model" classes in CoreModels for business logic

## Main Application Areas

### Controllers (12)
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

### SignalR Hubs (10)
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

## Core Data Models

### Primary Entities (Entity Framework DbSets)
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

### View Models (Database Views)
- vBallotInfo
- vElectionListInfo
- vImportFileInfo
- vLocationInfo
- vVoteInfo

### Key Business Models (CoreModels)
- BallotAnalyzer, BallotHelper, BallotNormalModel, BallotSingleModel
- ComputerModel, ElectionHelper, LocationModel
- ImportCsvModel, ImportBallotsModel
- PeopleModel, ResultsModel, RollCallModel
- ElectionLoader, ElectionExporter, ElectionDeleter
- MonitorModel, PulseModel

## Documentation Requirements

### 1. Code & Architecture Analysis

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

### 2. Database Documentation

- Complete database schema (tables, columns, types, constraints)
- Relationships and foreign keys
- Indexes and performance optimizations
- Stored procedures, functions, triggers
- Views and materialized queries
- Sample data patterns
- Migration scripts history

### 3. Functional Requirements

- Complete feature inventory
- User roles and permissions
- Business rules and validation logic
- Workflows and process flows
- Data transformation and calculation logic
- Reporting requirements
- Email templates and notifications
- File upload/download capabilities
- Export/import functionality

### 4. UI/UX Documentation

- **Screenshots**: All pages, modals, and UI states
- User journeys and navigation flows
- Form layouts and field validations
- Responsive behavior and breakpoints
- Error states and validation messages
- Success messages and confirmations
- Loading states and progress indicators

### 5. Integration Points

- External APIs consumed
- External services (payment gateways, email services, etc.)
- Authentication providers (OAuth, SAML, etc.)
- File storage systems
- Caching mechanisms (Redis, MemoryCache)
- Message queues or event systems

### 6. Non-Functional Requirements

- Performance benchmarks
- Concurrent user capacity
- Security requirements and compliance
- Browser compatibility requirements
- Deployment architecture
- Logging and monitoring approach
- Backup and disaster recovery

## Documentation Artifacts Needed

### From Current System

1. **Source Code Access**: Complete codebase
2. **Screenshots**: 
   - All application pages (logged out, logged in, different user roles)
   - Administrative interfaces
   - Configuration screens
   - Reports and exports
   - Error pages
   - Mobile/responsive views
3. **Database Backup**: For schema extraction and sample data
4. **Configuration Files**: All environment configs
5. **Deployment Documentation**: Current deployment process
6. **User Documentation**: If available
7. **Test Cases**: Existing test suites or test documentation

### To Be Created

1. **System Architecture Diagram**: Current state
2. **Database ERD**: Entity relationship diagram
3. **API Inventory**: All endpoints with request/response examples
4. **User Flow Diagrams**: Key user journeys
5. **Feature Matrix**: Comprehensive list of all features
6. **Business Rules Document**: All validation and business logic
7. **Migration Mapping**: ASP.NET Framework → .NET Core equivalents
8. **Vue Component Specification**: Proposed component hierarchy for new frontend

## Recommended Documentation Process

### Phase 1: Static Code Analysis (Automated)
1. **Database Schema Extraction**
   - Generate ERD from Entity Framework models
   - Document all relationships, constraints, indexes
   - Extract computed columns and validation rules
   - Identify stored procedures via StoredProcedures.cs

2. **API Endpoint Inventory**
   - Scan all Controllers for public ActionResult methods
   - Document route patterns, HTTP methods, parameters
   - Extract JsonResult return types
   - Map authorization requirements per endpoint

3. **Business Logic Documentation**
   - Document all CoreModels classes (24+ identified)
   - Extract business rules from validation attributes
   - Map workflow patterns in Helper classes
   - Document SignalR hub methods and event flows

4. **Configuration Documentation**
   - Extract all Web.config settings
   - Document Unity DI registrations
   - Map FluentSecurity authorization rules
   - Document session state configuration

### Phase 2: UI/UX Documentation (Manual + Screenshots)
1. **Screenshot Collection** (PRIORITY)
   - All 12 controller areas (Account, After, Ballots, Before, Dashboard, Elections, Manage2, People, Public, Setup, SysAdmin, Vote)
   - Different user roles: Anonymous, Voter, Teller, Admin
   - All election states/workflows
   - Mobile/responsive views
   - Error states and validation messages
   - Public displays (projector mode)

2. **User Flow Mapping**
   - Voter journey: Registration → Login → Vote → Confirmation
   - Teller journey: Setup → Collect Ballots → Tally → Results
   - Admin journey: Create Election → Configure → Monitor → Export

3. **Feature Inventory**
   - Election creation and configuration
   - Voter/person import (CSV)
   - Online voting portal
   - Ballot entry screens (multiple computers)
   - Real-time roll call display
   - Results calculation and tie-breaking
   - Report generation and export
   - Public election listings

### Phase 3: Runtime Behavior Documentation
1. **SignalR Communication Patterns**
   - Document each hub's client/server methods
   - Map real-time update triggers
   - Document connection groups and broadcasting

2. **Session Management**
   - UserSession usage patterns
   - Election context switching
   - Computer registration flow

3. **Integration Points**
   - External authentication (Facebook, Google OAuth mentioned in config)
   - SMS status endpoint (Twilio integration likely)
   - IFTTT logging integration
   - LogEntries integration

## Questions for Clarification

### Screenshots & Testing

1. **Can you provide screenshots of all major application pages?** Include:
   - Login/registration pages
   - Dashboard and election list
   - Election setup wizard
   - Voter registration/import
   - Ballot entry screens
   - Results and reporting
   - Admin/system administration
   - Public pages and roll call displays
   - Voter portal (online voting)

2. **Do you have access to a running instance** for live testing and screen capture?

3. **Do you have sample/test election data** that demonstrates all features?

### Current System Details

4. **Database access**: Can you provide a database schema export or access to a test database?

5. **External integrations**: What OAuth providers are currently configured? (Google, Facebook seen in config)

6. **SMS functionality**: Is Twilio or another SMS provider integrated for voter notifications?

7. **Email system**: How are election notifications and invitations sent?

8. **Deployment**: Is the production system on Azure, dedicated servers, or other hosting?

### Target System Preferences

9. **Which .NET version** should the new system target? (.NET 6 LTS, .NET 8 LTS, latest?)

10. **Vue 3 UI framework preference?**
    - Element Plus (successor to current Element UI)
    - Vuetify 3
    - PrimeVue
    - Quasar
    - Custom/unstyled

11. **Real-time communication**: 
    - Continue with SignalR (.NET Core version)
    - Consider WebSockets directly
    - Consider alternative (Socket.io, etc.)

12. **Authentication strategy**:
    - ASP.NET Core Identity (direct migration)
    - IdentityServer/Duende
    - OAuth2/OIDC only
    - Auth0 or similar SaaS

13. **Session management**:
    - Distributed cache (Redis)
    - Database-backed sessions
    - JWT tokens (stateless)

14. **Database migration**:
    - Keep SQL Server
    - Consider PostgreSQL
    - Consider MySQL/MariaDB
    - Multi-database support?

15. **Deployment target**:
    - Docker containers + Kubernetes
    - Azure App Service + Azure SQL
    - AWS (ECS/Fargate + RDS)
    - Traditional IIS hosting
    - Multi-platform (Linux + Windows)

## Success Criteria

The documentation will be considered complete when:
- An AI or development team can rebuild all functionality without access to the original developers
- All database schemas are fully documented with relationships
- All API endpoints are catalogued with examples
- All UI screens are captured with specifications
- All business rules and validations are explicitly documented
- All third-party integrations are identified with configuration requirements

## Next Steps

### Immediate Actions Required

1. **Screenshot Collection** (High Priority)
   - Run the application at `C:\Dev\TallyJ\v3\Site`
   - Capture all major pages across 12 controller areas
   - Document different user roles and states
   - Save to artifacts folder with descriptive names

2. **Answer Clarification Questions**
   - Review questions 1-15 above
   - Provide preferences for target technologies
   - Clarify integration requirements

3. **Database Schema Export**
   - Generate SQL script from development database
   - Or provide Entity Framework migration output
   - Or grant read access to test database

4. **Identify Documentation Priorities**
   - Which features are most critical?
   - Which areas are likely to change in rebuild?
   - Which areas must maintain exact compatibility?

### Documentation Workflow

Once screenshots and answers are provided:

**Phase 1 - Automated Analysis** (1-2 days)
- Generate database ERD and schema documentation
- Create API endpoint inventory with all 12 controllers
- Extract business rules from CoreModels
- Document SignalR hub methods
- Map authorization and security rules

**Phase 2 - Feature Documentation** (2-3 days)
- Document each major workflow with screenshots
- Map user journeys for 3 roles (Voter, Teller, Admin)
- Extract and document business logic
- Document validation rules and constraints
- Identify all integration points

**Phase 3 - Technical Specification** (2-3 days)
- Create migration mapping (Framework → Core)
- Design new API structure (RESTful)
- Design Vue 3 component hierarchy
- Plan SignalR hub migration
- Design authentication/authorization system
- Define deployment architecture

**Phase 4 - Implementation Plan** (1 day)
- Break down into implementation phases
- Prioritize feature delivery
- Identify risks and unknowns
- Create development timeline

**Total Estimated Time**: 6-9 days for complete documentation

### Deliverables

The documentation package will include:
1. **Database ERD** - Complete schema with relationships
2. **API Specification** - All endpoints with examples
3. **UI Component Map** - Vue 3 component hierarchy
4. **User Flows** - Diagrams for all user journeys
5. **Business Rules** - All validation and calculation logic
6. **Integration Guide** - OAuth, SMS, email, logging integrations
7. **Migration Mapping** - Framework 4.8 → .NET Core equivalents
8. **Security Model** - Authorization rules and user roles
9. **Deployment Guide** - Infrastructure and configuration
10. **Feature Inventory** - Complete list with priorities

## Assumptions

- **Feature Parity**: The rebuild should maintain all current functionality
- **Modern Architecture**: RESTful APIs, SPA with Vue 3, containerized deployment
- **Enhanced Testability**: Unit tests, integration tests, E2E tests
- **Improved Maintainability**: Better separation of concerns, modern patterns
- **Migration Strategy**: Complete rewrite, not incremental upgrade
- **Data Migration**: Existing elections and data must be importable
- **Backward Compatibility**: Consider data export/import between versions
- **Scalability**: Design for growth beyond 50,000 members
- **Security**: Maintain or improve current security model
- **Accessibility**: Improve accessibility compliance (WCAG)
