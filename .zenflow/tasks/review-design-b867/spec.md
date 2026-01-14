# Technical Specification: TallyJ 4 - Complete System Rebuild

## 1. Technical Context

### 1.1 Project Overview

**Objective**: Rebuild TallyJ election management system from ASP.NET Framework 4.8 to .NET 10 + Vue 3, maintaining feature parity while modernizing architecture and adding new capabilities.

**Timeline**: 1-2 months (solo developer with AI assistance)

**Deployment Strategy**: Runs in parallel with TallyJ v3 for several months before full replacement

**Key Constraint**: Must achieve feature parity before public launch

### 1.2 Current State

**Existing Documentation**:
- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/` - Comprehensive reverse engineering (~66,000 lines)
- Database schema fully documented (16 entities)
- 3 authentication systems documented
- 10 SignalR hubs documented
- Tally algorithms fully documented
- UI/UX screenshots (26 screenshots)

**Initial Build Status**:
- ✅ Basic .NET 9 backend structure
- ✅ Basic Vue 3 frontend structure
- ✅ Authentication foundation (Users, Roles, JWT)
- ✅ Database migrations framework
- ⚠️ Needs upgrade to .NET 10
- ❌ Limited feature implementation

### 1.3 Target Technology Stack

**Backend**
- **.NET 10** (upgrade from current .NET 9)
- **ASP.NET Core Web API** (RESTful + minimal APIs)
- **Entity Framework Core 10**
- **SQL Server Express** (avoid proprietary features for portability)
- **ASP.NET Core Identity** + JWT for admin auth
- **SignalR Core** for real-time features
- **Serilog** (already configured)

**Frontend**
- **Vue 3** (Composition API with `<script setup>`)
- **TypeScript**
- **Vite** (already configured)
- **Element Plus** UI library
- **LESS** for styling (scoped in Vue SFCs, NO Tailwind)
- **Pinia** state management
- **@microsoft/signalr** client
- **vue-i18n** for internationalization

**Authentication**
- **System 1 (Admin)**: Google OAuth, X.com OAuth, optional 2FA, password reset
- **System 2 (Guest Teller)**: Election access codes (no user accounts)
- **System 3 (Voter)**: Email/SMS/WhatsApp one-time codes (no user accounts)

**Infrastructure**
- **Monorepo** structure (frontend + backend in single repo)
- **Cross-platform**: Windows, Linux, macOS
- **Docker** containers for deployment
- **GitHub Actions** for CI/CD (optional)

**Deployment Targets**
- Self-hosted (simple installation for non-technical users)
- Azure VMs with IIS (historical approach)
- Docker containers (recommended modern approach)
- Any cloud provider (AWS, GCP, etc.)

### 1.4 Scale Requirements

**Concurrent Usage** (peak periods):
- 10-30 simultaneous elections
- 20-200 tellers per period
- 10-100 voters per period
- Bursty traffic (idle for weeks, then heavy usage)
- 10-20% annual growth

**Election Lifecycle**:
- Average duration: 1-3 weeks
- Self-contained (can delete old data)
- Data migration from v3 is optional (nice-to-have)

---

## 2. Implementation Approach

### 2.1 Complexity Assessment

**Difficulty Level**: **HARD**

**Justification**:
- Complex multi-tenancy (election isolation)
- 3 independent authentication systems
- Critical business logic (tally algorithms must be exact)
- Real-time collaboration (10 SignalR hubs)
- Large existing codebase to replicate (12 controllers, 16+ entities, 24+ business logic classes)
- New features to add while maintaining parity
- Must support self-hosting by non-technical users
- Architectural modernization (session state → JWT/Redis)
- Internationalization (new requirement)

### 2.2 Architecture Decision: Simplified Clean Architecture

**Chosen Structure** (hybrid approach):

```
TallyJ4/
├── backend/                      # .NET 10 Web API
│   ├── Controllers/              # API controllers (RESTful)
│   ├── EF/
│   │   ├── Context/             # DbContext
│   │   ├── Models/              # Entity classes
│   │   ├── Migrations/          # EF migrations
│   │   ├── Data/                # Seeding, configuration
│   │   └── Identity/            # User/Role entities
│   ├── Hubs/                    # SignalR hubs
│   ├── Services/                # Business logic services
│   │   ├── Elections/           # Election management
│   │   ├── Ballots/             # Ballot entry, tallying
│   │   ├── Voters/              # Voter management
│   │   ├── Auth/                # Authentication services
│   │   ├── Notifications/       # Email, SMS, WhatsApp
│   │   └── Reports/             # Report generation
│   ├── DTOs/                    # Request/response models
│   ├── Helpers/                 # Utilities, extensions
│   ├── Middleware/              # Custom middleware
│   ├── appsettings.json
│   └── Program.cs
├── frontend/                     # Vue 3 SPA
│   ├── src/
│   │   ├── components/          # Reusable components
│   │   │   ├── common/          # Buttons, modals, forms
│   │   │   ├── elections/       # Election-specific
│   │   │   ├── ballots/         # Ballot entry
│   │   │   └── voters/          # Voter management
│   │   ├── pages/               # Page-level components
│   │   │   ├── HomePage.vue
│   │   │   ├── DashboardPage.vue
│   │   │   ├── ElectionSetupPage.vue
│   │   │   ├── FrontDeskPage.vue
│   │   │   ├── BallotEntryPage.vue
│   │   │   ├── ReportsPage.vue
│   │   │   └── AdminPage.vue
│   │   ├── stores/              # Pinia stores
│   │   │   ├── auth.ts
│   │   │   ├── election.ts
│   │   │   ├── voter.ts
│   │   │   └── ballot.ts
│   │   ├── services/            # API clients
│   │   │   ├── api.ts           # Axios instance
│   │   │   ├── authService.ts
│   │   │   ├── electionService.ts
│   │   │   ├── voterService.ts
│   │   │   ├── ballotService.ts
│   │   │   ├── signalRService.ts
│   │   │   └── notificationService.ts
│   │   ├── types/               # TypeScript interfaces
│   │   ├── composables/         # Composition utilities
│   │   ├── locales/             # i18n translation files
│   │   │   ├── en.json
│   │   │   ├── es.json
│   │   │   └── fa.json (Persian)
│   │   ├── router/
│   │   ├── App.vue
│   │   └── main.ts
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── docker/
│   ├── Dockerfile.backend
│   ├── Dockerfile.frontend
│   └── docker-compose.yml
├── .github/
│   └── workflows/
│       └── ci.yml
└── README.md
```

**Why This Structure**:
- ✅ Simpler than full Clean Architecture (fewer projects)
- ✅ Still organized by concern (Controllers, Services, EF)
- ✅ Easy to navigate for solo developer
- ✅ Can refactor to multi-project later if needed
- ✅ Follows existing backend structure (minimal disruption)

### 2.3 Migration from Existing Codebase

**Strategy**: Incremental feature-by-feature migration, not a big-bang rewrite.

**Migration Priority** (based on Q17):

1. **Phase 1: Core Foundation** (Weeks 1-2)
   - ✅ Upgrade to .NET 10
   - Database schema (16 entities)
   - Admin authentication (Google OAuth, X.com OAuth, 2FA, password reset)
   - Basic election CRUD
   - Basic API structure

2. **Phase 2: Critical Features** (Weeks 3-4)
   - **Ballot Entry** (highest priority - "heart of the application")
   - **Tally Algorithms** (must match v3 exactly)
   - **Front Desk** (core feature)
   - Guest Teller authentication
   - Basic SignalR (MainHub, FrontDeskHub)

3. **Phase 3: Online Voting** (Weeks 5-6)
   - Voter authentication (email/SMS codes)
   - Voter portal UI
   - Online ballot submission
   - Voter matching logic (email/phone → Person records)
   - VoterPersonalHub, VoterCodeHub

4. **Phase 4: Reporting & Management** (Weeks 7-8)
   - Report generation (improved subsystem)
   - Election monitoring
   - Voter import/export
   - Roll Call (simplified)
   - Remaining SignalR hubs

5. **Phase 5: New Features** (Ongoing)
   - SMS cost estimation (Twilio API + caching)
   - WhatsApp integration for voter login
   - Linked elections (tie-breaking, two-stage)
   - Dynamic text enlargement
   - i18n (internationalization)

6. **Phase 6: Polish & Deployment** (Final week)
   - Docker setup
   - Deployment scripts
   - Self-hosting documentation
   - Testing/QA
   - Parallel deployment with v3

### 2.4 Key Design Decisions

#### Decision 1: Authentication Architecture

**Three Independent Systems** (preserve from v3):

1. **Admin Authentication**
   - **Method**: Username/password (local accounts) OR OAuth 2.0 (Google, X.com) - user chooses
   - **2FA**: TOTP (Time-based One-Time Password) - optional for all admin users
   - **Password Reset**: Email-based token flow (local accounts only)
   - **Storage**: AspNetUsers (all users), AspNetUserLogins (OAuth mappings), AspNetUserClaims (2FA)
   - **Session**: JWT tokens (stateless)
   - **Claims**: `UserId`, `UserName`, `Email`, `IsSysAdmin`, `AuthProvider` (local/google/x)
   - **Registration**: Users can create local account OR sign up with Google/X.com

2. **Guest Teller Authentication**
   - **Method**: Election access code (Election.ElectionPasscode)
   - **NO user accounts**, NO passwords
   - **Storage**: Session-based (JWT with ElectionGuid claim)
   - **Claims**: `ElectionGuid`, `IsGuestTeller`, `AccessCode`
   - **Expiration**: Session-based (8 hours)

3. **Voter Authentication**
   - **Method**: One-time codes via Email/SMS/WhatsApp
   - **NO user accounts**, NO passwords
   - **Storage**: OnlineVoter table (VoterId = email or phone)
   - **Matching**: OnlineVoter.VoterId → Person.Email OR Person.Phone
   - **Code Generation**: 6-digit numeric
   - **Code Expiration**: 15 minutes
   - **Claims**: `VoterId`, `ElectionGuid`, `IsVoter`, `VerificationMethod`

**Why Preserve 3 Systems**:
- Guest tellers need quick access without account creation
- Voters are non-technical, passwordless is critical
- Admins need security (OAuth, 2FA)

#### Decision 2: Session Management

**Old System**: StateServer (TCP, 6-hour timeout)

**New System**: **JWT tokens + Redis for multi-teller coordination**

**Rationale**:
- JWT for stateless auth (scales better)
- Redis for SignalR backplane (multi-server support)
- Redis for shared state (CurrentElectionGuid, etc.)
- Simpler deployment (no StateServer config)

**JWT Structure**:
```json
{
  "sub": "userId or voterId",
  "type": "admin|teller|voter",
  "electionGuid": "...",
  "claims": [...],
  "exp": 1234567890
}
```

#### Decision 3: SignalR Hub Strategy

**Old System**: 10 hubs with dual-class pattern (Wrapper + Core)

**New System**: **Consolidate to 5-7 strongly-typed hubs**

**Proposed Hubs**:
1. **ElectionHub** - General election status (combines MainHub + PublicHub)
2. **FrontDeskHub** - Voter check-in, registration
3. **BallotHub** - Ballot entry, tally progress (combines AnalyzeHub + BallotImportHub)
4. **VoterHub** - Online voting, verification codes (combines VoterPersonalHub + VoterCodeHub + AllVotersHub)
5. **RollCallHub** - Roll call display (projector mode)
6. **ImportHub** - CSV imports (voters, ballots)
7. **AdminHub** - Admin notifications, monitoring (optional)

**Why Consolidate**:
- Fewer connection overhead
- Easier to maintain
- Strongly-typed contracts (TypeScript on client)
- Still preserves all functionality

#### Decision 4: Internationalization (i18n)

**Libraries**:
- **Frontend**: `vue-i18n` with lazy loading (load on-demand, not pre-bundled)
- **Backend**: `Microsoft.Extensions.Localization`

**Supported Languages** (initial):
- English (en)
- Spanish (es) - large Bahá'í population in Latin America
- Persian (fa) - large population in Iran
- French (fr) - Africa, Canada
- Portuguese (pt) - Brazil

**Translation Strategy**:
- Extract all UI text to JSON locale files
- **AI-powered translations** (80% of work via ChatGPT/Claude)
- Human review for electoral terminology (20% effort)
- Lazy loading: only download selected language
- Language choice persisted in localStorage
- Right-to-left (RTL) support for Persian
- Backend error messages also translatable
- User selects language in profile or header dropdown

**AI Translation Workflow**:
1. Write all strings in `en.json` during development
2. Batch translate with Claude/ChatGPT prompt: "Translate this JSON to [language], maintaining structure"
3. Human review by native speakers (Fiverr, $5-20 per language)
4. Focus review on: electoral terms, formal tone, cultural appropriateness

#### Decision 5: Database Portability

**Primary**: SQL Server Express

**Strategy for Multi-DB Support**:
- Avoid SQL Server-specific features:
  - No `IDENTITY(1000, 1)` - use standard auto-increment
  - No `NEWID()` - generate GUIDs in C#
  - No stored procedures - all logic in C#
  - No T-SQL specific syntax
- Use EF Core abstractions (`.HasDefaultValueSql()` with provider checks)
- Test migrations on PostgreSQL in CI (optional)

**Future PostgreSQL Support**:
- Add `Npgsql.EntityFrameworkCore.PostgreSQL` package
- Connection string in `appsettings.json`
- Provider selection via environment variable

#### Decision 6: Reporting Subsystem Redesign

**Old System Issues**:
- Hard-coded report logic in controllers
- Difficult for developers to add/modify reports
- Limited formatting options

**New System**: **Template-based reporting with Razor views**

**Approach**:
```
Services/Reports/
├── IReportService.cs
├── ReportService.cs
├── Templates/
│   ├── TellerReport.cshtml       # Razor template
│   ├── ResultsReport.cshtml
│   ├── VoterListReport.cshtml
│   └── AuditReport.cshtml
└── Generators/
    ├── PdfGenerator.cs            # HTML → PDF (QuestPDF or Playwright)
    └── ExcelGenerator.cs          # CSV/Excel (ClosedXML)
