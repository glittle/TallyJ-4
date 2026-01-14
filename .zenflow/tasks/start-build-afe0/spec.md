# Technical Specification: Phase 1 - Foundation Setup

## Task Complexity Assessment

**Complexity Level**: **Hard**

**Rationale**:
- Major architectural restructuring (1 → 3 backend projects)
- Multi-layered changes across frontend and backend
- Database schema design with 16+ entities
- Complex authentication system with multiple flows (OAuth, local, 2FA)
- i18n infrastructure requiring coordination across stack
- High risk of breaking existing functionality during restructure

---

## Technical Context

### Backend
- **Framework**: .NET 10 (LTS)
- **Language**: C# 12 with nullable reference types
- **Current Structure**: Single monolithic project (`TallyJ4.csproj`)
- **Target Structure**: 3-project architecture (Domain, Application, Web)
- **Database**: SQL Server Express (dev) / SQL Server (prod) via EF Core 10
- **Authentication**: ASP.NET Core Identity + JWT, Google OAuth, TOTP 2FA
- **Real-time**: ASP.NET Core SignalR (no Redis)

### Frontend
- **Framework**: Vue 3 (Composition API with `<script setup>`)
- **Language**: TypeScript (strict mode)
- **Build Tool**: Vite
- **UI Library**: Element Plus
- **Styling**: LESS (top-level class pattern, NOT scoped)
- **State Management**: Pinia
- **i18n**: vue-i18n with lazy loading
- **HTTP Client**: axios (existing, needs verification)

### External Dependencies to Add
**Backend**:
- `Microsoft.AspNetCore.Authentication.Google` (Google OAuth)
- `Otp.NET` (TOTP 2FA)
- `QRCoder` (QR code generation for 2FA)
- `CsvHelper` (voter import, future phase)
- `MailKit` (email verification codes)
- `Twilio` (SMS verification codes)
- `Microsoft.Extensions.Localization` (backend i18n)

**Frontend**:
- `vue-i18n` (internationalization)
- `@intlify/unplugin-vue-i18n` (Vite plugin for i18n)
- `less` (LESS preprocessor)

---

## Implementation Approach

### Overview
Phase 1 establishes the architectural foundation for TallyJ4 by:
1. Restructuring the backend into a clean 3-layer architecture
2. Setting up i18n infrastructure (English + French)
3. Finalizing the database schema with all required entities
4. Implementing the complete admin authentication system

This phase focuses on **infrastructure and architecture** rather than user-facing features. The deliverable is a solid foundation that subsequent phases will build upon.

### Constraints & Design Decisions (from design-decisions.md)
- **No Tailwind CSS**: Use LESS with top-level class pattern
- **No Vue scoped styles**: Use component-scoped classes instead
- **No Redis**: Use native .NET SignalR
- **No PostgreSQL for v4.0**: SQL Server only
- **No WhatsApp/X.com OAuth**: Deferred to v4.1+
- **Database Keys**: IDENTITY PKs, GUID FKs (EF generates sequential GUIDs)
- **v3 Migration Note**: Ignore v3 "compressed" columns, use clean model properties

---

## Source Code Structure Changes

### Backend Restructure (Task 1.1)

