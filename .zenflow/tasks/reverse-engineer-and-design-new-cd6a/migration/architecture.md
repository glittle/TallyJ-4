# TallyJ Migration Architecture Document

## 1. Executive Summary

### 1.1 System Overview

**TallyJ** is an election management and ballot tallying system for Bahá'í communities serving up to 50,000 members. It facilitates complete election processes including voter registration, ballot collection, online voting, and tally reporting.

**Current Version**: 3.5.28 (April 4, 2024)  
**Current Deployment**: https://tallyj.com + self-hosted instances  
**Current Architecture**: ASP.NET Framework 4.8 MVC + Razor + jQuery/Vue 2

### 1.2 Current State (ASP.NET Framework 4.8)

**Backend Stack**:
- ASP.NET MVC 5.x on .NET Framework 4.8
- Entity Framework 6.4.4 (Code First)
- Unity 3.5 (Dependency Injection)
- SignalR 2.4.3 (10 hubs for real-time communication)
- ASP.NET Identity 2.2.4 + OWIN 4.2.2 (Authentication)
- StateServer session management (TCP on localhost:42424)
- FluentSecurity 2.1.0 (Authorization)
- SQL Server database

**Frontend Stack**:
- Razor views (.cshtml)
- jQuery 3.7.1 + jQuery UI 1.13.2
- Vue.js 2.x (embedded, no SFCs)
- Element UI components
- SignalR JavaScript client 2.4.3
- LESS preprocessor (per-view stylesheets)
- Highcharts (reporting)

**Architecture Patterns**:
- MVC pattern with Controllers returning Views/JsonResults
- View-specific assets (1 view → 1 .js + 1 .less file)
- Custom authorization attributes
- Hub-per-feature SignalR architecture
- UserSession state management class

### 1.3 Target State (.NET Core 8 + Vue 3)

**Backend Stack**:
- ASP.NET Core 8.0 LTS
- Entity Framework Core 8.0
- Built-in ASP.NET Core DI
- SignalR Core (strongly-typed hubs)
- ASP.NET Core Identity + JWT tokens
- Redis distributed cache or stateless JWT
- Policy-based authorization
- SQL Server (PostgreSQL support optional)
- RESTful API architecture

**Frontend Stack**:
- Vue 3 Composition API with TypeScript
- Pinia (state management)
- Vue Router 4
- Vite (build tool)
- Element Plus or PrimeVue
- Axios (HTTP client)
- @microsoft/signalr client
- Highcharts or Chart.js
- CSS Modules or Tailwind CSS