```

**Benefits**:
- Easy to modify templates (Razor syntax)
- Can preview HTML before PDF export
- Dynamic text enlargement via CSS
- Printable/projectable layouts

#### Decision 7: SMS Cost Estimation (New Feature)

**Requirements**:
- Estimate cost of sending SMS to voters before sending
- Use Twilio Lookup API to get country code
- Cache country → cost mapping in database
- Display total estimated cost to admin

**Implementation**:
```
EF/Models/SmsCostCache.cs       # CountryCode, CostPerSms, LastUpdated
Services/Notifications/SmsService.cs
  ├── EstimateCostAsync(phoneNumbers)
  ├── LookupCountryCode(phoneNumber)  # Twilio Lookup API
  └── GetCostForCountry(countryCode)   # From cache or Twilio Pricing API
```

**UI Flow**:
1. Admin reviews voter list (with phone numbers)
2. Clicks "Send SMS Notifications"
3. System shows: "Estimated cost: $12.50 for 150 messages"
4. Admin confirms or cancels

#### Decision 8: WhatsApp Integration (New Feature)

**Provider**: Twilio WhatsApp API

**Use Case**: Voters receive verification codes via WhatsApp instead of SMS

**Implementation**:
- Same verification code flow
- `NotificationMethod` enum: `Email`, `Sms`, `WhatsApp`
- OnlineVoter.NotificationMethod field
- Voter selects method during authentication

**Benefits**:
- Free for many users (vs. SMS cost)
- Better deliverability in some countries
- Rich formatting (links, buttons)

#### Decision 9: Linked Elections (New Feature)

**Use Case 1: Tie-Breaking**
- Election results in a tie
- Admin creates "tie-break election" linked to parent
- Only tied candidates are on ballot
- Result of tie-break updates parent election

**Use Case 2: Two-Stage Elections**
- Primary election → Secondary election
- Example: Delegate election → National Assembly election
- Results of primary feed into secondary

**Implementation**:
```
Election.ParentElectionGuid (nullable)
Election.ElectionType: Normal | TieBreak | Stage2
```

**UI**:
- "Create Tie-Break Election" button on results page
- Automatically copies relevant candidates
- Links back to parent election

---

## 3. Source Code Structure Changes

### 3.1 Files to Create

**Backend** (new files):

```
backend/
├── Controllers/
│   ├── AuthController.cs                 # NEW - OAuth, 2FA, password reset
│   ├── ElectionController.cs             # EXPAND - linked elections
│   ├── BallotController.cs               # EXPAND
│   ├── VoterController.cs                # EXPAND
│   ├── ReportsController.cs              # NEW - redesigned reporting
│   ├── NotificationsController.cs        # NEW - SMS/email/WhatsApp
│   ├── FrontDeskController.cs            # NEW
│   ├── TallyController.cs                # NEW - results computation
│   └── AdminController.cs                # NEW - system admin
├── Hubs/
│   ├── ElectionHub.cs                    # NEW
│   ├── FrontDeskHub.cs                   # NEW
│   ├── BallotHub.cs                      # NEW
│   ├── VoterHub.cs                       # NEW
│   ├── RollCallHub.cs                    # NEW
│   └── ImportHub.cs                      # NEW
├── Services/
│   ├── Auth/
│   │   ├── GoogleAuthService.cs          # NEW
│   │   ├── XAuthService.cs               # NEW
│   │   ├── TwoFactorService.cs           # NEW
│   │   ├── PasswordResetService.cs       # NEW
│   │   ├── GuestTellerAuthService.cs     # NEW
│   │   └── VoterAuthService.cs           # NEW
│   ├── Elections/
│   │   ├── ElectionService.cs            # NEW
│   │   ├── LinkedElectionService.cs      # NEW
│   │   └── ElectionStateService.cs       # NEW
│   ├── Ballots/
│   │   ├── BallotEntryService.cs         # NEW
│   │   ├── TallyService.cs               # NEW - port from v3 TallyAlgorithm
│   │   ├── TieDetectionService.cs        # NEW
│   │   └── BallotValidationService.cs    # NEW
│   ├── Voters/
│   │   ├── VoterImportService.cs         # NEW
│   │   ├── VoterMatchingService.cs       # NEW - email/phone matching
│   │   └── VoterEligibilityService.cs    # NEW
│   ├── Notifications/
│   │   ├── EmailService.cs               # NEW
│   │   ├── SmsService.cs                 # NEW - with cost estimation
│   │   ├── WhatsAppService.cs            # NEW
│   │   └── NotificationQueueService.cs   # NEW - background processing
│   ├── Reports/
│   │   ├── ReportService.cs              # NEW
│   │   ├── PdfGenerator.cs               # NEW
│   │   └── Templates/                    # Razor templates
│   └── Shared/
│       ├── CacheService.cs               # NEW - Redis wrapper
│       └── LocalizationService.cs        # NEW
├── DTOs/
│   ├── Auth/                             # NEW
│   ├── Elections/                        # NEW
│   ├── Ballots/                          # NEW
│   ├── Voters/                           # NEW
│   └── Reports/                          # NEW
├── Middleware/
│   ├── ElectionContextMiddleware.cs      # NEW - sets current election
│   ├── LocalizationMiddleware.cs         # NEW
│   └── ErrorHandlingMiddleware.cs        # NEW
└── EF/Models/
    ├── OnlineVoter.cs                    # MODIFY - add NotificationMethod
    ├── Election.cs                       # MODIFY - add ParentElectionGuid, ElectionType
    ├── SmsCostCache.cs                   # NEW
    └── TwoFactorToken.cs                 # NEW
