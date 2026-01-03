# Technical Specification: TallyJ Reverse Engineering & Modernization

## 1. Technical Context

### 1.1 Current Technology Stack (ASP.NET Framework 4.8)

**Backend**
- **Framework**: ASP.NET MVC 5.x on .NET Framework 4.8
- **ORM**: Entity Framework 6.4.4 (Code First with Migrations)
- **DI**: Unity 3.5
- **Real-time**: SignalR 2.4.3 (10 hubs)
- **Auth**: ASP.NET Identity 2.2.4 with OWIN 4.2.2
- **Session**: StateServer (TCP)
- **Security**: FluentSecurity 2.1.0
- **Database**: SQL Server

**Frontend**
- **Views**: Razor (.cshtml) with per-view .js and .less files
- **JavaScript**: jQuery 3.7.1, Vue 2.x (embedded), SignalR client 2.4.3, Element UI
- **CSS**: LESS preprocessor
- **Libraries**: Highcharts, Moment.js, Luxon, CKEditor

**Architecture Patterns**
- MVC with Controllers returning Views/JsonResults
- View-specific assets (1 .cshtml → 1 .js + 1 .less)
- Custom authorization attributes
- SignalR hub-per-feature architecture
- UserSession state management
- Helper/Model classes in CoreModels namespace

### 1.2 Target Technology Stack (.NET Core + Vue 3)

**Backend**
- **Framework**: ASP.NET Core 8.0 LTS (or .NET 9+ if preferred)
- **ORM**: Entity Framework Core 8.0
- **DI**: Built-in ASP.NET Core DI
- **Real-time**: SignalR Core (strongly-typed hubs)
- **Auth**: ASP.NET Core Identity + JWT tokens
- **Session**: Distributed cache (Redis) or stateless JWT approach
- **Security**: Policy-based authorization
- **Database**: SQL Server (with PostgreSQL support option)
- **API**: RESTful API + SignalR hubs

**Frontend**
- **Framework**: Vue 3 Composition API with TypeScript
- **State**: Pinia (official Vue 3 state management)
- **Router**: Vue Router 4
- **Build**: Vite
- **UI Library**: Element Plus (successor to Element UI) or PrimeVue
- **HTTP**: Axios
- **Real-time**: @microsoft/signalr client
- **Charting**: Highcharts or Chart.js
- **Styling**: CSS Modules or Tailwind CSS