**Architecture**:
- Clean Architecture (Domain, Application, Infrastructure, API layers)
- RESTful API backend (separate from frontend)
- SPA frontend with client-side routing
- Strongly-typed API contracts (C# DTOs ↔ TypeScript interfaces)
- Component-based UI architecture
- Stateless authentication (JWT) or distributed sessions
- Docker containerization
- CI/CD pipeline (GitHub Actions / Azure DevOps)

### 1.4 Migration Rationale

**Why Migrate?**

1. **.NET Framework 4.8 is Legacy**: No new features, long-term support ending
2. **Performance**: .NET Core 8 is 3-5x faster than .NET Framework
3. **Cross-Platform**: Can deploy on Linux containers (cost savings)
4. **Modern Tooling**: Better IDE support, faster build times
5. **Security**: Latest security features and patches
6. **Maintainability**: Modern architecture patterns, cleaner separation of concerns
7. **Developer Experience**: Composition API, TypeScript, better tooling
8. **Scalability**: Better support for cloud-native architectures

**Risks of NOT Migrating**:
- Security vulnerabilities in unsupported frameworks
- Difficulty hiring developers (legacy tech stack)
- Higher hosting costs (Windows-only)
- Limited scaling options
- Technical debt accumulation

### 1.5 Success Criteria

Migration is successful when:

✅ **Functional Parity**: All current features work identically  
✅ **Tally Accuracy**: Algorithm produces identical results (verified by comparison testing)  
✅ **Performance**: Page load times ≤ current system  
✅ **Authentication**: All 3 auth systems working (admin, guest teller, voter)  
✅ **Real-Time**: SignalR hubs provide same real-time updates  
✅ **Data Migration**: All production data migrated without loss  
✅ **Deployment**: Docker containers deployable to cloud or on-premises  
✅ **Documentation**: AI/development team can maintain and extend system  

---

## 2. Architecture Comparison

### 2.1 Current vs. Target Stack

| Component | Current (Framework 4.8) | Target (Core 8.0) | Migration Complexity |
|-----------|------------------------|-------------------|---------------------|
| **Web Framework** | ASP.NET MVC 5 | ASP.NET Core 8 API | Medium |
| **ORM** | Entity Framework 6.4.4 | EF Core 8.0 | Medium |
| **DI Container** | Unity 3.5 | Built-in ServiceCollection | Low |
| **Authentication** | ASP.NET Identity 2.x + OWIN | ASP.NET Core Identity + JWT | High |
| **Authorization** | FluentSecurity + Custom Attributes | Policy-based authorization | Medium |
| **Real-Time** | SignalR 2.4.3 | SignalR Core | Medium |
| **Session** | StateServer (TCP) | Redis / JWT | Medium |
| **Views** | Razor .cshtml | Vue 3 SFC | High |
| **JavaScript** | jQuery + Vue 2 | Vue 3 Composition API + TypeScript | High |
| **CSS** | LESS (per-view) | CSS Modules / Tailwind | Low |
| **Deployment** | IIS on Windows | Docker containers (Linux/Windows) | Medium |

### 2.2 Technology Stack Mapping

#### Backend Migrations

**Entity Framework 6 → EF Core 8**
- Code First models remain similar
- Migration syntax changes (`DbContext` configuration)
- `DbSet<T>` properties stay the same
- LINQ queries mostly compatible
- `async/await` patterns encouraged
- No `DbEntityEntry` (replaced with `EntityEntry<T>`)

**Unity DI → Built-in DI**
```csharp
// Old (Unity)
container.RegisterType<IRepository, Repository>();

// New (ASP.NET Core)
services.AddScoped<IRepository, Repository>();
```

**ASP.NET Identity 2.x → Core Identity**
- `User` entity inherits `IdentityUser`
- `UserManager<User>` API similar
- OWIN middleware → ASP.NET Core middleware
- Cookie authentication → JWT tokens (recommended)
- External auth providers: similar OAuth2 flow

**SignalR 2.4.3 → SignalR Core**
```csharp
// Old Pattern (Dual-class)
public class MainHub { ... }
public class MainHubCore : Hub { }

// New Pattern (Single class)
public class MainHub : Hub<IMainClient> { ... }
```

#### Frontend Migrations

**Razor Views → Vue 3 Components**
```html
<!-- Old: Account/LogOn.cshtml -->
@model LogOnModel
<div class="login-form">
  @Html.TextBoxFor(m => m.UserName)
</div>

<!-- New: LoginView.vue -->
<template>
  <div class="login-form">
    <input v-model="userName" />
  </div>
</template>
```

**jQuery AJAX → Axios**
```javascript
// Old
$.post('/Account/LogOn', { userName, password }, function(result) { ... });

// New
await axios.post('/api/auth/login', { userName, password });
```

**Element UI → Element Plus**
- Most components have 1:1 mapping
- API similar, some props renamed
- TypeScript support built-in

### 2.3 Architecture Pattern Changes

#### Current: MVC Monolith

```
Browser → IIS → ASP.NET MVC Controller → Entity Framework → SQL Server
                     ↓
                  Razor View (HTML + JS embedded)
```

**Characteristics**:
- Server-side rendering
- Session-based authentication
- Per-view JavaScript files
- Tight coupling between backend and frontend

#### Target: API + SPA

```
Browser → Vue 3 SPA → RESTful API → EF Core → SQL Server
                          ↓
                     JWT Auth / Redis Sessions
```

**Characteristics**:
- Client-side rendering
- Token-based authentication
- Component-based UI
- Clear separation of concerns
- Stateless backend (scalable)

### 2.4 Deployment Model Changes

#### Current Deployment
- **Platform**: Windows Server with IIS
- **Database**: SQL Server (Azure SQL or on-premises)
- **Session**: StateServer process (tcpip=localhost:42424)
- **Static Files**: Served by IIS
- **Scaling**: Vertical only (larger VM)

#### Target Deployment
- **Platform**: Docker containers (Linux or Windows)
- **Orchestration**: Kubernetes, Docker Compose, or Azure Container Apps
- **Database**: SQL Server, Azure SQL, or PostgreSQL
- **Session**: Redis cluster or stateless JWT
- **Static Files**: CDN + API serves SPA bundle
- **Scaling**: Horizontal (add more container instances)

**Deployment Options**:
1. **Azure**: Container Apps + Azure SQL + Redis Cache
2. **AWS**: ECS Fargate + RDS + ElastiCache
3. **On-Premises**: Docker Compose + SQL Server + Redis
4. **Kubernetes**: Self-managed cluster

---

## 3. Migration Strategy

### 3.1 Migration Approach: Big-Bang Rewrite

**Strategy**: Complete rewrite in parallel, then cutover

**Rationale**:
- Architecture changes too significant for incremental migration
- Frontend requires full rewrite (Razor → Vue 3)
- Authentication systems need complete redesign
- Clean break allows adopting best practices

**Timeline**: 24 weeks (6 months)

**Risks**:
- Extended development period
- Divergence from current system (feature parity challenge)
- Big-bang deployment risk

**Mitigations**:
- Feature freeze on current system during migration
- Weekly comparison testing
- Phased rollout (canary deployment)
- Rollback plan

### 3.2 Nine-Phase Migration Plan

#### Phase 1: Foundation & Infrastructure Setup (2 weeks)

**Objectives**:
- Set up new solution structure
- Configure CI/CD pipeline
- Set up development environments

**Tasks**:
1. Create .NET Core 8 solution with Clean Architecture structure
2. Set up GitHub repository with branch strategy
3. Configure GitHub Actions or Azure DevOps pipeline
4. Set up Docker Compose for local development
5. Create Vue 3 + Vite project with TypeScript
6. Configure code quality tools (ESLint, Prettier, SonarQube)

**Deliverables**:
- `TallyJ.Api` project (backend)
- `TallyJ.Core` project (domain)
- `TallyJ.Application` project (services)
- `TallyJ.Infrastructure` project (data access)
- `tallyj-web` project (Vue 3 frontend)
- Docker Compose file for local development
- CI/CD pipeline running

**Dependencies**: None

#### Phase 2: Database Migration (2 weeks)

**Objectives**:
- Migrate all 16 entities to EF Core
- Create EF Core migrations
- Test data migration scripts

**Tasks**:
1. Port all entity classes to EF Core syntax
2. Port `TallyJDbContext` to EF Core
3. Configure relationships, indexes, constraints
4. Create initial EF Core migration
5. Write data migration scripts (EF6 → EF Core compatible schema)
6. Test migration with production database backup
7. Set up database seeding for development

**Deliverables**:
- All entities in `TallyJ.Core/Entities/`
- `TallyJDbContext` in `TallyJ.Infrastructure/Data/`
- EF Core migrations
- Data migration scripts (SQL)
- Database seeding scripts

**Dependencies**: Phase 1

**Reference Documentation**:
- `database/entities.md` (all 16 entities)
- `database/erd.mmd` (relationships)

#### Phase 3: API Development (3 weeks)

**Objectives**:
- Create RESTful API endpoints for all 12 controllers
- Implement DTOs and mapping logic
- Set up Swagger documentation

**Tasks**:
1. Create API controllers for all 12 areas
2. Define request/response DTOs
3. Implement AutoMapper profiles
4. Add FluentValidation validators
5. Configure Swagger/OpenAPI
6. Add API versioning
7. Implement error handling middleware
8. Add logging (Serilog)

**Deliverables**:
- 12 API controllers
- DTOs for all endpoints
- Swagger documentation
- Postman collection

**Dependencies**: Phase 2

**Reference Documentation**:
- `api/controllers/*.md` (12 controller docs)
- `api/endpoints.md` (API inventory)

#### Phase 4: Authentication & Authorization (3 weeks)

**Objectives**:
- Implement all 3 authentication systems
- Migrate ASP.NET Identity to Core Identity
- Implement JWT token-based authentication

**Tasks**:
1. **System 1 (Admin Auth)**:
   - Set up ASP.NET Core Identity
   - Implement username/password login
   - Implement Google/Facebook OAuth
   - Add JWT token generation
   - Implement refresh tokens

2. **System 2 (Guest Teller Auth)**:
   - Implement access code validation
   - Generate temporary JWT tokens
   - Add rate limiting for access code attempts

3. **System 3 (Voter Auth)**:
   - Implement email/SMS verification code flow
   - Integrate Twilio for SMS
   - Implement SMTP for email codes
   - Add voter matching logic (email/phone → Person)
   - Generate voter JWT tokens

4. **Authorization**:
   - Create authorization policies
   - Implement policy handlers
   - Add role-based access control
   - Add custom authorization attributes

**Deliverables**:
- Authentication middleware
- JWT token service
- Authorization policies
- OAuth integration (Google, Facebook)
- SMS/email code generation
- Voter matching service

**Dependencies**: Phase 3

**Reference Documentation**:
- `security/authentication.md` (3 auth systems)
- `security/authorization.md` (policies and roles)

#### Phase 5: SignalR Migration (2 weeks)

**Objectives**:
- Migrate all 10 SignalR hubs to SignalR Core
- Implement strongly-typed hubs
- Test real-time communication

**Tasks**:
1. Create 10 SignalR Core hubs
2. Implement strongly-typed client interfaces
3. Port connection group logic
4. Implement hub authorization
5. Add reconnection handling
6. Test multi-client scenarios

**Deliverables**:
- 10 SignalR Core hubs
- Hub client interfaces
- Hub integration tests

**Dependencies**: Phase 4

**Reference Documentation**:
- `signalr/hubs-overview.md` (all 10 hubs)

#### Phase 6: Business Logic & Tally Algorithms (3 weeks)

**Objectives**:
- Migrate all CoreModels business logic
- Implement tally algorithms exactly
- Validate tally accuracy

**Tasks**:
1. Port all Helper classes (ElectionHelper, BallotHelper, etc.)
2. Port all Model classes (PeopleModel, ResultsModel, etc.)
3. **Implement tally algorithm** (CRITICAL):
   - Port `ElectionAnalyzerNormal`
   - Port `ElectionAnalyzerSingleName`
   - Port tie detection logic
   - Port result ranking logic
4. Create comparison tests (old system vs. new system)
5. Run tally on production data backups
6. Verify identical results

**Deliverables**:
- All business logic services
- Tally algorithm implementation
- Comparison test suite
- Tally accuracy report

**Dependencies**: Phase 5

**Reference Documentation**:
- `business-logic/tally-algorithms.md` (complete tally logic)

**Success Criteria**: **100% identical tally results on test data**

#### Phase 7: Frontend Development (4 weeks)

**Objectives**:
- Build Vue 3 SPA matching all 26 screenshots
- Implement all user workflows
- Integrate with API and SignalR

**Tasks**:
1. Create component library (buttons, modals, tables, etc.)
2. Implement routing (Vue Router 4)
3. Implement state management (Pinia stores)
4. Build all views:
   - Landing page
   - Login/authentication flows
   - Dashboard
   - Election setup wizard
   - Voter management
   - Front desk
   - Ballot entry
   - Roll call display
   - Reports
   - Admin pages
5. Integrate API services (Axios)
6. Integrate SignalR client
7. Add form validations
8. Implement error handling
9. Add responsive design

**Deliverables**:
- Complete Vue 3 SPA
- All 26 screenshots replicated
- API integration
- SignalR integration
- E2E tests (Playwright/Cypress)

**Dependencies**: Phase 6

**Reference Documentation**:
- `ui-screenshots-analysis.md` (26 screenshots)
- `ui-screenshots-supplement.md` (additional UI details)

#### Phase 8: Integration & Testing (3 weeks)

**Objectives**:
- End-to-end testing
- Performance testing
- Security testing
- Bug fixes

**Tasks**:
1. Integration testing (API + Database)
2. E2E testing (Playwright/Cypress)
3. Load testing (k6 or JMeter)
4. Security testing (OWASP ZAP)
5. Accessibility testing
6. Cross-browser testing
7. Mobile responsive testing
8. Bug triage and fixes
9. Performance optimization

**Deliverables**:
- Integration test suite
- E2E test suite
- Load test results
- Security audit report
- Bug fix releases

**Dependencies**: Phase 7

#### Phase 9: Deployment & Cutover (2 weeks)

**Objectives**:
- Deploy to production
- Migrate production data
- Monitor and support

**Tasks**:
1. Set up production infrastructure (Azure/AWS/on-prem)
2. Deploy backend containers
3. Deploy frontend (CDN + SPA bundle)
4. Migrate production database
5. Configure DNS cutover
6. Implement monitoring (Application Insights, Datadog, etc.)
7. Set up alerting
8. Train users
9. Go-live
10. Monitor and support

**Deliverables**:
- Production environment
- Monitoring dashboards
- User training materials
- Go-live checklist
- Rollback plan

**Dependencies**: Phase 8

**Rollback Plan**:
- Keep old system running for 2 weeks
- DNS switch back if critical issues
- Database backup before migration

---

## 4. Component Migration Mapping

### 4.1 Backend Component Mapping

| Current Component | Target Component | Complexity | Notes |
|-------------------|------------------|------------|-------|
| **Controllers** | | | |
| `AccountController` | `AuthController.cs` | High | 3 auth systems |
| `AfterController` | `ResultsController.cs` | Medium | Reports, tally trigger |
| `BallotsController` | `BallotsController.cs` | Medium | Ballot CRUD |
| `BeforeController` | `SetupController.cs` | Medium | Election setup |
| `DashboardController` | `DashboardController.cs` | Low | Election list |
| `ElectionsController` | `ElectionsController.cs` | Low | Election CRUD |
| `Manage2Controller` | `AdminController.cs` | Low | Admin functions |
| `PeopleController` | `VotersController.cs` | Medium | Voter management |
| `PublicController` | `PublicController.cs` | Medium | Public views, voter login |
| `SetupController` | `SetupController.cs` | Medium | Election wizard |
| `SysAdminController` | `SystemAdminController.cs` | Low | System admin |
| `VoteController` | `VotesController.cs` | Medium | Vote entry |
| **SignalR Hubs** | | | |
| `MainHub` | `MainHub.cs` | Medium | Strongly-typed |
| `FrontDeskHub` | `FrontDeskHub.cs` | Medium | Strongly-typed |
| `RollCallHub` | `RollCallHub.cs` | Medium | Strongly-typed |
| `PublicHub` | `PublicHub.cs` | Medium | Strongly-typed |
| `VoterPersonalHub` | `VoterPersonalHub.cs` | Medium | Strongly-typed |
| `AllVotersHub` | `AllVotersHub.cs` | Medium | Strongly-typed |
| `VoterCodeHub` | `VoterCodeHub.cs` | Medium | Strongly-typed |
| `AnalyzeHub` | `AnalyzeHub.cs` | Medium | Strongly-typed |
| `BallotImportHub` | `BallotImportHub.cs` | Medium | Strongly-typed |
| `ImportHub` | `ImportHub.cs` | Medium | Strongly-typed |
| **Business Logic** | | | |
| `ElectionAnalyzerNormal` | `ElectionAnalyzerService` | High | Tally algorithm |
| `BallotHelper` | `BallotService` | Medium | Ballot validation |
| `PeopleModel` | `VoterService` | Medium | Voter management |
| `ResultsModel` | `ResultsService` | Medium | Result generation |
| `UserSession` | `ICurrentUserService` | High | Session → JWT claims |

### 4.2 Frontend Component Mapping

| Current View | Target Vue Component | Complexity | Notes |
|-------------|----------------------|------------|-------|
| `/Public/Index` | `HomeView.vue` | Medium | Public landing page |
| `/Account/LogOn` | `LoginView.vue` | High | Admin login |
| `/Dashboard/Index` | `DashboardView.vue` | Medium | Election list |
| `/Elections/Create` | `CreateElectionView.vue` | Medium | New election |
| `/Setup/*` | `ElectionSetupView.vue` | High | 4-step wizard |
| `/Before/Import` | `ImportVotersView.vue` | Medium | CSV import |
| `/People/Index` | `VotersView.vue` | Medium | Voter list |
| `/Before/FrontDesk` | `FrontDeskView.vue` | High | Real-time updates |
| `/Ballots/Index` | `BallotEntryView.vue` | High | Ballot entry grid |
| `/Before/RollCall` | `RollCallView.vue` | High | Projector display |
| `/After/Monitor` | `MonitorView.vue` | Medium | Progress dashboard |
| `/After/Analyze` | `AnalyzeView.vue` | Medium | Tally trigger |
| `/After/Report` | `ReportsView.vue` | Medium | Results display |
| `/SysAdmin/Index` | `SystemAdminView.vue` | Medium | Admin tabs |

### 4.3 Database Component Mapping

All 16 entities migrate with minimal changes:

| Entity | Migration Complexity | Notes |
|--------|---------------------|-------|
| `Election` | Low | Add more indexes |
| `Person` | Low | Minor field changes |
| `Ballot` | Low | Same structure |
| `Vote` | Low | Same structure |
| `Location` | Low | Same structure |
| `Teller` | Low | Same structure |
| `Result` | Low | Same structure |
| `ResultSummary` | Low | Same structure |
| `ResultTie` | Low | Same structure |
| `JoinElectionUser` | Medium | Update to Core Identity |
| `OnlineVoter` | Low | Same structure |
| `OnlineVotingInfo` | Low | Same structure |
| `ImportFile` | Low | Same structure |
| `Message` | Low | Same structure |
| `C_Log` | Low | Same structure |
| `SmsLog` | Low | Same structure |
| **Identity Tables** | High | ASP.NET Identity 2 → Core |

**Reference**: `database/entities.md`, `database/erd.mmd`

### 4.4 External Integration Mapping

| Integration | Current | Target | Migration Complexity |
|------------|---------|--------|---------------------|
| **Google OAuth** | OWIN OAuth middleware | ASP.NET Core OAuth | Medium |
| **Facebook OAuth** | OWIN OAuth middleware | ASP.NET Core OAuth | Medium |
| **Twilio SMS** | Direct API calls | Twilio SDK for .NET Core | Low |
| **SMTP Email** | System.Net.Mail | MailKit or SendGrid SDK | Low |
| **LogEntries Logging** | Direct HTTP POST | Serilog LogEntries sink | Low |
| **IFTTT Webhooks** | Direct HTTP POST | IHttpClientFactory | Low |

**Reference**: `integrations/*.md` (oauth.md, sms.md, email.md, logging.md)

---

## 5. Critical Components Deep Dive

### 5.1 Three Authentication Systems

TallyJ has **THREE completely independent authentication systems** that must all be migrated carefully.

#### System 1: Admin Authentication (Username + Password)
**Purpose**: System administrators and election owners

**Current Implementation**:
- ASP.NET Membership Provider
- OWIN Cookie Authentication
- Optional 2FA (not actively used)
- External OAuth (Google, Facebook)

**Target Implementation**:
- ASP.NET Core Identity
- JWT tokens (access + refresh)
- 2FA via TOTP (Google Authenticator)
- External OAuth via ASP.NET Core OAuth middleware

**Migration Complexity**: **High**

**Key Challenges**:
- Password hash migration (Membership → Identity)
- Claims mapping
- External login migration

**Reference**: `security/authentication.md` (System 1 section)

#### System 2: Guest Teller Authentication (Access Code Only)
**Purpose**: Election workers without system accounts

**Current Implementation**:
- No user database
- No passwords
- Election access code validation
- Temporary fake username created from session ID
- Session-bound authentication

**Target Implementation**:
- Same access code model
- JWT tokens (short-lived, 8-hour expiration)
- Rate limiting for brute force protection
- Audit logging

**Migration Complexity**: **Medium**

**Key Challenges**:
- Session-bound → stateless JWT
- Access code security (add rate limiting)

**Reference**: `security/authentication.md` (System 2 section)

#### System 3: Voter Authentication (Email/SMS One-Time Codes)
**Purpose**: Voters authenticating to vote online

**Current Implementation**:
- No user accounts
- No passwords
- 6-digit verification codes sent via email or SMS
- Voter matching by email/phone to Person records
- Twilio SMS integration

**Target Implementation**:
- Same one-time code model
- JWT tokens (short-lived, voter-specific)
- Twilio SDK for SMS
- MailKit or SendGrid for email
- Rate limiting (max 10 codes per 15 minutes)

**Migration Complexity**: **High**

**Key Challenges**:
- Voter matching logic must be exact
- SMS/email integration changes
- Security (code expiration, rate limiting)

**Reference**: `security/authentication.md` (System 3 section)

**CRITICAL**: All three systems must work independently. Do NOT consolidate them.

### 5.2 Tally Algorithms

**Why Critical**: Election results must be **100% accurate and verifiable**. Any error is unacceptable.

**Current Implementation**:
- `ElectionAnalyzerNormal` (for 9-member LSA elections)
- `ElectionAnalyzerSingleName` (for single-position elections)
- Complex tie detection logic
- Result sectioning (Elected, Extra, Other)
- Progress reporting via SignalR

**Algorithm Steps** (Normal Election):
1. Prepare for analysis (load ballots, votes, people)
2. Calculate ballot statistics (spoiled, valid)
3. Count votes (loop through valid ballots)
4. Detect ties (check vote counts within ±5%)
5. Rank candidates
6. Section results (Elected, Extra, Other)
7. Save results to database
8. Notify via SignalR

**Target Implementation**:
- Port algorithm **exactly as-is**
- Add comprehensive unit tests
- Add comparison tests (old vs. new)
- Verify results on production data backups

**Migration Complexity**: **High**

**Testing Strategy**:
1. **Unit Tests**: Test each algorithm step independently
2. **Comparison Tests**: Run tally on same data in both systems, compare results
3. **Production Data Tests**: Use production database backups
4. **Edge Case Tests**: No ballots, all spoiled, multi-way ties, etc.

**Success Criteria**: **100% identical results** on all test data

**Reference**: `business-logic/tally-algorithms.md`

### 5.3 SignalR Real-Time Communication

**Why Critical**: Real-time updates are essential for:
- Multi-teller coordination (multiple users entering ballots simultaneously)
- Roll call displays (projector mode showing live voter status)
- Online voting countdown
- Tally progress reporting

**Current Implementation**:
- 10 SignalR hubs
- Dual-class pattern (Wrapper + Core)
- Connection groups (per-election, Known/Guest)
- Server → Client and Client → Server methods

**Target Implementation**:
- Migrate to SignalR Core
- Strongly-typed hubs (`Hub<IClientMethods>`)
- Same connection group strategy
- Implement reconnection handling
- Add authorization per hub

**Migration Complexity**: **Medium**

**Key Changes**:
```csharp
// Old (SignalR 2.4.3)
public class MainHub
{
    private IHubContext CoreHub { get; set; }
    public void StatusChanged(object info) {
        CoreHub.Clients.All.statusChanged(info);
    }
}
public class MainHubCore : Hub { }

// New (SignalR Core)
public interface IMainClient
{
    Task StatusChanged(object info);
}

public class MainHub : Hub<IMainClient>
{
    public async Task StatusChanged(object info) {
        await Clients.All.StatusChanged(info);
    }
}
```

**Reference**: `signalr/hubs-overview.md`

### 5.4 Voter Matching Logic

**Why Critical**: Voters don't have accounts. They're matched by email or phone to Person records.

**Current Logic**:
1. Voter enters email or phone number
2. System sends verification code
3. Voter enters code → authenticated
4. System finds Person record where `Person.Email = voter email` OR `Person.Phone = voter phone`
5. If no match → voter cannot vote
6. If match → ballot associated with that Person

**Target Implementation**:
- Port matching logic exactly
- Add fuzzy matching? (e.g., phone number normalization)
- Add voter registration step if no match?

**Migration Complexity**: **Medium**

**Key Challenges**:
- Email/phone normalization ("+1 555-1234" vs. "5551234")
- Case sensitivity (email@example.com vs. EMAIL@EXAMPLE.COM)
- Duplicate detection

**Reference**: `security/authentication.md` (System 3), `database/entities.md` (Person, OnlineVoter)

---

## 6. Risk Assessment

### 6.1 High-Risk Areas

#### Risk 1: Tally Algorithm Errors
**Impact**: **CRITICAL** - Incorrect election results

**Likelihood**: Medium

**Mitigation**:
- Port algorithm **exactly as-is** (no "improvements")
- Comprehensive comparison testing (old vs. new)
- Test with production data backups
- Manual verification of edge cases
- Code review by original developer (if available)

#### Risk 2: Authentication System Complexity
**Impact**: **HIGH** - Users unable to log in or vote

**Likelihood**: Medium

**Mitigation**:
- Implement all 3 systems independently
- Extensive testing of each auth flow
- Fallback mechanisms (e.g., manual voter entry)
- User acceptance testing

#### Risk 3: SignalR Migration Issues
**Impact**: **HIGH** - Real-time updates not working

**Likelihood**: Medium

**Mitigation**:
- Test with multiple concurrent users
- Test reconnection scenarios
- Load testing (100+ concurrent tellers)
- Graceful degradation (polling fallback)

#### Risk 4: Data Migration Errors
**Impact**: **CRITICAL** - Loss of election data

**Likelihood**: Low (with proper testing)

**Mitigation**:
- Test migration on production backups
- Dry-run migrations multiple times
- Verify data integrity post-migration
- Keep old database as backup for 6 months

#### Risk 5: Performance Degradation
**Impact**: **MEDIUM** - Slower than current system

**Likelihood**: Low (.NET Core is faster)

**Mitigation**:
- Performance benchmarking during development
- Load testing before go-live
- Database indexing optimization
- Caching strategy (Redis)

### 6.2 Medium-Risk Areas

#### Risk 6: Frontend UI/UX Differences
**Impact**: **MEDIUM** - User confusion

**Likelihood**: Medium

**Mitigation**:
- Exact replication of 26 screenshots
- User acceptance testing with actual tellers
- Training materials and videos
- Phased rollout (canary testing)

#### Risk 7: External Integration Failures
**Impact**: **MEDIUM** - SMS/email not working

**Likelihood**: Low

**Mitigation**:
- Test Twilio integration in sandbox
- Test email delivery (SendGrid or SMTP)
- Fallback providers (backup SMS provider)
- Rate limiting and error handling

#### Risk 8: Deployment Issues
**Impact**: **MEDIUM** - Downtime during cutover

**Likelihood**: Medium

**Mitigation**:
- Blue-green deployment
- DNS cutover strategy
- Rollback plan
- Staging environment testing

### 6.3 Testing Strategy

#### Unit Testing
**Target Coverage**: 80%+ for business logic

**Focus Areas**:
- Tally algorithms
- Voter matching logic
- Ballot validation
- Authorization policies

**Tools**: xUnit, Moq, FluentAssertions

#### Integration Testing
**Focus Areas**:
- API endpoints (all 12 controllers)
- Database operations (EF Core)
- SignalR hubs
- External integrations (Twilio, SMTP)

**Tools**: xUnit, TestContainers (Docker-based test databases)

#### End-to-End Testing
**Scenarios**:
- Admin creates election
- Voters imported via CSV
- Online voting flow (email/SMS)
- Guest teller joins election
- Ballot entry
- Tally analysis
- Results viewing

**Tools**: Playwright or Cypress

#### Comparison Testing
**Critical for Tally Algorithm**:
1. Export test data from current system
2. Run tally in current system → save results
3. Run tally in new system → save results
4. Compare results **exactly** (vote counts, rankings, tie detection)
5. Repeat with production data backups

**Acceptance**: **100% identical results**

#### Load Testing
**Scenarios**:
- 100 concurrent voters voting online
- 10 tellers entering ballots simultaneously
- 500 elections in database
- 50,000 voters in single election

**Tools**: k6, JMeter, or Apache Bench

**Targets**:
- API response time < 200ms (p95)
- SignalR message latency < 100ms
- Page load time < 2 seconds

#### Security Testing
**Focus Areas**:
- Authentication bypasses
- Authorization bypasses
- SQL injection
- XSS attacks
- CSRF attacks
- Rate limiting

**Tools**: OWASP ZAP, Burp Suite

---

## 7. Implementation Checklist

### 7.1 Phase 1: Foundation & Infrastructure Setup

- [ ] Create .NET Core 8 solution with Clean Architecture
  - [ ] `TallyJ.Core` (domain entities)
  - [ ] `TallyJ.Application` (services, interfaces)
  - [ ] `TallyJ.Infrastructure` (data access, external services)
  - [ ] `TallyJ.Api` (Web API)
- [ ] Create Vue 3 project with TypeScript and Vite
- [ ] Set up GitHub repository
- [ ] Configure CI/CD pipeline (GitHub Actions / Azure DevOps)
- [ ] Set up Docker Compose for local development
- [ ] Configure code quality tools (ESLint, Prettier, SonarQube)
- [ ] Create development, staging, production environments

### 7.2 Phase 2: Database Migration

- [ ] Port all 16 entities to EF Core
  - [ ] Election
  - [ ] Person
  - [ ] Ballot
  - [ ] Vote
  - [ ] Location
  - [ ] Teller
  - [ ] Result
  - [ ] ResultSummary
  - [ ] ResultTie
  - [ ] JoinElectionUser
  - [ ] OnlineVoter
  - [ ] OnlineVotingInfo
  - [ ] ImportFile
  - [ ] Message
  - [ ] C_Log
  - [ ] SmsLog
- [ ] Port ASP.NET Identity tables to Core Identity
- [ ] Configure `TallyJDbContext` with all relationships
- [ ] Create initial EF Core migration
- [ ] Write data migration scripts
- [ ] Test migration with production database backup
- [ ] Set up database seeding for development

### 7.3 Phase 3: API Development

- [ ] Create API controllers
  - [ ] `AuthController` (Account)
  - [ ] `ResultsController` (After)
  - [ ] `BallotsController` (Ballots)
  - [ ] `SetupController` (Before/Setup)
  - [ ] `DashboardController` (Dashboard)
  - [ ] `ElectionsController` (Elections)
  - [ ] `AdminController` (Manage2)
  - [ ] `VotersController` (People)
  - [ ] `PublicController` (Public)
  - [ ] `VotesController` (Vote)
  - [ ] `SystemAdminController` (SysAdmin)
  - [ ] `LocationsController` (Location management)
- [ ] Create request/response DTOs for all endpoints
- [ ] Implement AutoMapper profiles (Entity ↔ DTO)
- [ ] Add FluentValidation validators
- [ ] Configure Swagger/OpenAPI documentation
- [ ] Add API versioning
- [ ] Implement error handling middleware
- [ ] Add logging (Serilog)
- [ ] Create Postman collection

### 7.4 Phase 4: Authentication & Authorization

**System 1: Admin Authentication**
- [ ] Set up ASP.NET Core Identity
- [ ] Implement username/password login endpoint
- [ ] Implement JWT token generation (access + refresh)
- [ ] Implement token refresh endpoint
- [ ] Migrate password hashes from Membership to Identity
- [ ] Implement Google OAuth integration
- [ ] Implement Facebook OAuth integration
- [ ] Add 2FA via TOTP (Google Authenticator)
- [ ] Create admin login UI (Vue)

**System 2: Guest Teller Authentication**
- [ ] Implement access code validation endpoint
- [ ] Generate short-lived JWT tokens for guest tellers
- [ ] Add rate limiting for access code attempts
- [ ] Add audit logging for access code usage
- [ ] Create "Join as Teller" UI (Vue)

**System 3: Voter Authentication**
- [ ] Implement verification code generation (6-digit)
- [ ] Integrate Twilio SDK for SMS delivery
- [ ] Integrate MailKit/SendGrid for email delivery
- [ ] Implement code validation endpoint
- [ ] Implement voter matching logic (email/phone → Person)
- [ ] Generate short-lived JWT tokens for voters
- [ ] Add rate limiting (max 10 codes per 15 minutes)
- [ ] Create "Vote Online" authentication UI (Vue)

**Authorization**
- [ ] Create authorization policies
  - [ ] `IsKnownTeller` policy
  - [ ] `IsGuestTeller` policy
  - [ ] `IsVoter` policy
  - [ ] `IsSysAdmin` policy
  - [ ] `AllowTellersInActiveElection` policy
- [ ] Implement policy handlers
- [ ] Add authorization attributes to controllers
- [ ] Test all authorization scenarios

### 7.5 Phase 5: SignalR Migration

- [ ] Migrate SignalR hubs to SignalR Core
  - [ ] `MainHub` (election status)
  - [ ] `FrontDeskHub` (voter registration)
  - [ ] `RollCallHub` (roll call display)
  - [ ] `PublicHub` (home page updates)
  - [ ] `VoterPersonalHub` (per-voter notifications)
  - [ ] `AllVotersHub` (broadcast to all voters)
  - [ ] `VoterCodeHub` (verification code status)
  - [ ] `AnalyzeHub` (tally progress)
  - [ ] `BallotImportHub` (ballot import progress)
  - [ ] `ImportHub` (voter import progress)
- [ ] Create strongly-typed client interfaces
- [ ] Implement connection group logic
- [ ] Implement hub authorization
- [ ] Add reconnection handling
- [ ] Integrate SignalR client in Vue 3 frontend
- [ ] Test multi-client scenarios

### 7.6 Phase 6: Business Logic & Tally Algorithms

- [ ] Port business logic services
  - [ ] `ElectionService`
  - [ ] `BallotService`
  - [ ] `VoterService`
  - [ ] `ResultsService`
  - [ ] `LocationService`
  - [ ] `TellerService`
- [ ] **Port tally algorithms** (CRITICAL)
  - [ ] `ElectionAnalyzerService` (normal elections)
  - [ ] `SingleNameElectionAnalyzerService`
  - [ ] Tie detection logic
  - [ ] Result ranking logic
  - [ ] Result sectioning (Elected, Extra, Other)
- [ ] Port CSV import/export logic
  - [ ] Voter import
  - [ ] Ballot import
  - [ ] Results export
- [ ] Create comparison tests (old vs. new tally)
- [ ] Run tally on production data backups
- [ ] Verify 100% identical results
- [ ] Create unit tests for all business logic

### 7.7 Phase 7: Frontend Development

**Component Library**
- [ ] Create reusable components
  - [ ] Buttons
  - [ ] Modals
  - [ ] Tables/grids
  - [ ] Forms
  - [ ] Badges
  - [ ] Cards
  - [ ] Loading spinners

**Views**
- [ ] `HomeView.vue` (landing page)
- [ ] `LoginView.vue` (admin login)
- [ ] `DashboardView.vue` (election list)
- [ ] `CreateElectionView.vue` (new election)
- [ ] `ElectionSetupView.vue` (4-step wizard)
- [ ] `ImportVotersView.vue` (CSV import)
- [ ] `VotersView.vue` (voter list/edit)
- [ ] `SendNotificationsView.vue` (email/SMS)
- [ ] `FrontDeskView.vue` (voter registration)
- [ ] `BallotEntryView.vue` (ballot entry grid)
- [ ] `RollCallView.vue` (projector display)
- [ ] `MonitorView.vue` (progress dashboard)
- [ ] `AnalyzeView.vue` (tally trigger)
- [ ] `ReportsView.vue` (results display)
- [ ] `SystemAdminView.vue` (admin tabs)
- [ ] `VoteOnlineView.vue` (online ballot)

**State Management (Pinia)**
- [ ] `authStore` (authentication state)
- [ ] `electionStore` (current election)
- [ ] `voterStore` (voter list)
- [ ] `ballotStore` (ballot entry)
- [ ] `resultStore` (tally results)

**Services**
- [ ] `api.ts` (Axios base client)
- [ ] `authService.ts`
- [ ] `electionService.ts`
- [ ] `voterService.ts`
- [ ] `ballotService.ts`
- [ ] `resultService.ts`
- [ ] `signalRService.ts`

**Integration**
- [ ] Integrate API services (Axios)
- [ ] Integrate SignalR client
- [ ] Add form validations
- [ ] Implement error handling (toast notifications)
- [ ] Add responsive design (mobile, tablet, desktop)
- [ ] Add accessibility (ARIA labels, keyboard navigation)

**Testing**
- [ ] Create E2E tests (Playwright/Cypress)
- [ ] Test all user workflows
- [ ] Test responsive design
- [ ] Test accessibility

### 7.8 Phase 8: Integration & Testing

- [ ] Integration testing
  - [ ] API + Database integration tests
  - [ ] SignalR integration tests
  - [ ] External integration tests (Twilio, SMTP)
- [ ] E2E testing
  - [ ] Admin workflow
  - [ ] Teller workflow
  - [ ] Voter workflow
  - [ ] Multi-user scenarios
- [ ] Load testing
  - [ ] 100 concurrent voters
  - [ ] 10 concurrent tellers
  - [ ] Large elections (50,000 voters)
- [ ] Security testing
  - [ ] OWASP ZAP scan
  - [ ] Authentication/authorization tests
  - [ ] SQL injection tests
  - [ ] XSS tests
- [ ] Accessibility testing
- [ ] Cross-browser testing (Chrome, Firefox, Safari, Edge)
- [ ] Mobile testing (iOS, Android)
- [ ] Bug triage and fixes
- [ ] Performance optimization

### 7.9 Phase 9: Deployment & Cutover

- [ ] Set up production infrastructure
  - [ ] Azure Container Apps / AWS ECS / K8s cluster
  - [ ] Azure SQL / RDS / SQL Server
  - [ ] Redis cache (if using distributed sessions)
  - [ ] CDN for static assets
- [ ] Configure CI/CD for production deployments
- [ ] Deploy backend containers
- [ ] Deploy frontend to CDN
- [ ] Set up monitoring
  - [ ] Application Insights / Datadog / New Relic
  - [ ] Serilog logging to LogEntries
  - [ ] Error tracking (Sentry)
- [ ] Set up alerting
  - [ ] Error rate alerts
  - [ ] Performance alerts
  - [ ] Availability alerts
- [ ] Migrate production database
  - [ ] Backup current database
  - [ ] Run migration scripts
  - [ ] Verify data integrity
- [ ] Configure DNS cutover
- [ ] Train users
- [ ] Go-live
- [ ] Monitor and support (24/7 for first week)
- [ ] Keep old system running for 2 weeks (rollback option)

---

## 8. Documentation Index

### 8.1 All Documentation Files

| File | Size | Lines | Purpose |
|------|------|-------|---------|
| **Requirements & Planning** | | | |
| `requirements.md` | 17 KB | 458 | Product requirements document |
| `spec.md` | 47 KB | 1,483 | Technical specification |
| `plan.md` | 8 KB | 247 | Implementation plan |
| `REVERSE-ENGINEERING-SUMMARY.md` | 16 KB | 432 | Summary of reverse engineering work |
| **Database** | | | |
| `database/entities.md` | 22 KB | 594 | All 16 entities + Identity tables |
| `database/erd.mmd` | 12 KB | - | Entity relationship diagram (Mermaid) |
| **API** | | | |
| `api/endpoints.md` | 29 KB | - | Complete API inventory |
| `api/controllers/AccountController.md` | - | - | Account/auth endpoints |
| `api/controllers/AfterController.md` | - | - | Post-election endpoints |
| `api/controllers/BallotsController.md` | - | - | Ballot management endpoints |
| `api/controllers/BeforeController.md` | - | - | Pre-election endpoints |
| `api/controllers/DashboardController.md` | - | - | Dashboard endpoints |
| `api/controllers/ElectionsController.md` | - | - | Election CRUD endpoints |
| `api/controllers/Manage2Controller.md` | - | - | Management endpoints |
| `api/controllers/PeopleController.md` | - | - | Voter management endpoints |
| `api/controllers/PublicController.md` | - | - | Public-facing endpoints |
| `api/controllers/SetupController.md` | - | - | Election setup endpoints |
| `api/controllers/SysAdminController.md` | - | - | System admin endpoints |
| `api/controllers/VoteController.md` | - | - | Vote entry endpoints |
| **SignalR** | | | |
| `signalr/hubs-overview.md` | 19 KB | 692 | All 10 hubs with method signatures |
| **Business Logic** | | | |
| `business-logic/tally-algorithms.md` | 20 KB | 713 | Complete tally algorithm documentation |
| **Security** | | | |
| `security/authentication.md` | 25 KB | 763 | All 3 authentication systems |
| `security/authorization.md` | 28 KB | - | Authorization policies and roles |
| **Configuration** | | | |
| `configuration/settings.md` | 42 KB | 1,297 | Web.config settings and migration |
| **External Integrations** | | | |
| `integrations/oauth.md` | 23 KB | - | Google/Facebook OAuth |
| `integrations/sms.md` | 33 KB | - | Twilio SMS/Voice/WhatsApp |
| `integrations/email.md` | 35 KB | - | SendGrid/SMTP email |
| `integrations/logging.md` | 27 KB | - | LogEntries/IFTTT logging |
| **UI/UX** | | | |
| `ui-screenshots-analysis.md` | 31 KB | - | Analysis of 26 screenshots |
| `ui-screenshots-supplement.md` | 21 KB | - | Additional UI details |
| **Migration** | | | |
| `migration/architecture.md` | **THIS FILE** | - | Migration architecture and plan |

**Total Documentation**: ~400 KB, ~10,000+ lines

### 8.2 Reading Order by Role

#### Project Manager / Product Owner
1. `REVERSE-ENGINEERING-SUMMARY.md` - Quick overview
2. `requirements.md` - Product requirements
3. `migration/architecture.md` - Migration plan (this file)
4. `spec.md` - Technical details

#### Backend Developer
1. `database/entities.md` - Data model
2. `security/authentication.md` - 3 auth systems
3. `business-logic/tally-algorithms.md` - Tally logic
4. `api/endpoints.md` - API inventory
5. `signalr/hubs-overview.md` - Real-time communication
6. `configuration/settings.md` - Configuration migration
7. `integrations/*.md` - External services

#### Frontend Developer
1. `ui-screenshots-analysis.md` - UI requirements
2. `ui-screenshots-supplement.md` - Additional UI details
3. `api/endpoints.md` - API contracts
4. `signalr/hubs-overview.md` - Real-time communication
5. `security/authentication.md` - Auth flows

#### DevOps / Infrastructure
1. `spec.md` - Technical architecture
2. `configuration/settings.md` - Configuration requirements
3. `migration/architecture.md` - Deployment plan (this file)
4. `integrations/*.md` - External service configs

### 8.3 Quick Reference Guide

#### Critical Components Requiring Exact Migration

1. **Tally Algorithm** (`business-logic/tally-algorithms.md`)
   - Must produce 100% identical results
   - Comparison testing required
   - No improvements or optimizations during migration

2. **Three Authentication Systems** (`security/authentication.md`)
   - Admin: Username/password + OAuth
   - Guest Teller: Access code only
   - Voter: Email/SMS one-time codes
   - Do NOT consolidate these systems

3. **Voter Matching Logic** (`security/authentication.md`, System 3)
   - Voters matched by email/phone to Person records
   - Exact string matching required

4. **SignalR Hubs** (`signalr/hubs-overview.md`)
   - 10 hubs providing real-time updates
   - Connection groups strategy critical
   - Reconnection handling required

5. **Database Schema** (`database/entities.md`, `database/erd.mmd`)
   - 16 core entities + Identity tables
   - All relationships must be preserved

#### Common Pitfalls to Avoid

1. **Don't consolidate the 3 auth systems** - they must remain independent
2. **Don't optimize the tally algorithm during migration** - port it exactly
3. **Don't skip comparison testing** - tally accuracy is non-negotiable
4. **Don't forget about guest tellers** - they have no user accounts
5. **Don't assume voters have accounts** - they authenticate via one-time codes
6. **Don't remove SignalR** - real-time updates are essential
7. **Don't skip voter matching logic** - it's critical for online voting
8. **Don't change database schema** - maintain backward compatibility for data migration

### 8.4 Known Limitations and Assumptions

#### Assumptions Made During Reverse Engineering

1. **Source Code Access**: Web.config was accessible, but some runtime behaviors were inferred from documentation
2. **External Integrations**: Twilio and SendGrid configurations assumed based on Web.config settings
3. **Business Logic**: Some helper class implementations inferred from usage patterns
4. **UI Behavior**: Some interactions inferred from screenshots (not runtime testing)

#### Areas Requiring Runtime Verification

1. **Tally Edge Cases**: Multi-way ties, zero ballots, all spoiled ballots
2. **SignalR Reconnection**: Exact behavior after connection loss
3. **Session Timeout**: Exact timeout values and behavior
4. **Email/SMS Templates**: Exact wording and formatting
5. **Access Code Security**: Current rate limiting (if any)
6. **Performance Benchmarks**: Current system performance metrics

#### Gaps in Documentation

1. **Stored Procedures**: None identified, but may exist
2. **Database Triggers**: None documented
3. **Scheduled Jobs**: Any background processes not identified
4. **Custom Scripts**: Any deployment or maintenance scripts
5. **User Training Materials**: Not included in reverse engineering

---

## 9. Conclusion

This migration from ASP.NET Framework 4.8 to .NET Core 8 + Vue 3 is a significant undertaking requiring **24 weeks (6 months)** of focused development effort. The migration is necessary to modernize the technology stack, improve performance, enable cloud-native deployment, and ensure long-term maintainability.

### Success Factors

1. **Complete Documentation**: 55,000+ lines of reverse engineering documentation provide a solid foundation
2. **Phased Approach**: 9-phase migration plan breaks down work into manageable chunks
3. **Testing Strategy**: Comprehensive testing (unit, integration, E2E, comparison, load) ensures quality
4. **Critical Component Focus**: Special attention to tally algorithm, authentication, and SignalR
5. **Rollback Plan**: Old system kept running for 2 weeks post-migration

### Next Steps

1. **Approve Migration Plan**: Review and approve this architecture document
2. **Allocate Resources**: Assign development team (backend, frontend, DevOps)
3. **Set Up Infrastructure**: Create development, staging, production environments
4. **Begin Phase 1**: Foundation & Infrastructure Setup (Week 1)
5. **Weekly Progress Reviews**: Track progress against 24-week timeline

### Contact and Support

For questions about this migration architecture document, refer to:
- **Technical Specification**: `spec.md`
- **Reverse Engineering Summary**: `REVERSE-ENGINEERING-SUMMARY.md`
- **Original Requirements**: `requirements.md`
- **All Documentation**: See section 8.1 Documentation Index

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-03  
**Status**: Ready for Review  
**Next Review Date**: Before Phase 1 begins