```

**Frontend** (new files):

```
frontend/src/
├── pages/
│   ├── auth/
│   │   ├── LoginPage.vue                 # NEW
│   │   ├── RegisterPage.vue              # NEW
│   │   ├── TwoFactorPage.vue             # NEW
│   │   └── PasswordResetPage.vue         # NEW
│   ├── elections/
│   │   ├── ElectionListPage.vue          # NEW
│   │   ├── ElectionSetupPage.vue         # NEW
│   │   ├── ElectionDashboardPage.vue     # NEW
│   │   └── LinkedElectionPage.vue        # NEW
│   ├── ballots/
│   │   ├── BallotEntryPage.vue           # NEW
│   │   ├── TallyResultsPage.vue          # NEW
│   │   └── TieBreakPage.vue              # NEW
│   ├── voters/
│   │   ├── VoterImportPage.vue           # NEW
│   │   ├── VoterListPage.vue             # NEW
│   │   ├── FrontDeskPage.vue             # NEW
│   │   └── OnlineVotingPage.vue          # NEW - voter portal
│   ├── reports/
│   │   ├── ReportsPage.vue               # NEW
│   │   └── TellerReportPage.vue          # NEW - with dynamic enlargement
│   ├── admin/
│   │   ├── SystemAdminPage.vue           # NEW
│   │   └── NotificationSettingsPage.vue  # NEW
│   └── RollCallPage.vue                  # NEW - simplified
├── components/
│   ├── auth/
│   │   ├── GoogleLoginButton.vue         # NEW
│   │   ├── XLoginButton.vue              # NEW
│   │   ├── TwoFactorInput.vue            # NEW
│   │   └── VerificationCodeInput.vue     # NEW
│   ├── elections/
│   │   ├── ElectionCard.vue              # NEW
│   │   ├── ElectionSetupWizard.vue       # NEW - 4 steps
│   │   └── ElectionPhaseNav.vue          # REDESIGN - address confusion
│   ├── ballots/
│   │   ├── BallotEntryForm.vue           # NEW
│   │   ├── VoteInput.vue                 # NEW
│   │   ├── TallyProgress.vue             # NEW
│   │   └── ResultsTable.vue              # NEW
│   ├── voters/
│   │   ├── VoterTable.vue                # NEW
│   │   ├── VoterImportUpload.vue         # NEW
│   │   └── FrontDeskCheckin.vue          # NEW
│   ├── notifications/
│   │   ├── SmsPreview.vue                # NEW - with cost estimate
│   │   ├── NotificationMethodSelector.vue # NEW - Email/SMS/WhatsApp
│   │   └── WhatsAppPreview.vue           # NEW
│   └── common/
│       ├── LanguageSelector.vue          # NEW - i18n
│       ├── TextEnlarger.vue              # NEW - dynamic font sizing
│       └── SignalRStatus.vue             # NEW - connection indicator
├── stores/
│   ├── auth.ts                           # NEW - all 3 auth types
│   ├── election.ts                       # NEW
│   ├── ballot.ts                         # NEW
│   ├── voter.ts                          # NEW
│   ├── notification.ts                   # NEW
│   └── app.ts                            # NEW - locale, theme
├── services/
│   ├── api.ts                            # MODIFY - axios + interceptors
│   ├── authService.ts                    # NEW
│   ├── electionService.ts                # NEW
│   ├── ballotService.ts                  # NEW
│   ├── voterService.ts                   # NEW
│   ├── notificationService.ts            # NEW
│   ├── reportService.ts                  # NEW
│   └── signalRService.ts                 # NEW
├── composables/
│   ├── useAuth.ts                        # NEW
│   ├── useElection.ts                    # NEW
│   ├── useSignalR.ts                     # NEW
│   ├── useNotifications.ts               # NEW
│   └── useLanguage.ts                    # NEW - language switching helper
├── locales/
│   ├── en.json                           # NEW
│   ├── es.json                           # NEW
│   ├── fa.json                           # NEW
│   ├── fr.json                           # NEW
│   └── pt.json                           # NEW
└── types/
    ├── auth.ts                           # NEW
    ├── election.ts                       # NEW
    ├── ballot.ts                         # NEW
    ├── voter.ts                          # NEW
    └── api.ts                            # NEW
