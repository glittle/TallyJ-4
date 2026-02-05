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
<!-- chat-id: c65cacde-a234-44ad-ad4b-b28e58f782c7 -->

**Completed**: Technical specification created at `.zenflow/tasks/google-login-7730/spec.md`
- **Difficulty**: Medium
- **Approach**: Three-phase implementation (UI fixes, Google OAuth integration, configuration)
- **Root cause**: No "Officer" option on landing page prevents users from accessing admin login with Google

---

### [x] Step: Add Officer Login Option to Landing Page
<!-- chat-id: 0b2d8370-f267-4468-8f5b-af2774cffe99 -->

**Objective**: Add an "Officer" card to the landing page so users can access admin login with Google option.

**Tasks**:
- Add "Officer" card to `frontend/src/pages/LandingPage.vue` options array
- Use `UserFilled` or `Management` icon from Element Plus
- Position as first option (before Voter, Teller, Full Teller)
- Navigate to `/login?mode=officer` on click
- Add i18n strings to `frontend/src/locales/en.json` and `frontend/src/locales/fr.json`

**Verification**:
- Click Officer card on landing page → redirects to `/login?mode=officer`
- Verify Google login button is visible on login page
- Run `npm run test` and `npx vue-tsc --noEmit`

---

### [x] Step: Fix Logout Redirect for Admin Users
<!-- chat-id: bf11b9a2-45f1-4d82-9423-bb0b63ef6b26 -->

**Objective**: Ensure admin users see Google login option after logout.

**Tasks**:
- Update `frontend/src/components/AppHeader.vue` logout handler
- Change redirect from `/login` to `/login?mode=officer`

**Verification**:
- Log in as admin → logout → verify redirect to officer login with Google button visible
- Run `npm run test` and `npx vue-tsc --noEmit`

---

### [x] Step: Add Google Authentication Backend Infrastructure
<!-- chat-id: a387f89c-ade4-4eda-8988-fd752a049c70 -->

**Objective**: Install Google authentication package and configure middleware.

**Tasks**:
- Check if `Microsoft.AspNetCore.Authentication.Google` package exists in `backend/TallyJ4.csproj`
- If missing, add via `dotnet add package Microsoft.AspNetCore.Authentication.Google`
- Update `backend/Program.cs` to add Google authentication scheme after JWT config (around line 137)
- Read ClientId and ClientSecret from configuration section `Google:ClientId` and `Google:ClientSecret`
- Configure callback path as `/api/auth/google/callback`
- Add graceful handling if credentials are not configured (log warning, don't fail startup)

**Verification**:
- Run `dotnet build` → should succeed
- Run `dotnet test` → all tests pass
- Start backend → verify no startup errors, check logs for Google config status

---

### [ ] Step: Implement Google OAuth Endpoints

**Objective**: Add backend endpoints to initiate and handle Google OAuth flow.

**Tasks**:
- Add to `backend/Controllers/AuthController.cs`:
  - `GET /api/auth/google/login` - Initiates Google OAuth, redirects to Google
  - `GET /api/auth/google/callback` - Handles callback, creates/links user, generates JWT
- Create or find user by Google email
- Assign default role (e.g., "User" or "Officer")
- Generate JWT token using existing `JwtTokenService`
- Return redirect to frontend with token in query string or cookie
- Handle errors gracefully (missing config, failed authentication, etc.)

**Verification**:
- Check Swagger UI for new endpoints at `/swagger`
- Manually test endpoint `/api/auth/google/login` (should redirect to Google or show config error)
- Run `dotnet test` → all tests pass
- Run `dotnet format` for code style

---

### [ ] Step: Wire Google Button in Frontend

**Objective**: Connect Google login button to backend OAuth flow.

**Tasks**:
- Update `frontend/src/pages/LoginPage.vue`:
  - Add click handler to Google button (line 239)
  - Handler should redirect browser to `${API_URL}/api/auth/google/login?returnUrl=${encodeURIComponent(window.location.origin + '/auth/google/callback')}`
- Add frontend route `/auth/google/callback` in router
- Create callback handler that:
  - Extracts token from URL query/hash
  - Stores token in auth store
  - Redirects to dashboard or `returnUrl` from state
- Update `frontend/src/services/authService.ts` if needed for token extraction

**Verification**:
- Click "Sign in with Google" button → redirects to Google consent screen (or shows config warning)
- If configured, complete Google login → callback receives token → dashboard loads
- Run `npm run test` and `npx vue-tsc --noEmit`
- Test in Chrome, Firefox, and Edge

---

### [ ] Step: Update Configuration Files and Documentation

**Objective**: Add clear configuration instructions and placeholders for Google credentials.

**Tasks**:
- Update `backend/appsettings.json`: Add comments explaining where to get ClientId/ClientSecret
- Update `backend/appsettings.Development.json`: Add note that empty values are OK for local dev
- Ensure `backend/appsettings.Production.json` has Google section with placeholders
- Add setup instructions to `backend/SETUP.md` or `README.md`:
  - How to create Google OAuth credentials at Google Cloud Console
  - Required redirect URIs
  - Environment variable alternatives
  - How to test without Google (use email/password)

**Verification**:
- Review documentation with fresh eyes (or have peer review)
- Verify configuration validation works (startup warning if missing)
- Verify friendly error message if user clicks Google button without config

---

### [ ] Step: End-to-End Testing and Report

**Objective**: Perform comprehensive testing and document results.

**Tasks**:
- **Manual Testing**:
  - Test landing page → Officer login → Google button visible
  - Test logout flow → redirects to officer login
  - Test Google OAuth flow (if configured): click button → Google consent → callback → dashboard
  - Test without Google credentials → verify friendly error
  - Test mobile responsive layout
- **Automated Testing**:
  - Run `dotnet test` (backend)
  - Run `npm run test` (frontend)
  - Run `npx vue-tsc --noEmit` (type checking)
  - Run `npm run lint` (if available)
  - Run `dotnet format --verify-no-changes` (code style)
- **Write Report**:
  - Document what was implemented in `.zenflow/tasks/google-login-7730/report.md`
  - Include test results
  - Note any challenges or deviations from spec
  - Include screenshots or GIFs if helpful

**Verification**:
- All tests pass
- Manual testing complete
- Report written and reviewed