**Architecture**
- RESTful API backend (separate from frontend)
- SPA frontend with client-side routing
- Strongly-typed API contracts (C# DTOs ↔ TypeScript interfaces)
- Component-based UI architecture
- Stateless authentication (JWT) or distributed sessions
- Docker containerization
- CI/CD pipeline (GitHub Actions / Azure DevOps)

### 1.3 Migration Technology Mapping

| ASP.NET Framework 4.8 | .NET Core 8.0 |
|----------------------|---------------|
| ASP.NET MVC Controllers | ASP.NET Core API Controllers |
| Razor Views | Vue 3 SFC Components |
| Unity DI | Built-in ServiceCollection |
| FluentSecurity | Policy-based authorization |
| SignalR 2.4.3 | SignalR Core with typed hubs |
| ASP.NET Identity 2.x | ASP.NET Core Identity + JWT |
| StateServer sessions | Redis cache or JWT |
| Entity Framework 6.4.4 | Entity Framework Core 8.0 |
| jQuery + Vue 2 | Vue 3 + Composition API |
| LESS | CSS Modules / Tailwind |

---

## 2. Implementation Approach

### 2.1 Reverse Engineering Strategy

The reverse engineering will be accomplished through a **three-phase analysis approach**:

#### Phase 1: Automated Code Analysis
**Objective**: Extract structural and technical information using automated tools

**Backend Analysis**
1. **Database Schema Extraction**
   - Parse all EF6 `DbContext` and entity classes
   - Generate complete ERD using EF Power Tools or Mermaid diagrams
   - Document all relationships, indexes, constraints
   - Extract validation attributes and computed properties
   - Export SQL schema using EF Migrations

2. **API Endpoint Inventory**
   - Scan all 12 Controllers for public `ActionResult` methods
   - Document routes, HTTP methods, parameters, return types
   - Extract authorization attributes (`[ForAuthenticatedTeller]`, etc.)
   - Map ViewModels and JsonResults to response schemas
   - Generate OpenAPI/Swagger-like specification

3. **Business Logic Documentation**
   - Catalog all classes in `CoreModels` namespace (24+ classes)
   - Extract business rules from Helper and Model classes
   - Document validation logic and custom attributes
   - Map data transformation patterns
   - Identify calculation algorithms (especially election tallying)

4. **SignalR Hub Analysis**
   - Document all 10 hubs and their methods
   - Map client-to-server and server-to-client method signatures
   - Identify connection groups and broadcast patterns
   - Document real-time event flows

5. **Security Model Extraction**
   - Map FluentSecurity rules to authorization requirements
   - Document role definitions and permissions
   - Extract custom authorization attributes
   - Map UserSession patterns

6. **Configuration Analysis**
   - Extract all Web.config settings
   - Document Unity DI registrations
   - Map connection strings and external service configs
   - Identify environment-specific settings

**Frontend Analysis**
1. **View Catalog**
   - List all .cshtml files with their controller/action mappings
   - Map each view to its .js and .less files
   - Document Razor helpers and HTML helpers used

2. **Vue Component Extraction**
   - Identify all Vue 2 component definitions in .js files
   - Document component properties, data, methods, computed
   - Extract Element UI component usage patterns

3. **Client-Server Communication**
   - Map AJAX calls to API endpoints
   - Document SignalR client method registrations
   - Extract form submission patterns

**Tools to Use**
- **Roslyn analyzers** for C# code inspection
- **EF Power Tools** for ERD generation
- **Mermaid** for diagrams
- **Custom scripts** for parsing .cshtml/.js/.less files
- **SQL Server Management Studio** for schema export
- **GitHub Copilot / ChatGPT** for batch code documentation

#### Phase 2: UI/UX Documentation
**Objective**: Capture complete user experience and workflows

**Screenshot Collection** (COMPLETED)
- ✅ 26 screenshots captured covering all major areas
- ✅ Landing page and public views
- ✅ Voter journey (online voting modals)
- ✅ Teller journey (setup, front desk, ballot entry, roll call)
- ✅ Admin journey (monitor, reports, system admin)
- ✅ Different user roles and states documented

**User Flow Mapping**
1. **Voter Workflows**
   - Anonymous → Vote Online → Authenticate (email/phone) → Online Ballot → Submit → Confirmation
   - Teller-assisted voting → Front Desk → Mark status → Ballot collection

2. **Teller Workflows**
   - Join as Teller → Access code → Election setup → Import voters → Send notifications → Front Desk → Roll Call → Ballot Entry → Analyze → Reports

3. **Admin Workflows**
   - Create Election → Configure (4 steps) → Import CSV → Edit people → Enable online voting → Monitor progress → Process ballots → Finalize → Export reports

**Component Mapping**
- Map existing Razor views to proposed Vue 3 SFC structure
- Identify reusable UI patterns (modals, tables, forms, badges)
- Document validation patterns and error messaging
- Extract responsive behaviors

#### Phase 3: Runtime Behavior Analysis
**Objective**: Understand dynamic behavior not visible in static code

**Testing Scenarios**
1. Run application locally at `C:\Dev\TallyJ\v3\Site`
2. Exercise each workflow end-to-end
3. Capture network traffic (browser DevTools)
4. Document SignalR message flows
5. Observe session state management
6. Test multi-teller concurrent scenarios
7. Test online voting flow
8. Test roll call display (projector mode)

**Integration Testing**
- Test OAuth integrations (Google, Facebook)
- Test SMS integration (if Twilio configured)
- Test email notifications
- Test CSV import/export
- Test external logging integrations

### 2.2 Documentation Structure

All documentation artifacts will be stored in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/` with the following structure:

```
.zenflow/tasks/reverse-engineer-and-design-new-cd6a/
├── requirements.md                    # ✅ COMPLETED
├── ui-screenshots-analysis.md         # ✅ COMPLETED
├── ui-screenshots-supplement.md       # ✅ COMPLETED
├── spec.md                            # THIS DOCUMENT
├── plan.md                            # Implementation plan
├── database/
│   ├── schema.sql                     # Full schema export
│   ├── erd.mmd                        # Mermaid ERD diagram
│   ├── entities.md                    # Entity documentation
│   └── migrations-summary.md          # EF migrations history
├── api/
│   ├── endpoints.md                   # Complete API inventory
│   ├── controllers/                   # Per-controller docs
│   │   ├── AccountController.md
│   │   ├── AfterController.md
│   │   ├── BallotsController.md
│   │   ├── BeforeController.md
│   │   ├── DashboardController.md
│   │   ├── ElectionsController.md
│   │   ├── Manage2Controller.md
│   │   ├── PeopleController.md
│   │   ├── PublicController.md
│   │   ├── SetupController.md
│   │   ├── SysAdminController.md
│   │   └── VoteController.md
│   └── models-dto.md                  # Request/response models
├── signalr/
│   ├── hubs-overview.md               # Hub architecture
│   └── hubs/                          # Per-hub docs
│       ├── AllVotersHub.md
│       ├── AnalyzeHub.md
│       ├── BallotImportHub.md
│       ├── FrontDeskHub.md
│       ├── ImportHub.md
│       ├── MainHub.md
│       ├── PublicHub.md
│       ├── RollCallHub.md
│       ├── VoterCodeHub.md
│       └── VoterPersonalHub.md
├── business-logic/
│   ├── overview.md                    # Business logic summary
│   ├── election-workflow.md           # Election state machine
│   ├── ballot-tallying.md             # Tally algorithms
│   ├── eligibility-rules.md           # Voter eligibility logic
│   └── validation-rules.md            # All validation logic
├── security/
│   ├── authentication.md              # Auth flows
│   ├── authorization.md               # Policy/role mapping
│   └── session-management.md          # UserSession patterns
├── frontend/
│   ├── views-inventory.md             # All views catalog
│   ├── vue-components.md              # Extracted Vue 2 components
│   ├── component-hierarchy.md         # Proposed Vue 3 structure
│   └── ui-patterns.md                 # Reusable patterns
├── workflows/
│   ├── voter-journey.md               # Voter user flows
│   ├── teller-journey.md              # Teller user flows
│   └── admin-journey.md               # Admin user flows
├── integrations/
│   ├── oauth.md                       # Google/Facebook OAuth
│   ├── sms.md                         # SMS provider (Twilio?)
│   ├── email.md                       # Email service
│   └── logging.md                     # IFTTT/LogEntries
└── migration/
    ├── architecture.md                # New architecture design
    ├── api-design.md                  # RESTful API design
    ├── vue3-components.md             # Vue 3 component specs
    ├── deployment.md                  # Docker/K8s deployment
    └── phased-plan.md                 # Implementation phases
```

---

## 3. Source Code Structure Changes

### 3.1 Current Structure (ASP.NET Framework 4.8)

```
TallyJ/v3/Site/
├── Controllers/              # 12 MVC controllers
├── Views/                    # Razor .cshtml files
│   ├── Account/
│   ├── After/
│   ├── Ballots/
│   ├── Before/
│   ├── Dashboard/
│   ├── Elections/
│   ├── Manage2/
│   ├── People/
│   ├── Public/
│   ├── Setup/
│   ├── SysAdmin/
│   └── Vote/
├── Scripts/                  # Per-view .js files
├── Content/                  # Per-view .less files
├── Models/                   # ViewModels
├── CoreModels/               # Business logic (24+ classes)
│   ├── Helpers/
│   ├── Models/
│   └── Analyzers/
├── Hubs/                     # 10 SignalR hubs
├── Code/                     # Utilities and helpers
│   ├── UnityDependencyResolver.cs
│   ├── UserSession.cs
│   └── ...
├── Migrations/               # EF6 migrations
├── Web.config
└── Global.asax.cs
```

### 3.2 Target Structure (.NET Core + Vue 3)

**Option A: Monorepo (Recommended)**
```
TallyJ/
├── backend/                           # .NET Core API
│   ├── TallyJ.Api/                    # Web API project
│   │   ├── Controllers/               # API controllers
│   │   ├── Hubs/                      # SignalR hubs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── TallyJ.Core/                   # Domain layer
│   │   ├── Entities/                  # EF Core entities
│   │   ├── Interfaces/                # Repository/service interfaces
│   │   └── Specifications/            # Business rules
│   ├── TallyJ.Application/            # Application layer
│   │   ├── Services/                  # Business logic services
│   │   ├── DTOs/                      # Data transfer objects
│   │   ├── Mappers/                   # Entity ↔ DTO mapping
│   │   └── Validators/                # FluentValidation
│   ├── TallyJ.Infrastructure/         # Infrastructure layer
│   │   ├── Data/                      # EF Core DbContext
│   │   ├── Repositories/              # Data access
│   │   ├── Identity/                  # Auth/identity
│   │   ├── Email/                     # Email service
│   │   ├── Sms/                       # SMS service
│   │   └── Migrations/                # EF Core migrations
│   └── TallyJ.Tests/                  # Unit/integration tests
├── frontend/                          # Vue 3 SPA
│   ├── src/
│   │   ├── components/                # Reusable components
│   │   │   ├── common/                # Buttons, modals, etc.
│   │   │   ├── elections/             # Election-specific
│   │   │   ├── voters/                # Voter management
│   │   │   └── ballots/               # Ballot entry
│   │   ├── views/                     # Page-level components
│   │   │   ├── HomeView.vue
│   │   │   ├── DashboardView.vue
│   │   │   ├── ElectionSetupView.vue
│   │   │   ├── FrontDeskView.vue
│   │   │   ├── BallotEntryView.vue
│   │   │   ├── ReportsView.vue
│   │   │   └── AdminView.vue
│   │   ├── stores/                    # Pinia stores
│   │   │   ├── auth.ts
│   │   │   ├── election.ts
│   │   │   ├── voter.ts
│   │   │   └── ballot.ts
│   │   ├── services/                  # API clients
│   │   │   ├── api.ts                 # Base HTTP client
│   │   │   ├── authService.ts
│   │   │   ├── electionService.ts
│   │   │   ├── voterService.ts
│   │   │   ├── ballotService.ts
│   │   │   └── signalRService.ts
│   │   ├── types/                     # TypeScript types
│   │   │   ├── models.ts              # Domain models
│   │   │   └── api.ts                 # API contracts
│   │   ├── router/                    # Vue Router config
│   │   ├── composables/               # Composition API utilities
│   │   ├── assets/                    # Static assets
│   │   ├── App.vue
│   │   └── main.ts
│   ├── public/
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── docker/
│   ├── Dockerfile.api
│   ├── Dockerfile.frontend
│   └── docker-compose.yml
├── docs/                              # Reverse engineering docs
│   └── (all documentation from 2.2)
└── README.md
```

**Option B: Separate Repositories**
- `tallyj-api` repository (backend)
- `tallyj-web` repository (frontend)
- `tallyj-docs` repository (documentation)

### 3.3 Clean Architecture Adoption

The new system will follow **Clean Architecture** principles:

1. **Core Layer** (`TallyJ.Core`)
   - Domain entities (Election, Person, Ballot, Vote, etc.)
   - Domain interfaces (no dependencies on infrastructure)
   - Business rules and specifications

2. **Application Layer** (`TallyJ.Application`)
   - Use cases / services (CreateElection, ImportVoters, TallyBallots)
   - DTOs for API contracts
   - Validation logic (FluentValidation)
   - Mappers (AutoMapper or Mapster)

3. **Infrastructure Layer** (`TallyJ.Infrastructure`)
   - EF Core DbContext and repositories
   - External service implementations (email, SMS, OAuth)
   - Caching (Redis)
   - File storage

4. **API Layer** (`TallyJ.Api`)
   - API controllers (thin, delegate to Application services)
   - SignalR hubs
   - Authentication/authorization middleware
   - Swagger/OpenAPI generation

**Dependency Flow**: API → Application → Core ← Infrastructure

---

## 4. Data Model / API / Interface Changes

### 4.1 Database Schema Migration

**Approach**: Maintain schema compatibility with minimal changes

1. **Entity Framework 6 → EF Core Migration**
   - Convert EF6 entities to EF Core entities
   - Migrate from `DbModelBuilder` to `ModelBuilder` fluent API
   - Update data annotations to EF Core equivalents
   - Maintain existing table/column names for data migration

2. **Schema Preservation**
   - Keep all 15+ core entities (Election, Person, Ballot, Vote, etc.)
   - Preserve foreign key relationships
   - Maintain indexes for performance
   - Keep computed columns and SQL views

3. **New Additions**
   - Add `CreatedAt`, `UpdatedAt` timestamps to entities (audit)
   - Add `RowVersion` for optimistic concurrency
   - Consider adding `IsDeleted` for soft deletes
   - Add indexes for common query patterns

4. **Data Migration Strategy**
   - Create migration scripts from current production schema
   - Test data import from SQL Server backup
   - Validate referential integrity post-migration
   - Provide rollback plan

### 4.2 API Design

**Transition**: ASP.NET MVC (Views + JSON) → RESTful API + SPA

#### RESTful API Structure

**Base URL**: `https://api.tallyj.com/v1/` or `https://tallyj.com/api/v1/`

**Controllers → API Endpoints Mapping**

1. **AccountController → Auth API**
   ```
   POST   /api/v1/auth/login
   POST   /api/v1/auth/logout
   POST   /api/v1/auth/register
   POST   /api/v1/auth/verify-code
   POST   /api/v1/auth/send-code
   GET    /api/v1/auth/me
   ```

2. **ElectionsController → Elections API**
   ```
   GET    /api/v1/elections
   POST   /api/v1/elections
   GET    /api/v1/elections/{id}
   PUT    /api/v1/elections/{id}
   DELETE /api/v1/elections/{id}
   GET    /api/v1/elections/public
   POST   /api/v1/elections/{id}/duplicate
   POST   /api/v1/elections/{id}/export
   ```

3. **SetupController → Election Setup API**
   ```
   GET    /api/v1/elections/{id}/config
   PUT    /api/v1/elections/{id}/config/general
   PUT    /api/v1/elections/{id}/config/tellers
   PUT    /api/v1/elections/{id}/config/features
   PUT    /api/v1/elections/{id}/config/online-voting
   POST   /api/v1/elections/{id}/import-csv
   GET    /api/v1/elections/{id}/import-files
   ```

4. **PeopleController → Voters API**
   ```
   GET    /api/v1/elections/{id}/voters
   POST   /api/v1/elections/{id}/voters
   GET    /api/v1/elections/{id}/voters/{voterId}
   PUT    /api/v1/elections/{id}/voters/{voterId}
   DELETE /api/v1/elections/{id}/voters/{voterId}
   POST   /api/v1/elections/{id}/voters/import
   POST   /api/v1/elections/{id}/voters/send-notifications
   ```

5. **BeforeController → Front Desk API**
   ```
   GET    /api/v1/elections/{id}/front-desk
   PUT    /api/v1/elections/{id}/front-desk/{voterId}/status
   GET    /api/v1/elections/{id}/roll-call
   ```

6. **BallotsController → Ballots API**
   ```
   GET    /api/v1/elections/{id}/ballots
   POST   /api/v1/elections/{id}/ballots
   GET    /api/v1/elections/{id}/ballots/{ballotId}
   PUT    /api/v1/elections/{id}/ballots/{ballotId}
   DELETE /api/v1/elections/{id}/ballots/{ballotId}
   POST   /api/v1/elections/{id}/ballots/import
   ```

7. **VoteController → Votes API**
   ```
   GET    /api/v1/elections/{id}/ballots/{ballotId}/votes
   POST   /api/v1/elections/{id}/ballots/{ballotId}/votes
   DELETE /api/v1/elections/{id}/ballots/{ballotId}/votes/{voteId}
   ```

8. **AfterController → Results API**
   ```
   GET    /api/v1/elections/{id}/results
   POST   /api/v1/elections/{id}/results/analyze
   POST   /api/v1/elections/{id}/results/finalize
   GET    /api/v1/elections/{id}/reports/{reportType}
   POST   /api/v1/elections/{id}/results/tie-break
   ```

9. **SysAdminController → Admin API**
   ```
   GET    /api/v1/admin/elections
   GET    /api/v1/admin/logs
   GET    /api/v1/admin/online-voting
   GET    /api/v1/admin/unconnected-voters
   ```

10. **Manage2Controller → Management API**
    ```
    GET    /api/v1/elections/{id}/monitor
    GET    /api/v1/elections/{id}/tellers
    POST   /api/v1/elections/{id}/tellers/{tellerId}/assign
    ```

**API Conventions**
- Use nouns for resources (not verbs)
- Use HTTP methods semantically (GET, POST, PUT, DELETE)
- Use plural nouns (`/elections`, `/voters`, `/ballots`)
- Nest resources logically (`/elections/{id}/voters`)
- Return appropriate status codes (200, 201, 400, 401, 404, 500)
- Use JSON for request/response bodies
- Include `Content-Type: application/json` header
- Implement pagination for list endpoints (`?page=1&size=50`)
- Implement filtering/sorting (`?status=active&sort=name`)
- Version the API (`/v1/`)

**Response Format**
```json
{
  "success": true,
  "data": { ... },
  "errors": null,
  "meta": {
    "page": 1,
    "pageSize": 50,
    "total": 200
  }
}
```

**Error Format**
```json
{
  "success": false,
  "data": null,
  "errors": [
    {
      "code": "VALIDATION_ERROR",
      "message": "Election name is required",
      "field": "name"
    }
  ]
}
```

### 4.3 SignalR Hub Migration

**Current Hubs (10)** → **Consolidated Hubs (5)**

| Old Hub | New Hub | Purpose |
|---------|---------|---------|
| MainHub | ElectionHub | General election updates |
| FrontDeskHub | FrontDeskHub | Front desk/roll call real-time |
| AllVotersHub, VoterCodeHub, VoterPersonalHub | VoterHub | Voter management |
| AnalyzeHub, BallotImportHub | BallotHub | Ballot processing |
| PublicHub, RollCallHub | PublicHub | Public displays |
| ImportHub | (merged into VoterHub) | CSV import progress |

**Strongly-Typed Hubs**
```csharp
// Server → Client interface
public interface IElectionClient
{
    Task ElectionUpdated(ElectionDto election);
    Task LocationStatusChanged(LocationStatusDto status);
    Task BallotProcessed(int ballotId);
}

// Hub implementation
public class ElectionHub : Hub<IElectionClient>
{
    public async Task JoinElection(int electionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"election-{electionId}");
    }

    public async Task UpdateLocationStatus(int electionId, int locationId, LocationStatusDto status)
    {
        await Clients.Group($"election-{electionId}").LocationStatusChanged(status);
    }
}
```

**Vue 3 SignalR Client**
```typescript
// services/signalRService.ts
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

export class ElectionHubService {
  private connection: HubConnection | null = null;

  async connect(electionId: number) {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/election`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('ElectionUpdated', (election) => {
      // Update Pinia store
    });

    await this.connection.start();
    await this.connection.invoke('JoinElection', electionId);
  }

  async disconnect() {
    await this.connection?.stop();
  }
}
```

### 4.4 Authentication & Authorization

**IMPORTANT**: TallyJ implements **THREE COMPLETELY INDEPENDENT authentication systems** to serve different user types. See `security/authentication.md` for full details.

#### System 1: Admin Authentication (Username + Password + Optional 2FA)
**Current**: ASP.NET Membership Provider + OWIN Cookie Authentication
**Purpose**: System administrators and election owners

**Technology**:
- Database: `AspNetUsers` table (ASP.NET Identity 2.2.4)
- Credentials: Username + password (hashed)
- Optional: External OAuth (Google, Facebook via `AspNetUserLogins`)
- Session: 7-day cookie, StateServer session
- Claims: `UserName`, `UniqueID` (prefix "A:"), `IsKnownTeller`, `IsSysAdmin` (optional)

**New**: ASP.NET Core Identity + JWT tokens + OAuth2/OIDC
- Add mandatory 2FA (TOTP)
- JWT tokens for stateless authentication
- Refresh tokens for long-lived sessions
- Policy-based authorization

#### System 2: Guest Teller Authentication (Access Code Only - NO PASSWORDS)
**Current**: Election access code validation + temporary OWIN cookie
**Purpose**: Election workers without system accounts

**Technology**:
- **NO user database** - ephemeral authentication
- **NO passwords** - only election access code
- Access code stored in `Election.ElectionPasscode` (plaintext, max 50 chars)
- Temporary username: `T:{sessionId}{guid}`
- Claims: `UserName`, `UniqueID` (prefix "T:"), `IsGuestTeller`
- Session-bound: Loss of session = loss of access

**Process**:
1. User selects election from public list
2. Enters access code (validates against `Election.ElectionPasscode`)
3. If valid, creates temporary ClaimsIdentity with fake username
4. Can work in that election until session expires
5. Cannot create/delete elections, cannot access admin functions

**New**: Keep access code model (works well for this use case)
- Add rate limiting to prevent brute force
- Consider time-limited codes (optional expiration)
- JWT tokens instead of session-bound auth

#### System 3: Voter Authentication (One-Time Code via Email/SMS - NO PASSWORDS, NO ACCOUNTS)
**Current**: Email/SMS verification code + OWIN cookie
**Purpose**: Voters casting online ballots

**Technology**:
- Database: `OnlineVoter` table (NOT AspNetUsers)
- **NO passwords** - one-time 6-digit codes only
- VoterId: Email address or phone number
- VoterIdType: "email" or "sms"
- Code delivery: SMTP (email) or Twilio (SMS)
- Claims: `VoterId`, `VoterIdType`, `IsVoter`, `UniqueID` (prefix "V:")

**Process**:
1. Voter enters email or phone number
2. System generates 6-digit code, sends via email/SMS
3. Voter enters code to verify
4. If valid, creates ClaimsIdentity
5. Voter joins election by matching email/phone to `Person` record
6. Can vote in any election where email/phone matches

**Matching**: Voters matched to `Person` records by:
- `Person.Email` = VoterId (if VoterIdType = "email")
- `Person.Phone` = VoterId (if VoterIdType = "sms")
- `Person.KioskCode` = VoterId (if VoterIdType = "Kiosk")

**New**: ASP.NET Core Identity + passwordless authentication
- Keep one-time code model (excellent for voters)
- Add WebAuthn for biometric authentication (optional)
- Add social login (Google, Facebook, Apple)
- Add magic links as alternative to codes
- JWT tokens for stateless sessions

**JWT Authentication Flow**
1. User submits email/phone → server sends verification code
2. User submits code → server validates and returns JWT access token + refresh token
3. Frontend stores tokens (httpOnly cookies or localStorage)
4. All API requests include `Authorization: Bearer <token>` header
5. Refresh token used to obtain new access token when expired

**Authorization Policies**
```csharp
// Map FluentSecurity rules to policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedTeller", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("Role", "Teller", "Admin"));

    options.AddPolicy("ActiveElectionTeller", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new ActiveElectionRequirement()));

    options.AddPolicy("HeadTeller", policy =>
        policy.RequireClaim("Role", "Admin"));
});