```

### 3.2 Files to Modify

**Backend**:
- `Program.cs` - Add OAuth providers, i18n, SignalR, Redis
- `appsettings.json` - Add Google/X OAuth keys, Twilio, WhatsApp, Redis
- `TallyJ4.csproj` - Add NuGet packages (see section 4.2)
- `EF/Context/ApplicationDbContext.cs` - Add new entities (SmsCostCache, TwoFactorToken)
- `EF/Models/Election.cs` - Add ParentElectionGuid, ElectionType
- `EF/Models/OnlineVoter.cs` - Add NotificationMethod

**Frontend**:
- `main.ts` - Add vue-i18n, Element Plus locale
- `vite.config.ts` - Add LESS preprocessor options
- `package.json` - Add dependencies (see section 4.3)
- `router/index.ts` - Add all routes with auth guards
- `App.vue` - Add SignalR connection, locale provider

### 3.3 Files to Delete

**Backend**:
- None (new project)

**Frontend**:
- Consider simplifying existing boilerplate if needed

---

## 4. Data Model / API / Interface Changes

### 4.1 Database Schema Additions

**New Tables**:

```sql
-- SMS cost caching
CREATE TABLE SmsCostCache (
    Id INT PRIMARY KEY IDENTITY,
    CountryCode VARCHAR(5) NOT NULL UNIQUE,
    CountryName NVARCHAR(100),
    CostPerSms DECIMAL(10, 4),
    Currency VARCHAR(3) DEFAULT 'USD',
    LastUpdated DATETIME2 NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 2FA tokens (TOTP)
CREATE TABLE TwoFactorTokens (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    SecretKey NVARCHAR(MAX) NOT NULL, -- Encrypted
    IsEnabled BIT DEFAULT 0,
    RecoveryCodes NVARCHAR(MAX), -- JSON array, encrypted
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);
```

**Modified Tables**:

```sql
-- Add linked election support
ALTER TABLE Elections ADD ParentElectionGuid UNIQUEIDENTIFIER NULL;
ALTER TABLE Elections ADD ElectionType VARCHAR(20) DEFAULT 'Normal'; -- Normal, TieBreak, Stage2
ALTER TABLE Elections ADD FOREIGN KEY (ParentElectionGuid) REFERENCES Elections(ElectionGuid);

-- Add notification method for voters
ALTER TABLE OnlineVoters ADD NotificationMethod VARCHAR(20) DEFAULT 'Email'; -- Email, Sms, WhatsApp

-- Add cost tracking for SMS
ALTER TABLE SmsLog ADD EstimatedCost DECIMAL(10, 4) NULL;
ALTER TABLE SmsLog ADD ActualCost DECIMAL(10, 4) NULL;
ALTER TABLE SmsLog ADD CountryCode VARCHAR(5) NULL;
```

### 4.2 API Design

**RESTful Endpoints** (key routes):

```
Authentication
POST   /api/auth/google/login          # OAuth redirect
POST   /api/auth/x/login               # OAuth redirect
POST   /api/auth/login                 # Username/password
POST   /api/auth/logout
POST   /api/auth/register
POST   /api/auth/2fa/enable
POST   /api/auth/2fa/verify
POST   /api/auth/password/reset-request
POST   /api/auth/password/reset

Guest Teller Auth
POST   /api/teller/join                # Access code validation
POST   /api/teller/leave

Voter Auth
POST   /api/voter/request-code         # Email/SMS/WhatsApp code
POST   /api/voter/verify-code
POST   /api/voter/logout

Elections
GET    /api/elections                  # List all elections
POST   /api/elections                  # Create election
GET    /api/elections/{id}
PUT    /api/elections/{id}
DELETE /api/elections/{id}
POST   /api/elections/{id}/link        # Create linked election (tie-break, stage2)

Voters
GET    /api/elections/{id}/voters
POST   /api/elections/{id}/voters/import  # CSV upload
PUT    /api/elections/{id}/voters/{voterId}
DELETE /api/elections/{id}/voters/{voterId}

Ballots
GET    /api/elections/{id}/ballots
POST   /api/elections/{id}/ballots     # Enter ballot
PUT    /api/elections/{id}/ballots/{ballotId}
DELETE /api/elections/{id}/ballots/{ballotId}

Tally
POST   /api/elections/{id}/tally       # Compute results
GET    /api/elections/{id}/results

Notifications
POST   /api/elections/{id}/notifications/estimate  # Estimate SMS cost
POST   /api/elections/{id}/notifications/send      # Send via Email/SMS/WhatsApp

Reports
GET    /api/elections/{id}/reports/{reportType}    # HTML preview
GET    /api/elections/{id}/reports/{reportType}/pdf
GET    /api/elections/{id}/reports/{reportType}/excel

Front Desk
GET    /api/elections/{id}/frontdesk/voters
POST   /api/elections/{id}/frontdesk/checkin

Roll Call
GET    /api/elections/{id}/rollcall    # Public display data

Admin
GET    /api/admin/system-status
GET    /api/admin/users
```

**SignalR Hub Contracts** (strongly-typed):

```typescript
// ElectionHub
interface IElectionHub {
  // Server → Client
  ElectionStatusChanged(electionId: string, status: string): void;
  ElectionDeleted(electionId: string): void;
  
  // Client → Server
  JoinElection(electionGuid: string): Promise<void>;
  LeaveElection(electionGuid: string): Promise<void>;
}

// FrontDeskHub
interface IFrontDeskHub {
  // Server → Client
  VoterCheckedIn(voterId: number): void;
  VoterStatusChanged(voterId: number, status: string): void;
  
  // Client → Server
  CheckInVoter(voterId: number): Promise<void>;
}

// BallotHub
interface IBallotHub {
  // Server → Client
  BallotEntered(ballotId: number): void;
  TallyProgress(processed: number, total: number, percentage: number): void;
  TallyCompleted(results: TallyResults): void;
  
  // Client → Server
  StartTally(): Promise<void>;
}

// VoterHub
interface IVoterHub {
  // Server → Client
  VerificationCodeSent(method: string): void;
  VotingWindowClosed(): void;
  
  // Client → Server
  RequestCode(method: 'email' | 'sms' | 'whatsapp'): Promise<void>;
}

// RollCallHub
interface IRollCallHub {
  // Server → Client
  RollCallUpdated(voterNames: string[]): void;
}

// ImportHub
interface IImportHub {
  // Server → Client
  ImportProgress(processed: number, total: number): void;
  ImportCompleted(summary: ImportSummary): void;
  ImportError(error: string): void;
}
```

### 4.3 TypeScript Interfaces (Frontend)

**Core Models**:

```typescript
// types/auth.ts
export interface User {
  id: number;
  username: string;
  email: string;
  isSysAdmin: boolean;
  has2FA: boolean;
  authProvider?: 'google' | 'x' | 'password';
}

export interface GuestTeller {
  electionGuid: string;
  accessCode: string;
  joinedAt: Date;
}

export interface Voter {
  voterId: string; // email or phone
  electionGuid: string;
  notificationMethod: 'email' | 'sms' | 'whatsapp';
  verifiedAt?: Date;
}

// types/election.ts
export interface Election {
  electionGuid: string;
  name: string;
  electionType: 'Normal' | 'TieBreak' | 'Stage2';
  parentElectionGuid?: string;
  numberOfSeats: number;
  votingMethods: ('InPerson' | 'Online')[];
  status: 'Setup' | 'Active' | 'Closed' | 'Finalized';
  createdAt: Date;
}

export interface LinkedElection {
  parentElection: Election;
  childElection: Election;
  linkType: 'TieBreak' | 'Stage2';
}

// types/ballot.ts
export interface Ballot {
  id: number;
  electionGuid: string;
  ballotNumber: number;
  statusCode: string;
  votes: Vote[];
  enteredAt: Date;
  enteredBy: string;
}

export interface Vote {
  id: number;
  ballotId: number;
  candidateName: string;
  rank: number;
  statusCode: 'Ok' | 'Spoiled' | 'Changed';
}

export interface TallyResults {
  electionGuid: string;
  results: CandidateResult[];
  hasTie: boolean;
  tieDetails?: TieDetails;
  tallyCompletedAt: Date;
}

export interface CandidateResult {
  candidateName: string;
  voteCount: number;
  section: 'Elected' | 'Extra' | 'Other';
  rank: number;
}

// types/voter.ts
export interface Person {
  id: number;
  electionGuid: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  votingMethod: 'InPerson' | 'Online' | 'Both';
  status: 'NotVoted' | 'Voted' | 'Ineligible';
}

// types/notification.ts
export interface NotificationEstimate {
  totalRecipients: number;
  emailCount: number;
  smsCount: number;
  whatsappCount: number;
  estimatedCost: number;
  currency: string;
  breakdown: CostBreakdown[];
}

export interface CostBreakdown {
  countryCode: string;
  countryName: string;
  count: number;
  costPerMessage: number;
  totalCost: number;
}
```

---

## 5. Verification Approach

### 5.1 Testing Strategy

**Unit Tests** (critical for tally algorithm):
```
backend.Tests/
├── Services/
│   ├── TallyServiceTests.cs           # Must match v3 results exactly
│   ├── TieDetectionServiceTests.cs
│   ├── VoterMatchingServiceTests.cs
│   └── BallotValidationServiceTests.cs
```

**Integration Tests**:
```
backend.Tests/Integration/
├── AuthenticationTests.cs             # All 3 auth systems
├── BallotEntryTests.cs
├── TallyEndToEndTests.cs              # Complete election flow
└── SignalRTests.cs
```

**E2E Tests** (optional, Playwright):
```
e2e/
├── auth.spec.ts
├── election-setup.spec.ts
├── ballot-entry.spec.ts
├── online-voting.spec.ts
└── reports.spec.ts
```

### 5.2 Tally Algorithm Verification

**CRITICAL**: Tally results must be **identical** to v3.

**Verification Process**:
1. Export test data from v3 (ballots, votes, expected results)
2. Import into v4 database
3. Run v4 tally algorithm
4. Compare results field-by-field
5. **Zero tolerance for differences**

**Test Cases**:
- Normal election (9 seats, 30+ candidates)
- Tie scenarios (2-way, 3-way, across sections)
- Edge cases (no ballots, all spoiled, single candidate)
- Tie-break elections
- Large elections (1000+ ballots)

### 5.3 Linting & Type Checking

**Backend**:
```bash
dotnet format                  # C# formatting
dotnet build --no-incremental  # Type checking
```

**Frontend**:
```bash
npm run lint       # ESLint + vue-tslint
npm run type-check # Vue TSC
```

### 5.4 Deployment Verification

**Docker Compose Test**:
```bash
docker-compose up
# Verify:
# - Backend API responds
# - Frontend loads
# - Database migrations applied
# - SignalR connections work
# - OAuth redirects work
```

**Cross-Platform Test**:
- Windows 10/11
- Ubuntu 22.04 LTS
- macOS (Intel + Apple Silicon)

### 5.5 Performance Benchmarks

**Load Testing** (k6 or Artillery):
- 30 concurrent elections
- 200 concurrent tellers entering ballots
- 100 concurrent voters submitting online ballots
- Tally computation time (< 5 seconds for 500 ballots)
- SignalR message latency (< 100ms)

**Success Criteria**:
- API response time: p95 < 500ms
- Tally computation: Linear time (O(n) ballots)
- SignalR delivery: < 100ms latency
- Database queries: < 100ms for all reads

---

## 6. Dependencies

### 6.1 Backend NuGet Packages

```xml
<ItemGroup>
  <!-- Framework -->
  <PackageReference Include="Microsoft.AspNetCore.App" />
  
  <!-- Database -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.*" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" /> <!-- Optional -->
  
  <!-- Authentication -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="10.0.*" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.Twitter" Version="10.0.*" /> <!-- X.com -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.*" />
  
  <!-- 2FA -->
  <PackageReference Include="Otp.NET" Version="1.4.*" /> <!-- TOTP -->
  <PackageReference Include="QRCoder" Version="1.6.*" /> <!-- QR codes for 2FA setup -->
  
  <!-- SignalR -->
  <PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="10.0.*" />
  
  <!-- Caching -->
  <PackageReference Include="StackExchange.Redis" Version="2.8.*" />
  
  <!-- Notifications -->
  <PackageReference Include="Twilio" Version="7.5.*" />
  <PackageReference Include="MailKit" Version="4.8.*" />
  
  <!-- Reporting -->
  <PackageReference Include="QuestPDF" Version="2024.12.*" /> <!-- PDF generation -->
  <PackageReference Include="RazorLight" Version="2.3.*" /> <!-- Razor templates outside MVC -->
  
  <!-- CSV -->
  <PackageReference Include="CsvHelper" Version="33.*" />
  
  <!-- Logging -->
  <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
  
  <!-- i18n -->
  <PackageReference Include="Microsoft.Extensions.Localization" Version="10.0.*" />
</ItemGroup>
```

### 6.2 Frontend NPM Packages

```json
{
  "dependencies": {
    "vue": "^3.5.13",
    "vue-router": "^4.5.0",
    "pinia": "^2.3.0",
    
    "@microsoft/signalr": "^8.0.7",
    "axios": "^1.7.9",
    
    "element-plus": "^2.9.1",
    "@element-plus/icons-vue": "^2.3.1",
    
    "vue-i18n": "^10.0.5",
    
    "less": "^4.2.1",
    
    "highcharts": "^11.4.8",
    "highcharts-vue": "^2.0.1"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^5.2.1",
    "vite": "^6.0.5",
    
    "typescript": "^5.7.3",
    "vue-tsc": "^2.1.10",
    
    "@vue/eslint-config-typescript": "^14.1.3",
    "eslint": "^9.17.0",
    "eslint-plugin-vue": "^9.31.0",
    
    "@types/node": "^22.10.5"
  }
}
```

---

## 7. Risks & Mitigation

### 7.1 Tally Algorithm Accuracy

**Risk**: Ported tally algorithm produces different results than v3.

**Impact**: **CRITICAL** - Election results would be invalid.

**Mitigation**:
- Port algorithm line-by-line from v3 documentation
- Create comprehensive test suite with v3 exported data
- Manual verification with sample elections
- Side-by-side comparison tool (run both systems)

### 7.2 Multi-Auth Complexity

**Risk**: Managing 3 independent auth systems introduces bugs.

**Impact**: HIGH - Users unable to log in.

**Mitigation**:
- Separate auth services per type
- Comprehensive integration tests
- Clear documentation of each flow
- Staged rollout (admin → teller → voter)

### 7.3 SignalR Scalability

**Risk**: SignalR connections fail under load (200 concurrent users).

**Impact**: MEDIUM - Real-time updates stop working.

**Mitigation**:
- Use Redis backplane for scale-out
- Connection pooling
- Load testing before production
- Graceful degradation (polling fallback)

### 7.4 Self-Hosting Complexity

**Risk**: Non-technical users struggle to install/deploy.

**Impact**: MEDIUM - Limited adoption in some countries.

**Mitigation**:
- Docker Compose one-command setup
- Detailed installation guides
- Automated setup scripts (Windows, Linux)
- Video tutorials

### 7.5 Timeline Slippage

**Risk**: 1-2 month estimate is too optimistic.

**Impact**: LOW - This is a passion project, no hard deadline.

**Mitigation**:
- Phased delivery (core features first)
- Incremental releases (v4.0, v4.1, v4.2...)
- Parallel v3 operation (no rush to switch)
- AI assistance for boilerplate code

### 7.6 i18n Implementation

**Risk**: Retrofitting i18n is harder than building it in from day 1.

**Impact**: MEDIUM - Delays international adoption.

**Mitigation**:
- Set up i18n infrastructure in Phase 1
- Extract strings as you build features
- Use English keys initially, translate later
- Hire translators for initial 5 languages

---

## 8. Success Criteria

### 8.1 Feature Parity Checklist

**Core Features**:
- ✅ Admin authentication (Google, X.com, 2FA, password reset)
- ✅ Guest teller authentication (access codes)
- ✅ Voter authentication (email/SMS/WhatsApp codes)
- ✅ Election creation and setup (4-step wizard)
- ✅ Voter import (CSV)
- ✅ Front desk (voter check-in)
- ✅ Ballot entry (multi-teller)
- ✅ Tally computation (exact v3 algorithm)
- ✅ Results display (tie detection)
- ✅ Online voting portal
- ✅ Reports (teller report, results, audit log)
- ✅ Roll call display (simplified)
- ✅ Real-time updates (SignalR)

**New Features**:
- ✅ SMS cost estimation
- ✅ WhatsApp integration
- ✅ Linked elections (tie-break, two-stage)
- ✅ i18n support (5 languages)
- ✅ Dynamic text enlargement

**UI/UX**:
- ✅ All 26 screenshots replicated in Vue 3
- ✅ Improved navigation (less confusing than v3)
- ✅ Responsive design (mobile-friendly)
- ✅ Admin control indicators

### 8.2 Technical Quality

- ✅ .NET 10 (latest)
- ✅ TypeScript strict mode
- ✅ Unit test coverage > 80% (Services layer)
- ✅ Integration tests for critical flows
- ✅ Tally algorithm tests pass 100%
- ✅ ESLint/TSC pass with zero errors
- ✅ Docker Compose deployment works
- ✅ Cross-platform tested (Windows, Linux, macOS)

### 8.3 Performance

- ✅ API response time p95 < 500ms
- ✅ Tally computation < 5 seconds (500 ballots)
- ✅ Supports 30 concurrent elections
- ✅ Supports 200 concurrent tellers
- ✅ SignalR latency < 100ms

### 8.4 Documentation

- ✅ Installation guide (Docker, manual)
- ✅ API documentation (Swagger/OpenAPI)
- ✅ User guide (election admin workflow)
- ✅ Developer guide (contributing, architecture)
- ✅ Deployment guide (Azure, AWS, self-hosted)

---

## 9. Delivery Phases

### Phase 1: Foundation (Weeks 1-2)
**Goal**: Runnable backend + frontend skeleton

**Deliverables**:
- Upgrade to .NET 10
- Database schema complete (16 entities + new tables)
- Admin authentication (Google, X.com, 2FA)
- Basic election CRUD
- Vue 3 app with routing, auth pages, Element Plus
- i18n infrastructure

**Verification**:
- Can create user account
- Can log in with Google/X.com
- Can enable 2FA
- Can create/edit/delete election
- Frontend shows translated UI

---

### Phase 2: Ballot Entry & Tally (Weeks 3-4)
**Goal**: Core election functionality

**Deliverables**:
- Guest teller authentication
- Ballot entry UI + API
- Tally algorithm (ported from v3)
- Tie detection
- Basic results display
- FrontDeskHub, BallotHub SignalR

**Verification**:
- Teller can join with access code
- Can enter ballots (multi-teller concurrent)
- Tally produces identical results to v3
- Tie detection works correctly
- Real-time progress updates work

---

### Phase 3: Front Desk & Voter Management (Weeks 5-6)
**Goal**: In-person voting workflow

**Deliverables**:
- Voter import (CSV)
- Front desk UI (check-in voters)
- Voter matching logic (email/phone → Person)
- FrontDeskHub real-time updates
- Roll call display (simplified)

**Verification**:
- Can import 100+ voters from CSV
- Can check in voters at front desk
- Roll call display updates in real-time
- Multiple tellers see synchronized state

---

### Phase 4: Online Voting (Weeks 7-8)
**Goal**: Voter self-service portal

**Deliverables**:
- Voter authentication (email/SMS/WhatsApp codes)
- Online voting portal UI
- Ballot submission API
- VoterHub SignalR
- Twilio integration (SMS)
- WhatsApp integration (new feature)

**Verification**:
- Voter receives code via email/SMS/WhatsApp
- Can verify code and access ballot
- Can submit ballot online
- Online ballots appear in tally

---

### Phase 5: Reporting & Notifications (Week 9)
**Goal**: Complete admin workflow

**Deliverables**:
- Report templates (Razor)
- PDF generation (QuestPDF)
- SMS cost estimation (new feature)
- Notification sending (email/SMS/WhatsApp)
- Notification preview UI

**Verification**:
- Can generate teller report (HTML + PDF)
- Can estimate SMS costs before sending
- Can send notifications to all voters
- Reports display correctly with dynamic enlargement

---

### Phase 6: Linked Elections (Week 10)
**Goal**: New features

**Deliverables**:
- Linked election creation (tie-break, two-stage)
- Tie-break workflow UI
- Parent/child election navigation

**Verification**:
- Can create tie-break election from results page
- Tied candidates automatically copied
- Tie-break result updates parent election

---

### Phase 7: Polish & Testing (Week 11)
**Goal**: Production-ready quality

**Deliverables**:
- Comprehensive testing (unit, integration, E2E)
- Performance optimization
- UI polish (accessibility, responsive)
- Error handling improvements

**Verification**:
- All tests pass
- Performance benchmarks met
- Accessibility audit passes
- Works on mobile devices

---

### Phase 8: Deployment & Documentation (Week 12)
**Goal**: Deployable system

**Deliverables**:
- Docker Compose setup
- Installation guides
- User documentation
- API documentation (Swagger)
- Deployment to staging environment

**Verification**:
- One-command Docker setup works
- Can deploy to Azure VM
- Documentation is clear and complete
- Staging environment accessible

---

### Phase 9: Parallel Operation (Ongoing)
**Goal**: Gradual migration from v3

**Deliverables**:
- v4 running in production (separate domain)
- User feedback collection
- Bug fixes
- Feature tweaks

**Verification**:
- First real election runs successfully
- Users can complete entire workflow
- Results verified against v3 (if parallel test)

---

## 10. Next Steps

### 10.1 Phase 1 Start Guide

**Goal**: Upgrade to .NET 10, install dependencies, set up auth infrastructure

---

#### Step 1: Upgrade to .NET 10

```bash
cd C:\Users\glenl\.zenflow\worktrees\review-design-b867
```

**Update `global.json`**:
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

**Update `backend/TallyJ4.csproj`**:
Change `<TargetFramework>net9.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>`

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

Verify backend starts on http://localhost:5000

---

#### Step 2: Install Backend Dependencies

```bash
cd backend
dotnet add package Microsoft.AspNetCore.Authentication.Google
dotnet add package Microsoft.AspNetCore.Authentication.Twitter
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Otp.NET
dotnet add package QRCoder
dotnet add package StackExchange.Redis
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
dotnet add package Twilio
dotnet add package MailKit
dotnet add package QuestPDF
dotnet add package RazorLight
dotnet add package CsvHelper
dotnet build
```

---

#### Step 3: Install Frontend Dependencies

```bash
cd ../frontend
npm install element-plus @element-plus/icons-vue
npm install vue-i18n
npm install less --save-dev
npm install @microsoft/signalr
npm install axios
npm install highcharts highcharts-vue
npm run dev
```

Verify frontend starts on http://localhost:5173

---

#### Step 4: Register Google OAuth App

1. Go to https://console.cloud.google.com/
2. Create new project: "TallyJ4"
3. Enable Google+ API (or Identity Platform)
4. Go to **Credentials** → **Create Credentials** → **OAuth 2.0 Client ID**
5. Application type: **Web application**
6. Authorized redirect URIs:
   - `http://localhost:5000/signin-google` (dev)
   - `https://tallyj4.yourdomain.com/signin-google` (production)
7. Copy **Client ID** and **Client Secret**

**Add to `backend/appsettings.Development.json`**:
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

---

#### Step 5: Set Up Redis Cloud (Free Tier)

1. Go to https://redis.com/try-free/
2. Sign up for free account
3. Create database:
   - Cloud: AWS
   - Region: Closest to you
   - Type: Redis Stack (free)
4. Copy connection string (format: `redis://default:password@host:port`)

**Add to `backend/appsettings.Development.json`**:
```json
{
  "Redis": {
    "ConnectionString": "YOUR_REDIS_CONNECTION_STRING"
  }
}
```

---

#### Step 6: Configure Email (Google Workspace SMTP)

**Add to `backend/appsettings.Development.json`**:
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@yourdomain.com",
    "SmtpPassword": "YOUR_APP_PASSWORD",
    "FromAddress": "noreply@yourdomain.com",
    "FromName": "TallyJ Elections"
  }
}
```

**Note**: Generate App Password at https://myaccount.google.com/apppasswords (not your regular password)

---

#### Step 7: Configure Twilio

**Add to `backend/appsettings.Development.json`**:
```json
{
  "Twilio": {
    "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
    "AuthToken": "YOUR_TWILIO_AUTH_TOKEN",
    "FromPhoneNumber": "+1234567890",
    "WhatsAppFromNumber": "whatsapp:+1234567890"
  }
}
```

---

#### Step 8: Configure IFTTT Logging (Optional)

**Add to `backend/appsettings.Development.json`**:
```json
{
  "Logging": {
    "IFTTT": {
      "WebhookKey": "YOUR_IFTTT_WEBHOOK_KEY",
      "EventName": "tallyj4_log"
    }
  }
}
```

---

#### Step 9: Set Up i18n in Frontend (Lazy Loading)

Create `frontend/src/locales/en.json`:
```json
{
  "app": {
    "title": "TallyJ Elections"
  },
  "auth": {
    "login": "Log In",
    "register": "Register",
    "loginWithGoogle": "Log in with Google",
    "username": "Username",
    "password": "Password"
  }
}
```

Create `frontend/src/locales/es.json`:
```json
{
  "app": {
    "title": "TallyJ Elecciones"
  },
  "auth": {
    "login": "Iniciar sesión",
    "register": "Registrarse",
    "loginWithGoogle": "Iniciar sesión con Google",
    "username": "Nombre de usuario",
    "password": "Contraseña"
  }
}
```

**Create `frontend/src/i18n.ts`** (lazy loading with localStorage persistence):
```typescript
import { createI18n } from 'vue-i18n'