#### New Project Structure
```
backend/
├── TallyJ4.Domain/           # NEW
│   ├── Entities/
│   │   ├── Election.cs
│   │   ├── Person.cs
│   │   ├── Ballot.cs
│   │   ├── Vote.cs
│   │   ├── Teller.cs
│   │   ├── Location.cs
│   │   ├── Result.cs
│   │   ├── ResultSummary.cs
│   │   ├── ResultTie.cs
│   │   ├── OnlineVoter.cs
│   │   ├── OnlineVotingInfo.cs
│   │   ├── ImportFile.cs
│   │   ├── JoinElectionUser.cs
│   │   ├── Message.cs
│   │   ├── SmsLog.cs
│   │   ├── Log.cs
│   │   └── TwoFactorToken.cs      # NEW entity
│   ├── Enums/
│   │   ├── ElectionType.cs
│   │   ├── ElectionMode.cs
│   │   ├── TallyStatus.cs
│   │   ├── VotingMethod.cs
│   │   └── OnlineSelectionProcess.cs
│   └── Interfaces/
│       ├── IEntity.cs             # Base interface with RowId, Guid, RowVersion
│       └── IElectionScoped.cs     # Entities tied to an election
│
├── TallyJ4.Application/       # NEW
│   ├── Services/
│   │   ├── Auth/
│   │   │   ├── GoogleAuthService.cs
│   │   │   ├── LocalAuthService.cs
│   │   │   ├── JwtTokenService.cs
│   │   │   ├── TwoFactorService.cs
│   │   │   └── PasswordResetService.cs
│   │   ├── Elections/
│   │   │   └── ElectionService.cs    # Placeholder for Phase 2
│   │   └── Email/
│   │       └── EmailService.cs       # MailKit wrapper
│   ├── DTOs/
│   │   ├── Auth/
│   │   │   ├── LoginRequest.cs
│   │   │   ├── RegisterRequest.cs
│   │   │   ├── TokenResponse.cs
│   │   │   ├── Enable2FARequest.cs
│   │   │   ├── Verify2FARequest.cs
│   │   │   └── PasswordResetRequest.cs
│   │   └── Elections/
│   │       └── (placeholders for Phase 2)
│   └── Validators/
│       └── Auth/
│           ├── LoginRequestValidator.cs
│           └── RegisterRequestValidator.cs
│
└── TallyJ4.Web/               # RENAMED from TallyJ4 (root backend folder)
    ├── Controllers/
    │   ├── AuthController.cs       # NEW - admin auth endpoints
    │   ├── ElectionsController.cs  # KEEP (existing)
    │   ├── BallotsController.cs    # KEEP (existing)
    │   ├── PeopleController.cs     # KEEP (existing)
    │   └── VotesController.cs      # KEEP (existing)
    ├── Hubs/
    │   ├── PublicHub.cs            # NEW (placeholder)
    │   ├── VoterHub.cs             # NEW (placeholder)
    │   ├── TellerHub.cs            # NEW (placeholder)
    │   └── AdminHub.cs             # NEW (placeholder)
    ├── EF/
    │   ├── Context/
    │   │   └── MainDbContext.cs    # UPDATED (reference Domain entities)
    │   ├── Data/
    │   │   └── DbSeeder.cs         # UPDATED
    │   ├── Identity/
    │   │   └── AppUser.cs          # UPDATED (add 2FA fields)
    │   └── Migrations/
    │       └── (regenerate all)
    ├── Middleware/
    │   └── (existing Serilog/correlation ID middleware)
    ├── Resources/                  # NEW - i18n resource files
    │   ├── ErrorMessages.en.resx
    │   └── ErrorMessages.fr.resx
    ├── Helpers/                    # KEEP (existing)
    ├── Properties/                 # KEEP (existing)
    ├── Program.cs                  # UPDATED (DI, middleware, SignalR)
    ├── appsettings.json            # UPDATED (Google OAuth, JWT, email, SMS)
    ├── appsettings.Development.json
    └── TallyJ4.Web.csproj          # RENAMED + updated references
```

#### File Migration Plan
1. **Move**: `backend/EF/Models/*.cs` → `TallyJ4.Domain/Entities/`
2. **Update**: Change namespace from `TallyJ4.EF.Models` → `TallyJ4.Domain.Entities`
3. **Keep**: `backend/EF/Context/MainDbContext.cs` in Web project (EF lives with API)
4. **Keep**: `backend/EF/Identity/AppUser.cs` in Web project (Identity integration)
5. **Keep**: All existing Controllers, Helpers, middleware in Web project

---

### Frontend Structure Changes (Task 1.2)

```
frontend/src/
├── locales/                        # NEW
│   ├── en.json                     # English translations
│   ├── fr.json                     # French translations
│   └── index.ts                    # i18n config + lazy loading
├── components/                     # NEW
│   └── common/
│       └── LanguageSelector.vue    # NEW - language switcher
├── pages/                          # EXISTING
├── router/                         # EXISTING
├── stores/                         # NEW (Pinia)
│   └── auth.ts                     # Placeholder for Phase 1.4
├── services/                       # NEW
│   └── api/
│       └── auth.ts                 # Auth API client (axios)
├── types/                          # NEW
│   └── auth.ts                     # TypeScript interfaces for auth
├── App.vue                         # UPDATED (add LanguageSelector)
├── main.ts                         # UPDATED (register i18n plugin)
└── style.css                       # UPDATED (global styles using LESS)
```

---

## Data Model Changes

### New Entity: TwoFactorToken (Task 1.3)