// Use in controllers
[Authorize(Policy = "AuthenticatedTeller")]
public class BallotsController : ControllerBase { }
```

**UserSession Replacement**
- Current: `UserSession.CurrentElectionId`, `UserSession.CurrentTellerId`
- New: Claims in JWT token + request context

```csharp
// Extract from claims
var electionId = int.Parse(User.FindFirst("ElectionId")?.Value ?? "0");
var tellerId = int.Parse(User.FindFirst("TellerId")?.Value ?? "0");
```

### 4.5 Frontend State Management

**Current**: Server-side session + ViewBag/ViewData + per-page JavaScript

**New**: Pinia stores (Vue 3 state management)

**Stores**
```typescript
// stores/auth.ts
export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
    token: null as string | null,
    isAuthenticated: false
  }),
  actions: {
    async login(email: string, code: string) { ... },
    logout() { ... }
  }
});

// stores/election.ts
export const useElectionStore = defineStore('election', {
  state: () => ({
    currentElection: null as Election | null,
    elections: [] as Election[]
  }),
  actions: {
    async fetchElections() { ... },
    async selectElection(id: number) { ... }
  }
});
```

---

## 5. Delivery Phases

### Phase 1: Foundation & Reverse Engineering (Weeks 1-3)

**Objectives**
- Complete reverse engineering documentation
- Set up new project structure
- Establish development environment

**Deliverables**
1. ✅ Requirements document (COMPLETED)
2. ✅ UI/UX screenshots (COMPLETED)
3. Database schema documentation
   - SQL schema export
   - ERD diagram (Mermaid)
   - Entity documentation
4. API endpoint inventory
   - All 12 controllers documented
   - Request/response examples
   - Authorization requirements
5. Business logic documentation
   - CoreModels catalog
   - Tally algorithms
   - Validation rules
6. SignalR hub documentation
   - Hub method signatures
   - Event flow diagrams
7. Security model documentation
   - Authorization mapping
   - Authentication flows
8. New project scaffolding
   - Backend solution structure
   - Frontend Vue 3 project
   - Docker setup
   - CI/CD pipeline (basic)

**Verification**
- All documentation reviewed and approved
- Development environment running locally
- Database migrated to EF Core (dev instance)

### Phase 2: Core API & Authentication (Weeks 4-6)

**Objectives**
- Implement core backend infrastructure
- Build authentication system
- Create base API structure

**Deliverables**
1. EF Core Data Layer
   - All entities migrated
   - DbContext configured
   - Repositories implemented
   - Migrations created
2. Authentication API
   - JWT token generation
   - Email/phone verification
   - OAuth integration (Google, Facebook)
   - Refresh token flow
3. Authorization System
   - Policy-based authorization
   - Custom requirements (ActiveElectionRequirement)
   - Role management
4. Core API Controllers
   - ElectionsController
   - AuthController
   - Health check endpoint
5. Base Services
   - Email service
   - SMS service (Twilio)
   - Logging service

**Verification**
- Unit tests for authentication (>80% coverage)
- Integration tests for elections API
- Postman collection for all endpoints
- Docker container runs successfully

### Phase 3: Election Setup & Voter Management (Weeks 7-9)

**Objectives**
- Implement election creation and configuration
- Build voter import and management
- Create Vue 3 frontend foundation

**Deliverables**
1. Backend APIs
   - Election setup API (4 configuration steps)
   - Voters API (CRUD, import, notifications)
   - Locations API
   - Tellers API
2. Frontend Foundation
   - Vue 3 + Vite setup
   - Router configuration
   - Pinia stores (auth, election, voter)
   - Base layout components
   - Authentication pages
3. Frontend Pages
   - Landing page / Home
   - Dashboard (election list)
   - Election setup (4-step wizard)
   - Voter import/edit
   - Send notifications
4. CSV Import
   - File upload API
   - Column mapping logic
   - Background processing
   - Progress reporting (SignalR)

**Verification**
- End-to-end test: Create election → Import voters → Send notifications
- Frontend unit tests (components)
- API integration tests
- CSV import with 10,000+ records

### Phase 4: Front Desk & Roll Call (Weeks 10-11)

**Objectives**
- Implement teller workflows
- Build front desk and roll call features
- Real-time SignalR integration

**Deliverables**
1. Backend APIs
   - Front desk API (voter status updates)
   - Roll call API
   - SignalR FrontDeskHub
2. Frontend Pages
   - Front desk page (voter search, status marking)
   - Roll call settings page
   - Roll call display (projector mode)
3. Real-time Features
   - SignalR service in Vue 3
   - Live voter status updates
   - Roll call progression

**Verification**
- Multi-teller scenario test (concurrent updates)
- Roll call display in full-screen mode
- SignalR reconnection handling
- Performance test (1000+ voters)

### Phase 5: Ballot Entry & Tallying (Weeks 12-14)

**Objectives**
- Implement ballot entry workflow
- Build tallying algorithms
- Create results reporting

**Deliverables**
1. Backend APIs
   - Ballots API (CRUD)
   - Votes API
   - Tally calculation service
   - Results API
   - SignalR BallotHub
2. Tally Logic
   - Vote counting algorithm (port from current system)
   - Duplicate detection
   - Tie-break handling
   - Result summaries
3. Frontend Pages
   - Monitor progress page
   - Ballot entry page
   - Analyze ballots page
   - Results reports pages
4. Ballot Import
   - External ballot import
   - CSV ballot import

**Verification**
- Tally accuracy test (compare with current system)
- Ballot entry with 100+ ballots
- Duplicate detection validation
- Tie-break scenarios tested

### Phase 6: Online Voting (Weeks 15-17)

**Objectives**
- Implement online voting portal
- Build ballot submission workflow
- Kiosk mode

**Deliverables**
1. Backend APIs
   - Online ballot API (draft, submit)
   - Voter authentication (email/phone code)
   - Ballot processing
   - SignalR VoterHub
2. Frontend Pages
   - Vote online modal
   - Voter authentication flow
   - Online ballot page
   - Confirmation page
   - Kiosk mode
3. Real-time Features
   - Live ballot status updates
   - Online voting countdown
   - Auto-close scheduler

**Verification**
- End-to-end voter journey test
- Concurrent voter test (50+ simultaneous)
- Ballot encryption/security audit
- Mobile responsive testing

### Phase 7: Reports & Admin (Weeks 18-19)

**Objectives**
- Implement reporting system
- Build system administration features
- Chart integration

**Deliverables**
1. Backend APIs
   - Reports API (multiple report types)
   - Export API (PDF, CSV)
   - Admin API (logs, system monitoring)
2. Frontend Pages
   - Reports page (10+ report types)
   - Chart displays (Highcharts)
   - System admin pages
   - Elections list
   - General log
   - Online voting monitoring
   - Unconnected voters
3. Export Features
   - PDF generation
   - CSV export
   - Report printing (presenter mode)

**Verification**
- All 10+ report types generated correctly
- Export large datasets (10,000+ records)
- Admin log filtering and searching

### Phase 8: Testing & Polish (Weeks 20-22)

**Objectives**
- Comprehensive testing
- Performance optimization
- Security audit
- Accessibility improvements

**Deliverables**
1. Testing
   - End-to-end test suite (Playwright/Cypress)
   - Load testing (K6 or JMeter)
   - Security testing (OWASP top 10)
   - Accessibility audit (WCAG 2.1 AA)
2. Performance
   - Database query optimization
   - API response time improvements
   - Frontend bundle optimization
   - Caching strategy (Redis)
3. Documentation
   - API documentation (Swagger)
   - User guide
   - Admin guide
   - Deployment guide
4. Bug Fixes
   - Address all critical/high bugs
   - UX improvements based on testing

**Verification**
- All tests passing
- Load test: 500+ concurrent users
- API response time < 200ms (p95)
- Lighthouse score > 90
- Zero critical security vulnerabilities

### Phase 9: Deployment & Migration (Weeks 23-24)

**Objectives**
- Production deployment
- Data migration from v3.5
- User acceptance testing

**Deliverables**
1. Infrastructure
   - Production environment (Docker/K8s or Azure App Service)
   - Database (Azure SQL or self-hosted SQL Server)
   - Redis cache
   - Monitoring (Application Insights)
   - Logging (Seq, Serilog)
2. Data Migration
   - Migration scripts (v3.5 → v4.0)
   - Data validation
   - Backup/rollback plan
3. Deployment
   - CI/CD pipeline (GitHub Actions / Azure DevOps)
   - Blue-green deployment
   - Health checks
4. UAT
   - Beta testing with select communities
   - Feedback collection
   - Bug fixes

**Verification**
- Production environment stable
- Data migration successful (test with 3+ elections)
- UAT sign-off
- Rollback plan tested

---

## 6. Verification Approach

### 6.1 Testing Strategy

**Unit Testing**
- **Backend**: xUnit + Moq + FluentAssertions
- **Frontend**: Vitest + Vue Test Utils
- **Coverage Target**: >80% for business logic

**Integration Testing**
- **Backend**: WebApplicationFactory for API tests
- **Database**: In-memory SQLite or Testcontainers (SQL Server)
- **Focus**: API endpoints, database interactions, SignalR hubs

**End-to-End Testing**
- **Tool**: Playwright or Cypress
- **Scenarios**: Complete user journeys (voter, teller, admin)
- **Coverage**: All major workflows documented in Phase 2

**Performance Testing**
- **Tool**: K6 or Apache JMeter
- **Scenarios**:
  - 500 concurrent voters submitting ballots
  - 10 tellers entering ballots simultaneously
  - Import 10,000+ voter records
  - Generate reports with 50,000+ votes
- **Metrics**: Response time (p50, p95, p99), throughput, error rate

**Security Testing**
- **OWASP Top 10** checklist
- **Dependency scanning**: Snyk or GitHub Dependabot
- **SAST**: SonarQube
- **Authentication/authorization**: Penetration testing
- **Data validation**: SQL injection, XSS prevention

**Accessibility Testing**
- **Tool**: axe DevTools, Lighthouse
- **Standard**: WCAG 2.1 Level AA
- **Manual testing**: Keyboard navigation, screen reader compatibility

### 6.2 Code Quality

**Linting & Formatting**
- **Backend**: .editorconfig, StyleCop, Roslynator
- **Frontend**: ESLint, Prettier, TypeScript strict mode

**Code Review**
- All PRs require review
- Automated checks (tests, linting, build)
- Security review for auth/sensitive code

**Documentation**
- XML comments for public APIs
- JSDoc for complex TypeScript functions
- README for each major component

### 6.3 Continuous Integration

**GitHub Actions / Azure DevOps Pipeline**
```yaml
# Example CI workflow
on: [push, pull_request]
jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      - name: Upload coverage
        uses: codecov/codecov-action@v3
  
  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: 20
      - name: Install
        run: npm ci
      - name: Lint
        run: npm run lint
      - name: Type Check
        run: npm run type-check
      - name: Test
        run: npm run test:unit
      - name: Build
        run: npm run build