export const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('locale') || 'en',
  fallbackLocale: 'en',
  messages: {}
})

const loadedLanguages = new Set<string>()

export async function loadLanguage(lang: string) {
  if (loadedLanguages.has(lang)) {
    i18n.global.locale.value = lang
    localStorage.setItem('locale', lang)
    return
  }

  try {
    const messages = await import(`./locales/${lang}.json`)
    i18n.global.setLocaleMessage(lang, messages.default)
    loadedLanguages.add(lang)
    i18n.global.locale.value = lang
    localStorage.setItem('locale', lang)
    document.documentElement.dir = lang === 'fa' ? 'rtl' : 'ltr' // RTL for Persian
  } catch (error) {
    console.error(`Failed to load language: ${lang}`, error)
  }
}

// Load default language
loadLanguage(i18n.global.locale.value)

export default i18n
```

**Update `frontend/src/main.ts`**:
```typescript
import { createApp } from 'vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import App from './App.vue'
import router from './router'
import { createPinia } from 'pinia'
import i18n from './i18n' // Import configured i18n

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(ElementPlus)
app.use(i18n)
app.mount('#app')
```

**Benefits**:
- Only loads English initially (~10KB)
- Other languages loaded on-demand when user switches
- Language choice persisted in localStorage
- Automatic RTL support for Persian

---

#### Step 10: Verify Setup

```bash
# Backend
cd backend
dotnet build
dotnet run

