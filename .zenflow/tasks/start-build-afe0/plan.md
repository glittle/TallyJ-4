# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: e7e275ae-d676-4ec2-97c0-fd1a8c4777dc -->

**Complexity Assessment**: Hard

Technical specification created in `spec.md` covering:
- Backend restructure (1 → 3 projects)
- i18n infrastructure (frontend + backend)
- Database schema finalization (16+ entities)
- Admin authentication (Google OAuth, local, 2FA)

---

## Implementation Tasks

### Task 1.1: Backend Restructure

#### [x] Task 1.1.1: Create Domain and Application Projects

**Objective**: Set up new project structure without breaking existing code

**Steps**:
1. Create `backend/TallyJ4.Domain/TallyJ4.Domain.csproj` (.NET 10 class library)
2. Create `backend/TallyJ4.Application/TallyJ4.Application.csproj` (.NET 10 class library)
3. Add project references:
   - Application → Domain
   - Web (existing backend folder) → Application
   - Web → Domain
4. Update solution file to include new projects
5. Create folder structure:
   - Domain: `Entities/`, `Enums/`, `Interfaces/`
   - Application: `Services/`, `DTOs/`, `Validators/`

**Verification**:
- Run: `dotnet build` (should succeed with warnings about empty projects)
- Verify no circular dependencies

---

#### [x] Task 1.1.2: Move Entity Models to Domain

**Objective**: Migrate all EF models from Web to Domain project

**Steps**:
1. Copy `backend/EF/Models/*.cs` → `TallyJ4.Domain/Entities/`
2. Update namespace: `TallyJ4.EF.Models` → `TallyJ4.Domain.Entities`
3. Remove `[Table("...")]` attributes (clean schema, EF conventions)
4. Keep navigation properties but remove `[InverseProperty]` (rely on EF conventions)
5. Create base interfaces:
   - `IEntity` (RowId, Guid, RowVersion)
   - `IElectionScoped` (ElectionGuid)

**Files to migrate**:
- Election.cs, Person.cs, Ballot.cs, Vote.cs, Teller.cs
- Location.cs, Result.cs, ResultSummary.cs, ResultTie.cs
- OnlineVoter.cs, OnlineVotingInfo.cs, ImportFile.cs
- JoinElectionUser.cs, Message.cs, SmsLog.cs, Log.cs

**Verification**:
- Domain project builds successfully
- Run: `dotnet build backend/TallyJ4.Domain`

---

#### [x] Task 1.1.3: Update MainDbContext and Controllers

**Objective**: Fix compilation errors from namespace changes

**Steps**:
1. Update `backend/EF/Context/MainDbContext.cs`:
   - Add `using TallyJ4.Domain.Entities;`
   - Update DbSet references
2. Update all controllers:
   - `ElectionsController.cs`
   - `BallotsController.cs`
   - `PeopleController.cs`
   - `VotesController.cs`
   - Add `using TallyJ4.Domain.Entities;`
3. Update `DbSeeder.cs` with new namespace
4. Delete `backend/EF/Models/` folder (now empty)

**Verification**:
- Run: `dotnet build backend/` (should succeed)
- No namespace errors in any file

---

#### [ ] Task 1.1.4: Rename Web Project and Verify

**Objective**: Finalize project structure and ensure build stability

**Steps**:
1. Rename `backend/TallyJ4.csproj` → `backend/TallyJ4.Web/TallyJ4.Web.csproj`
2. Move all backend files into `TallyJ4.Web/` folder
3. Update solution file with new path
4. Update namespace in Program.cs and controllers if needed
5. Run full solution build
6. Verify Swagger UI still works

**Verification**:
- Run: `dotnet build` from solution root (should succeed)
- Run: `dotnet run --project backend/TallyJ4.Web`
- Navigate to: `http://localhost:5000/swagger` (should load)

---

### Task 1.2: i18n Infrastructure

#### [ ] Task 1.2.1: Frontend i18n Setup

**Objective**: Enable English/French language switching in Vue app

**Steps**:
1. Install packages:
   ```bash
   cd frontend
   npm install vue-i18n@10 @intlify/unplugin-vue-i18n
   npm install --save-dev less
   ```
2. Create `src/locales/en.json` (English translations from spec.md)
3. Create `src/locales/fr.json` (French translations from spec.md)
4. Create `src/locales/index.ts` (i18n configuration with lazy loading)
5. Update `vite.config.ts` to include i18n plugin
6. Update `main.ts` to register i18n
7. Create `src/components/common/LanguageSelector.vue`
8. Add LanguageSelector to `App.vue` (top-right corner)
9. Convert hardcoded text in existing components to use `$t('key')`

**Verification**:
- Run: `npm run dev`
- Language selector appears in top-right
- Toggle language updates text
- Reload page maintains language selection
- Check localStorage for `preferred-language` key

---

#### [ ] Task 1.2.2: Backend i18n Setup

**Objective**: Enable localized API error messages