```csharp
namespace TallyJ4.Domain.Entities;

[Table("TwoFactorToken")]
public class TwoFactorToken
{
    [Key]
    public int RowId { get; set; }
    
    public Guid TokenGuid { get; set; }
    
    [Required]
    public string UserId { get; set; } = null!;  // FK to AspNetUsers
    
    [Required]
    [StringLength(200)]
    public string Secret { get; set; } = null!;  // Encrypted TOTP secret
    
    public bool IsEnabled { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? VerifiedAt { get; set; }
    
    [Column("_RowVersion")]
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
```

### Updated Entity: AppUser (Task 1.4)

Add fields to `backend/EF/Identity/AppUser.cs`:

```csharp
public class AppUser : IdentityUser
{
    // ... existing fields ...
    
    // NEW: Google OAuth support
    public string? GoogleId { get; set; }
    
    // NEW: Track auth method
    [Required]
    [StringLength(20)]
    public string AuthMethod { get; set; } = "Local";  // "Local" | "Google"
    
    // NEW: Password reset token (local accounts only)
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    
    // NEW: 2FA
    public bool TwoFactorEnabled { get; set; }
    
    // Navigation to TwoFactorToken
    public virtual TwoFactorToken? TwoFactorToken { get; set; }
}
```

### Database Migration Strategy

1. **Drop existing database** (safe for early development)
2. **Regenerate migrations** after entity restructure
3. **Seed admin account** via `DbSeeder.cs`:
   - Email: `admin@tallyj.local`
   - Password: `Admin123!`
   - 2FA: Disabled initially

---

## API Changes

### New AuthController Endpoints (Task 1.4)

Base route: `/api/auth`

#### Local Authentication
- `POST /register` - Create local account
  - Body: `{ email, password, confirmPassword, firstName, lastName }`
  - Returns: `201 Created` + user info (no token until email verified)
  
- `POST /login` - Local login
  - Body: `{ email, password, twoFactorCode? }`
  - Returns: `200 OK` + JWT token OR `202 Accepted` + `{ requires2FA: true }`
  
- `POST /logout` - Invalidate token (optional, JWT stateless for v4.0)
  - Returns: `204 No Content`

#### Google OAuth
- `GET /google-login` - Redirect to Google OAuth consent
  - Query: `?returnUrl=/dashboard`
  - Returns: `302 Redirect` to Google
  
- `GET /google-callback` - Handle Google OAuth callback
  - Query: `?code=...&state=...`
  - Returns: `302 Redirect` to frontend with token in URL fragment

#### 2FA
- `POST /2fa/setup` - Generate TOTP secret + QR code
  - Headers: `Authorization: Bearer <token>`
  - Returns: `{ secret, qrCodeDataUrl }`
  
- `POST /2fa/enable` - Verify and enable 2FA
  - Body: `{ code }`
  - Returns: `{ backupCodes: string[] }`
  
- `POST /2fa/disable` - Disable 2FA
  - Body: `{ password, code }`
  - Returns: `204 No Content`

#### Password Reset
- `POST /password/forgot` - Request reset email
  - Body: `{ email }`
  - Returns: `204 No Content` (always, no user enumeration)
  
- `POST /password/reset` - Reset with token
  - Body: `{ token, newPassword, confirmPassword }`
  - Returns: `200 OK`

### JWT Token Structure

```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "role": "Admin",
  "authMethod": "Google",
  "electionGuid": null,
  "exp": 1234567890,
  "iat": 1234567890
}
```

**Claims**:
- `sub`: User GUID
- `email`: User email
- `role`: `Admin` | `GuestTeller` | `Voter`
- `authMethod`: `Local` | `Google` | `AccessCode` | `VoterCode`
- `electionGuid`: For guest tellers and voters (null for admins)
- `exp`: Token expiration (15 minutes for v4.0)
- `iat`: Issued at

---

## Configuration Changes

### Backend: appsettings.json