# Frontend (new terminal)
cd frontend
npm run dev
```

**Checklist**:
- ✅ Backend runs on .NET 10
- ✅ Frontend shows Element Plus components
- ✅ No build errors
- ✅ appsettings.Development.json has all credentials

---

**Next**: Start implementing admin authentication (local accounts + Google OAuth)

### 10.2 Infrastructure Decisions

**✅ Decisions Made**:

1. **OAuth Apps**: Register new Google OAuth app during Phase 1. Also support username/password local accounts (users choose OAuth OR local account).

2. **Redis**: Redis Cloud free tier for SignalR backplane and caching.

3. **Email Service**: SMTP via Google Workspace (existing account).

4. **Twilio**: Existing Twilio account ready with credits.

5. **Error Tracking**: 
   - Local database logging (existing pattern)
   - IFTTT webhook → Google Sheets (existing pattern)
   - Optional: Add Sentry for production errors

6. **CI/CD**: GitHub Actions for automated testing (free for public repos).

7. **Database**: SQL Server Express (already installed on dev machine).

---

## 11. Appendix

### 11.1 Reference Documentation

All reverse engineering documentation is located in:
`.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`

**Key Files**:
- `database/entities.md` - Complete database schema
- `security/authentication.md` - All 3 auth systems (12,000+ lines)
- `business-logic/tally-algorithms.md` - Tally implementation details
- `signalr/hubs-overview.md` - SignalR architecture
- `ui-screenshots-analysis.md` - UI/UX reference
- `api/endpoints.md` - v3 API catalog

### 11.2 Glossary

- **Teller**: Election worker who enters ballots
- **Guest Teller**: Temporary teller (access code only, no account)
- **LSA**: Local Spiritual Assembly (9-member elected body)
- **Ballot**: Physical or digital ballot with multiple votes
- **Vote**: Single candidate selection on a ballot
- **Tally**: Process of counting votes to determine results
- **Tie**: Two or more candidates with equal vote counts
- **Tie-Break Election**: Secondary election to resolve a tie
- **Two-Stage Election**: Primary election → Secondary election
- **Roll Call**: Public display of voters who have voted
- **Front Desk**: In-person voter check-in station
- **ElectionPasscode**: Access code for guest tellers

### 11.3 i18n Usage Examples

**Language Switcher Component**:
```vue
<!-- components/common/LanguageSelector.vue -->
<script setup lang="ts">
import { loadLanguage } from '@/i18n'
import { useI18n } from 'vue-i18n'