```

### 6.4 Acceptance Criteria

**For each phase, verify:**
1. All planned features implemented
2. All tests passing (unit, integration, E2E)
3. Code review completed
4. Documentation updated
5. Performance benchmarks met
6. Security checklist completed
7. Accessibility checks passed
8. User acceptance (for UAT phases)

**Final acceptance criteria (Phase 9):**
- ✅ All features from v3.5 replicated in v4.0
- ✅ Data migration successful for 5+ real elections
- ✅ Performance equal to or better than v3.5
- ✅ Zero critical bugs
- ✅ UAT sign-off from 3+ beta communities
- ✅ Documentation complete (user + admin guides)
- ✅ Production deployment successful
- ✅ Monitoring and alerting operational

---

## 7. Risks & Mitigation

### Risk 1: Incomplete Business Logic Documentation
**Impact**: Missing critical validation rules or calculation logic  
**Mitigation**:
- Compare tally results between v3.5 and v4.0 using identical data
- Engage original stakeholders for validation
- Create comprehensive test data covering edge cases

### Risk 2: Performance Degradation
**Impact**: Slower response times with .NET Core vs Framework  
**Mitigation**:
- Benchmark early and often
- Optimize database queries (indexes, query plans)
- Implement caching (Redis)
- Use async/await throughout
- Load test with realistic data volumes

### Risk 3: SignalR Migration Complexity
**Impact**: Real-time features not working correctly  
**Mitigation**:
- Implement strongly-typed hubs early
- Test reconnection scenarios
- Monitor connection stability
- Fallback to polling if SignalR fails

### Risk 4: Data Migration Issues
**Impact**: Data loss or corruption during migration  
**Mitigation**:
- Test migration on copies of production data
- Validate data integrity post-migration
- Maintain rollback plan
- Run migration scripts multiple times before production

### Risk 5: Timeline Overruns
**Impact**: Project takes longer than 24 weeks  
**Mitigation**:
- Prioritize core features (MVP approach)
- Defer non-critical features to v4.1
- Allocate buffer time (20% contingency)
- Regular sprint reviews to adjust scope

---

## 8. Next Steps

### Immediate Actions (This Week)

1. **Approve This Specification**
   - Review with stakeholders
   - Confirm target technology stack
   - Confirm timeline (24 weeks)

2. **Set Up Development Environment**
   - Provision developer machines
   - Set up Git repository (monorepo or separate repos)
   - Configure Azure DevOps / GitHub Projects
   - Set up development database (copy of production)

3. **Begin Phase 1: Reverse Engineering**
   - Database schema extraction (SQL export)
   - Generate ERD using EF Power Tools
   - Start API endpoint inventory (AccountController first)
   - Document first CoreModel (ElectionHelper)

### Week 1 Deliverables

- Database schema.sql export
- ERD diagram (Mermaid format)
- AccountController documentation
- AuthenticationController API specification
- ElectionHelper business logic documentation

### Planning Document (Next)

After this specification is approved, create `plan.md` with:
- Detailed task breakdown for each phase
- Sprint planning (2-week sprints)
- Resource allocation
- Risk register
- Decision log
- Progress tracking

---

## Appendix A: Technology Decision Rationale

**Why .NET 8 LTS?**
- Long-term support until Nov 2026
- Mature ecosystem
- Performance improvements over Framework 4.8
- Cross-platform (Linux containers)

**Why Vue 3 Composition API?**
- Modern, maintainable code
- Better TypeScript support
- Smaller bundle size than Vue 2
- Active ecosystem
- Easier testing

**Why Element Plus?**
- Direct successor to current Element UI
- Familiar to users (similar UI patterns)
- Comprehensive component library
- Good documentation

**Why EF Core over Dapper?**
- Maintains consistency with current EF6
- Migration is simpler
- Change tracking for audit logs
- LINQ query support
- Code-first migrations

**Why JWT over Session?**
- Stateless (easier to scale horizontally)
- Mobile-friendly
- Works with SPA architecture
- Can store in httpOnly cookies for security

**Why Pinia over Vuex?**
- Official Vue 3 recommendation
- Better TypeScript support
- Simpler API
- Composition API friendly

**Why Docker?**
- Consistent environments (dev, staging, prod)
- Easier deployment
- Platform-independent
- Supports Kubernetes for scaling

---

## Appendix B: Migration from ASP.NET Framework Patterns

### Session State → JWT Claims + Distributed Cache

**Before (Framework 4.8)**
```csharp
// UserSession.cs
public static class UserSession
{
    public static int CurrentElectionId 
    { 
        get => (int)(HttpContext.Current.Session["ElectionId"] ?? 0); 
        set => HttpContext.Current.Session["ElectionId"] = value; 
    }
}
```

**After (.NET Core 8)**
```csharp
// Claims in JWT
var claims = new List<Claim>
{
    new Claim("ElectionId", electionId.ToString()),
    new Claim("TellerId", tellerId.ToString()),
    new Claim("Role", "Teller")
};