Add new sections:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TallyJ4;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Secret": "GENERATE-LONG-RANDOM-SECRET-AT-RUNTIME",
    "Issuer": "TallyJ4",
    "Audience": "TallyJ4-Client",
    "ExpiryMinutes": 15
  },
  "Google": {
    "ClientId": "YOUR-GOOGLE-CLIENT-ID",
    "ClientSecret": "YOUR-GOOGLE-CLIENT-SECRET"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "Username": "",
    "Password": "",
    "FromAddress": "noreply@tallyj.com",
    "FromName": "TallyJ"
  },
  "Twilio": {
    "AccountSid": "",
    "AuthToken": "",
    "FromPhoneNumber": ""
  },
  "Serilog": {
    "...existing..."
  }
}
```

### Frontend: i18n Configuration

**locales/en.json**:
```json
{
  "common": {
    "language": "Language",
    "english": "English",
    "french": "Français",
    "loading": "Loading...",
    "error": "An error occurred"
  },
  "auth": {
    "login": "Login",
    "register": "Register",
    "logout": "Logout",
    "email": "Email",
    "password": "Password",
    "forgotPassword": "Forgot Password?",
    "googleLogin": "Sign in with Google",
    "enable2FA": "Enable Two-Factor Authentication",
    "verify2FA": "Verify Code",
    "errors": {
      "invalidCredentials": "Invalid email or password",
      "emailRequired": "Email is required",
      "passwordRequired": "Password is required"
    }
  }
}
```

**locales/fr.json**:
```json
{
  "common": {
    "language": "Langue",
    "english": "English",
    "french": "Français",
    "loading": "Chargement...",
    "error": "Une erreur s'est produite"
  },
  "auth": {
    "login": "Connexion",
    "register": "S'inscrire",
    "logout": "Déconnexion",
    "email": "Courriel",
    "password": "Mot de passe",
    "forgotPassword": "Mot de passe oublié?",
    "googleLogin": "Se connecter avec Google",
    "enable2FA": "Activer l'authentification à deux facteurs",
    "verify2FA": "Vérifier le code",
    "errors": {
      "invalidCredentials": "Courriel ou mot de passe invalide",
      "emailRequired": "Le courriel est requis",
      "passwordRequired": "Le mot de passe est requis"
    }
  }
}
```

---

## Verification Approach

### Phase 1 Success Criteria

#### Task 1.1: Backend Restructure
- [ ] Solution builds successfully with 3 projects
- [ ] All existing controllers compile with updated namespaces
- [ ] No circular dependencies between projects
- [ ] Project references: Web → Application → Domain (one-way)
- [ ] Run: `dotnet build` in solution root

#### Task 1.2: i18n Infrastructure
**Frontend**:
- [ ] Language selector component renders in top-right corner
- [ ] Toggle between English/French updates all UI text
- [ ] Browser localStorage persists language preference
- [ ] Page reload maintains selected language
- [ ] Manual test: Click language selector, verify translations

**Backend**:
- [ ] API returns localized error messages based on `Accept-Language` header
- [ ] Test: `curl -H "Accept-Language: fr" http://localhost:5000/api/auth/login`

#### Task 1.3: Database Schema Finalization
- [ ] All 16 entities present in Domain project
- [ ] `TwoFactorToken` entity created
- [ ] EF migrations generated without errors
- [ ] Database created successfully: `dotnet ef database update`
- [ ] Seed data creates admin account
- [ ] Query: `SELECT * FROM AspNetUsers WHERE Email = 'admin@tallyj.local'`

#### Task 1.4: Admin Authentication
- [ ] Local registration creates user in database
- [ ] Local login returns valid JWT token
- [ ] JWT token includes correct claims (sub, email, role)
- [ ] Google OAuth redirects to consent screen
- [ ] Google callback creates/updates user and returns token
- [ ] 2FA setup generates QR code
- [ ] 2FA verification succeeds with valid TOTP code
- [ ] Password reset email sent (check logs/mailtrap)
- [ ] Password reset with token succeeds
- [ ] Protected endpoint rejects invalid/expired tokens

### Testing Commands

**Backend**:
```bash
# Build solution
dotnet build

# Run migrations
cd backend/TallyJ4.Web
dotnet ef database update

# Run backend
dotnet run

# Check Swagger UI
# Navigate to: http://localhost:5000/swagger
```

**Frontend**:
```bash
# Install dependencies
npm install

# Run dev server
npm run dev

# Build production
npm run build

# TypeScript check
npx vue-tsc --noEmit
```

### Manual Testing Checklist

1. **Local Auth Flow**:
   - Register new account → verify 201 response
   - Login → verify JWT in response
   - Decode JWT at jwt.io → verify claims
   - Call protected endpoint with token → verify 200 response
   - Call protected endpoint without token → verify 401 response

2. **Google OAuth Flow**:
   - Click "Sign in with Google" → verify redirect to Google
   - Approve consent → verify callback redirects to frontend
   - Check database → verify user created with `AuthMethod = "Google"`