const { locale } = useI18n()

const languages = [
  { code: 'en', name: 'English' },
  { code: 'es', name: 'Español' },
  { code: 'fa', name: 'فارسی' },
  { code: 'fr', name: 'Français' },
  { code: 'pt', name: 'Português' }
]

async function switchLanguage(lang: string) {
  await loadLanguage(lang)
}
</script>

<template>
  <el-dropdown @command="switchLanguage">
    <el-button>
      {{ languages.find(l => l.code === locale)?.name }}
      <el-icon class="el-icon--right"><arrow-down /></el-icon>
    </el-button>
    <template #dropdown>
      <el-dropdown-menu>
        <el-dropdown-item 
          v-for="lang in languages" 
          :key="lang.code"
          :command="lang.code"
          :disabled="locale === lang.code"
        >
          {{ lang.name }}
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>
```

**Using Translations in Components**:
```vue
<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <h1>{{ t('elections.createNew') }}</h1>
  <el-form>
    <el-form-item :label="t('elections.electionName')">
      <el-input :placeholder="t('elections.electionName')" />
    </el-form-item>
  </el-form>
</template>
```

**Pluralization**:
```json
// en.json
{
  "voters": {
    "count": "no voters | 1 voter | {count} voters"
  }
}
```

```vue
<template>
  <p>{{ t('voters.count', voterCount) }}</p>