// Access in controller
var electionId = int.Parse(User.FindFirst("ElectionId")?.Value ?? "0");
```

### Unity DI → Built-in DI

**Before**
```csharp
// UnityConfig.cs
container.RegisterType<IElectionRepository, ElectionRepository>();
```

**After**
```csharp
// Program.cs
builder.Services.AddScoped<IElectionRepository, ElectionRepository>();
```

### FluentSecurity → Policy-Based Authorization

**Before**
```csharp
[ForAuthenticatedTeller]
public ActionResult FrontDesk()
```

**After**
```csharp
[Authorize(Policy = "AuthenticatedTeller")]
public async Task<IActionResult> GetFrontDesk()
```

### Razor Views → Vue SFCs

**Before**
```html
<!-- Views/Before/FrontDesk.cshtml -->
@{
    ViewBag.Title = "Front Desk";
}
<div id="frontDesk">
    <!-- HTML -->
</div>
@section Scripts {
    <script src="~/Scripts/Before/FrontDesk.js"></script>
}
```

**After**
```vue
<!-- views/FrontDeskView.vue -->
<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useFrontDeskStore } from '@/stores/frontDesk';

const store = useFrontDeskStore();
</script>

<template>
  <div class="front-desk">
    <!-- Template -->
  </div>
</template>

<style scoped>
/* CSS */
</style>
```

---

**End of Technical Specification**