**Steps**:
1. Create `backend/TallyJ4.Web/Resources/ErrorMessages.en.resx`
2. Create `backend/TallyJ4.Web/Resources/ErrorMessages.fr.resx`
3. Add sample error messages (e.g., "InvalidCredentials", "EmailRequired")
4. Configure localization in `Program.cs`:
   ```csharp
   builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
   builder.Services.Configure<RequestLocalizationOptions>(options => {
       var supportedCultures = new[] { "en", "fr" };
       options.SetDefaultCulture("en")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
   });
   ```
5. Add localization middleware in `Program.cs`
6. Inject `IStringLocalizer<ErrorMessages>` in a controller to test

**Verification**:
- Run: `dotnet run --project backend/TallyJ4.Web`
- Test with curl:
  ```bash
  curl -H "Accept-Language: en" http://localhost:5000/api/elections
  curl -H "Accept-Language: fr" http://localhost:5000/api/elections
  ```
- Verify error messages change based on header

---

### Task 1.3: Database Schema Finalization

#### [ ] Task 1.3.1: Create TwoFactorToken Entity and Update AppUser

**Objective**: Add 2FA support to data model

**Steps**:
1. Create `TallyJ4.Domain/Entities/TwoFactorToken.cs` (from spec.md)
2. Update `backend/TallyJ4.Web/EF/Identity/AppUser.cs`:
   - Add `GoogleId`, `AuthMethod`, `PasswordResetToken`, `PasswordResetExpiry`
   - Add `TwoFactorEnabled` (bool)
   - Add navigation property: `virtual TwoFactorToken? TwoFactorToken`
3. Update `MainDbContext.cs`:
   - Add `DbSet<TwoFactorToken> TwoFactorTokens`
   - Configure relationship in `OnModelCreating`:
     ```csharp
     modelBuilder.Entity<TwoFactorToken>()
         .HasOne<AppUser>()
         .WithOne(u => u.TwoFactorToken)
         .HasForeignKey<TwoFactorToken>(t => t.UserId);
     ```

**Verification**:
- Domain project builds successfully
- Web project builds successfully
- No EF configuration errors

---

#### [ ] Task 1.3.2: Regenerate Migrations and Seed Data

**Objective**: Create fresh database with updated schema

**Steps**:
1. Delete existing migrations folder: `backend/TallyJ4.Web/EF/Migrations/`
2. Drop database: `dotnet ef database drop --force`
3. Create initial migration:
   ```bash
   cd backend/TallyJ4.Web
   dotnet ef migrations add InitialCreate
   ```
4. Update `DbSeeder.cs`:
   - Create admin user: `admin@tallyj.local` / `Admin123!`
   - Set `AuthMethod = "Local"`
   - Assign role: `Admin`
5. Update database: `dotnet ef database update`
6. Verify seed data

**Verification**:
- Run: `dotnet ef database update` (should succeed)
- Query database:
  ```sql
  SELECT Email, AuthMethod, TwoFactorEnabled FROM AspNetUsers;
  ```
- Verify admin user exists with correct fields

---

### Task 1.4: Admin Authentication

#### [ ] Task 1.4.1: Install Packages and Configure appsettings

**Objective**: Set up authentication infrastructure

**Steps**:
1. Add NuGet packages to `TallyJ4.Web.csproj`:
   ```bash
   dotnet add package Microsoft.AspNetCore.Authentication.Google
   dotnet add package Otp.NET
   dotnet add package QRCoder
   dotnet add package MailKit
   ```
2. Update `appsettings.json` with new sections (from spec.md):
   - `Jwt` (Secret, Issuer, Audience, ExpiryMinutes)
   - `Google` (ClientId, ClientSecret)
   - `Email` (SmtpHost, SmtpPort, etc.)
3. Update `appsettings.Development.json` with development overrides
4. Configure services in `Program.cs`:
   - JWT authentication
   - Google OAuth
   - Add `JwtTokenService` to DI container

**Verification**:
- Run: `dotnet build` (should succeed)
- No missing package errors
- appsettings schema valid (no JSON syntax errors)

---

#### [ ] Task 1.4.2: Implement Local Authentication

**Objective**: Enable username/password registration and login

**Steps**:
1. Create `TallyJ4.Application/Services/Auth/LocalAuthService.cs`:
   - `RegisterAsync(email, password)` → Create user via UserManager
   - `LoginAsync(email, password)` → Verify credentials
2. Create `TallyJ4.Application/Services/Auth/JwtTokenService.cs`:
   - `GenerateToken(user)` → Create JWT with claims
3. Create `TallyJ4.Application/Services/Auth/PasswordResetService.cs`:
   - `GenerateResetTokenAsync(email)` → Create reset token, send email
   - `ResetPasswordAsync(token, newPassword)` → Validate and reset