</template>
```

**Composable Helper** (optional):
```typescript
// composables/useLanguage.ts
import { loadLanguage } from '@/i18n'
import { useI18n } from 'vue-i18n'

export function useLanguage() {
  const { locale, t } = useI18n()
  
  const availableLanguages = [
    { code: 'en', name: 'English', nativeName: 'English' },
    { code: 'es', name: 'Spanish', nativeName: 'Español' },
    { code: 'fa', name: 'Persian', nativeName: 'فارسی' },
    { code: 'fr', name: 'French', nativeName: 'Français' },
    { code: 'pt', name: 'Portuguese', nativeName: 'Português' }
  ]
  
  async function setLanguage(lang: string) {
    await loadLanguage(lang)
  }
  
  const currentLanguage = computed(() => 
    availableLanguages.find(l => l.code === locale.value)
  )
  
  const isRTL = computed(() => locale.value === 'fa')
  
  return {
    locale,
    t,
    availableLanguages,
    currentLanguage,
    setLanguage,
    isRTL
  }
}
```

### 11.4 Technology References

- [.NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [Vue 3 Documentation](https://vuejs.org/)
- [Element Plus Documentation](https://element-plus.org/)
- [SignalR Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [EF Core 10 Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Twilio API Documentation](https://www.twilio.com/docs)
- [vue-i18n Documentation](https://vue-i18n.intlify.dev/)
- [vue-i18n Lazy Loading](https://vue-i18n.intlify.dev/guide/advanced/lazy.html)

---

## Summary

This specification defines a complete rebuild of TallyJ from ASP.NET Framework 4.8 to .NET 10 + Vue 3, maintaining full feature parity while adding modernization (OAuth, 2FA, i18n) and new features (SMS cost estimation, WhatsApp, linked elections).

**Complexity**: HARD (multi-auth, critical tally algorithm, real-time collaboration, large codebase)

**Timeline**: 12 weeks (phased delivery)

**Priority**: Ballot entry → Tally → Front desk → Online voting → Reports → New features

**Success Metrics**: Feature parity, tally accuracy (100% match with v3), cross-platform deployment, self-hosting simplicity

This spec is ready for implementation. Phase 1 can begin immediately after answering the 7 questions in section 10.2.