3. **2FA Flow**:
   - Setup 2FA → verify QR code displayed
   - Scan with authenticator app (Google Authenticator, Authy)
   - Enter code → verify 2FA enabled
   - Logout and login → verify prompted for 2FA code
   - Enter invalid code → verify error message
   - Enter valid code → verify successful login

4. **i18n**:
   - Open app → verify default language (English)
   - Click language selector → switch to French
   - Verify all text translated
   - Reload page → verify French persisted
   - Switch back to English → verify translations update

---

## Dependencies & Risks

### Critical Dependencies
- **External**: Google OAuth credentials (must create in Google Cloud Console)
- **External**: Email SMTP server (Gmail or Mailtrap for testing)
- **External**: Twilio account (SMS, deferred to Phase 5)
- **Internal**: SQL Server Express installed

### Known Risks

#### Risk 1: Breaking Existing Endpoints During Restructure
**Impact**: High - existing controllers may fail to compile  
**Mitigation**:
- Create new projects FIRST without modifying existing code
- Copy files instead of moving initially
- Verify build after each step
- Keep Git history clean with atomic commits

#### Risk 2: EF Migration Conflicts
**Impact**: Medium - database may fail to update  
**Mitigation**:
- Drop database and regenerate migrations from scratch (safe in early dev)
- Don't attempt incremental migrations during restructure
- Verify model snapshot matches entities

#### Risk 3: Google OAuth Configuration Complexity
**Impact**: Medium - OAuth may fail silently  
**Mitigation**:
- Use Swagger UI to test redirect URLs
- Enable verbose logging for authentication errors
- Test with multiple Google accounts (personal, workspace)

#### Risk 4: vue-i18n Lazy Loading Issues
**Impact**: Low - translations may not load  
**Mitigation**:
- Use dynamic imports: `import('locales/en.json')`
- Test with browser network throttling
- Add fallback to inline messages if load fails

---

## Open Questions for User

1. **Google OAuth Credentials**: Do you have a Google Cloud project set up, or should we defer Google OAuth testing until you provide credentials?

2. **Email Service**: Should we use Mailtrap (free dev SMTP) for testing password reset emails, or do you have an existing SMTP server?

3. **Admin Seed Account**: Is `admin@tallyj.local` / `Admin123!` acceptable for the default admin account, or do you prefer different credentials?

4. **Database Drop**: Is it acceptable to drop and recreate the database during Phase 1, or do you have existing data that must be preserved?

5. **2FA Testing**: Do you have a TOTP authenticator app (Google Authenticator, Authy) for testing 2FA, or should we add a "show secret key" option for manual entry?

---

## Implementation Plan

Given the complexity, Phase 1 will be broken down into **12 incremental tasks**:

### Task 1.1: Backend Restructure (4 subtasks)
1. Create Domain and Application projects, set up project references
2. Move entity models from Web to Domain, update namespaces
3. Update MainDbContext and existing controllers to reference Domain entities
4. Verify build and fix compilation errors

### Task 1.2: i18n Infrastructure (2 subtasks)
5. Frontend: Install vue-i18n, create locale files, implement LanguageSelector
6. Backend: Configure localization, create resource files for error messages

### Task 1.3: Database Schema Finalization (2 subtasks)
7. Create TwoFactorToken entity and update AppUser
8. Regenerate EF migrations, update database, verify seed data

### Task 1.4: Admin Authentication (4 subtasks)
9. Install auth packages, configure JWT and Google OAuth in appsettings.json
10. Implement local authentication (register, login, password reset)
11. Implement Google OAuth flow (redirect, callback)
12. Implement 2FA (setup, verify, disable)

Each subtask represents a **testable milestone** with clear verification steps.

---

## Estimated Effort

- **Task 1.1**: 4-6 hours (high risk of namespace issues)
- **Task 1.2**: 2-3 hours (straightforward)
- **Task 1.3**: 2-3 hours (assuming database drop is acceptable)
- **Task 1.4**: 6-8 hours (complex auth flows, external dependencies)

**Total**: 14-20 hours

**Timeline**: 3-4 days for solo developer with AI assistance

---

## Next Steps After Phase 1

Once Phase 1 is complete, Phase 2 will focus on:
- Election CRUD operations (Create, Read, Update, Delete)
- Guest teller authentication (access codes)
- Basic election setup wizard
- SignalR hub infrastructure (4 hubs with placeholder methods)

Phase 1 deliverables become the foundation for all subsequent phases.