4. Create `TallyJ4.Application/DTOs/Auth/`:
   - `LoginRequest.cs`
   - `RegisterRequest.cs`
   - `TokenResponse.cs`
   - `PasswordResetRequest.cs`
5. Create `TallyJ4.Web/Controllers/AuthController.cs`:
   - `POST /api/auth/register`
   - `POST /api/auth/login`
   - `POST /api/auth/password/forgot`
   - `POST /api/auth/password/reset`
6. Register services in `Program.cs` DI container

**Verification**:
- Run: `dotnet run --project backend/TallyJ4.Web`
- Test with Swagger UI or curl:
  ```bash
  # Register
  curl -X POST http://localhost:5000/api/auth/register \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"Test123!","confirmPassword":"Test123!"}'
  
  # Login
  curl -X POST http://localhost:5000/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"Test123!"}'
  ```
- Verify JWT token in response
- Decode token at jwt.io, verify claims

---

#### [ ] Task 1.4.3: Implement Google OAuth

**Objective**: Enable "Sign in with Google" functionality

**Steps**:
1. Create `TallyJ4.Application/Services/Auth/GoogleAuthService.cs`:
   - `HandleGoogleCallbackAsync(code)` → Exchange code for user info
   - `CreateOrUpdateUserAsync(googleUser)` → Find or create user
2. Add endpoints to `AuthController.cs`:
   - `GET /api/auth/google-login` → Redirect to Google OAuth
   - `GET /api/auth/google-callback` → Handle callback, issue JWT
3. Configure Google OAuth in `Program.cs`:
   ```csharp
   builder.Services.AddAuthentication()
       .AddGoogle(options => {
           options.ClientId = builder.Configuration["Google:ClientId"];
           options.ClientSecret = builder.Configuration["Google:ClientSecret"];
           options.CallbackPath = "/api/auth/google-callback";
       });
   ```
4. Add redirect logic to return token to frontend

**Verification**:
- Run backend
- Navigate to: `http://localhost:5000/api/auth/google-login`
- Should redirect to Google consent screen
- Approve → should redirect back with token
- Check database: Verify user created with `AuthMethod = "Google"`

**Note**: Requires Google OAuth credentials from Google Cloud Console

---

#### [ ] Task 1.4.4: Implement 2FA (TOTP)

**Objective**: Enable two-factor authentication for admin accounts

**Steps**:
1. Create `TallyJ4.Application/Services/Auth/TwoFactorService.cs`:
   - `SetupAsync(userId)` → Generate TOTP secret, return QR code data URL
   - `EnableAsync(userId, code)` → Verify code and enable 2FA
   - `VerifyAsync(userId, code)` → Validate TOTP code during login
   - `DisableAsync(userId, password, code)` → Disable 2FA
2. Create `TallyJ4.Application/DTOs/Auth/`:
   - `Enable2FARequest.cs`
   - `Verify2FARequest.cs`
   - `TwoFactorSetupResponse.cs` (includes QR code data URL)
3. Add endpoints to `AuthController.cs`:
   - `POST /api/auth/2fa/setup` → Generate secret + QR code
   - `POST /api/auth/2fa/enable` → Verify and enable
   - `POST /api/auth/2fa/disable` → Disable 2FA
4. Update `POST /api/auth/login` to check for 2FA:
   - If 2FA enabled, return `{ requires2FA: true }` instead of token
   - Accept optional `twoFactorCode` in request body
5. Use Otp.NET for TOTP generation/verification
6. Use QRCoder to generate QR code image as data URL

**Verification**:
- Login as admin
- Call `POST /api/auth/2fa/setup` → verify QR code returned
- Scan QR code with Google Authenticator app
- Call `POST /api/auth/2fa/enable` with code from app → verify success
- Logout and login again → verify prompted for 2FA code
- Enter invalid code → verify error
- Enter valid code → verify token returned

---

### [x] Final Verification
<!-- chat-id: ed29fe9b-3f1d-4030-9986-4aa2634f0bfa -->

**Objective**: Ensure all Phase 1 components work together

**Steps**:
1. Run full build: `dotnet build` (solution root)
2. Run frontend build: `npm run build` (frontend folder)
3. Start backend: `dotnet run --project backend/TallyJ4.Web`
4. Start frontend: `npm run dev` (frontend folder)
5. Test complete user flow:
   - Register local account
   - Login and receive JWT
   - Toggle language (English ↔ French)
   - Setup 2FA
   - Logout and login with 2FA
   - Test password reset flow
   - Test Google OAuth (if credentials available)
6. Check database for all entities
7. Review logs for errors

**Success Criteria**:
- All builds succeed
- No console errors in browser or backend logs
- i18n works in both languages
- Authentication flows complete successfully
- Database schema matches spec.md
- JWT tokens include correct claims

---

### [ ] Final Report

Write implementation report to `report.md`:
- What was implemented (summary of 12 tasks)
- How the solution was tested (verification steps from each task)
- Biggest challenges encountered
- Known issues or limitations
- Next steps for Phase 2
