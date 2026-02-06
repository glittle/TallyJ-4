# Technical Specification: Google Login for Admin Accounts

## Difficulty Assessment
**Medium** - Requires UI changes, routing logic, and Google OAuth integration with configuration management.

## Problem Analysis

### Current Behavior
1. **Landing Page** ([`./frontend/src/pages/LandingPage.vue`](./frontend/src/pages/LandingPage.vue)): Displays three login options (Voter, Teller, Full Teller) plus an external link, but no "Officer" option
2. **Login Page Mode** ([`./frontend/src/pages/LoginPage.vue:19`](./frontend/src/pages/LoginPage.vue:19)): Defaults to `mode = "officer"` when no query parameter is provided
3. **Google Button Visibility** ([`./frontend/src/pages/LoginPage.vue:237`](./frontend/src/pages/LoginPage.vue:237)): Only shown when `mode === 'officer'`
4. **Logout Behavior** ([`./frontend/src/components/AppHeader.vue:43`](./frontend/src/components/AppHeader.vue:43)): Redirects to `/login` without query parameters, resulting in officer mode by default
5. **Google Auth Status**: 
   - Configuration placeholders exist in [`./backend/appsettings.json:11-14`](./backend/appsettings.json:11-14) and [`./backend/appsettings.Development.json:11-14`](./backend/appsettings.Development.json:11-14)
   - Backend ([`./backend/Program.cs`](./backend/Program.cs)) has no Google authentication middleware or endpoints
   - Frontend button ([`./frontend/src/pages/LoginPage.vue:239`](./frontend/src/pages/LoginPage.vue:239)) is not wired to any handler

### Root Cause
Users coming from the landing page cannot access officer login (with Google option) because:
- No "Officer" card exists on the landing page
- Users must manually navigate to `/login` or logout to see officer mode
- First-time visitors have no clear path to admin login with Google

## Technical Context
- **Frontend**: Vue 3 (Composition API), TypeScript, Vue Router, Element Plus UI
- **Backend**: ASP.NET Core Web API with Identity, JWT authentication
- **Current Auth**: Local authentication (email/password) with JWT tokens
- **Dependencies**: 
  - Backend: `Microsoft.AspNetCore.Authentication.JwtBearer`, ASP.NET Core Identity
  - Frontend: Axios for API calls, Vue Router for navigation

## Implementation Approach

### Phase 1: UI/UX Fixes (Landing & Login Pages)
1. **Add Officer Login Option to Landing Page**
   - Add an "Officer" card to [`./frontend/src/pages/LandingPage.vue`](./frontend/src/pages/LandingPage.vue) alongside existing options
   - Use appropriate icon (e.g., `UserFilled` or `Management` from Element Plus icons)
   - Position as the first option (before Voter, Teller, etc.) to emphasize admin access
   - Navigate to `/login?mode=officer` when clicked

2. **Update Login Page Logic**
   - Ensure Google button always shows for officer mode
   - Current implementation already correct ([`./frontend/src/pages/LoginPage.vue:237-243`](./frontend/src/pages/LoginPage.vue:237-243))
   
3. **Fix Logout Redirect**
   - Update [`./frontend/src/components/AppHeader.vue:43`](./frontend/src/components/AppHeader.vue:43) to redirect to `/login?mode=officer` instead of `/login`
   - This ensures admin users see Google login option after logout

### Phase 2: Google OAuth Integration

#### Backend Changes
1. **Install NuGet Package** (if not present):
   ```bash
   dotnet add package Microsoft.AspNetCore.Authentication.Google
   ```

2. **Configure Google Authentication** in [`./backend/Program.cs`](./backend/Program.cs):
   - Add Google authentication scheme after JWT configuration (around line 137)
   - Read ClientId and ClientSecret from configuration
   - Configure callback path `/api/auth/google/callback`

3. **Add Google Auth Endpoints** to [`./backend/Controllers/AuthController.cs`](./backend/Controllers/AuthController.cs):
   - `GET /api/auth/google/login` - Initiates Google OAuth flow
   - `GET /api/auth/google/callback` - Handles Google OAuth callback
   - Create or link user account, generate JWT token
   - Return auth response with token

4. **Create DTOs** (if needed):
   - `GoogleAuthResponse` for callback handling
   - Reuse existing `AuthResponse` for token return

#### Frontend Changes
1. **Wire Google Button** in [`./frontend/src/pages/LoginPage.vue:239`](./frontend/src/pages/LoginPage.vue:239):
   - Add click handler to redirect to backend endpoint: `/api/auth/google/login`
   - Include returnUrl query parameter for post-login redirect

2. **Add Callback Route** in frontend router:
   - Handle `/auth/google/callback` route
   - Extract token from URL or response
   - Store in auth store
   - Redirect to dashboard

3. **Update Auth Service** ([`./frontend/src/services/authService.ts`](./frontend/src/services/authService.ts)):
   - Add method for extracting Google auth response
   - Handle token storage and user state

### Phase 3: Configuration Management
1. **Update Configuration Files**:
   - [`./backend/appsettings.json`](./backend/appsettings.json): Keep placeholders with clear instructions
   - [`./backend/appsettings.Development.json`](./backend/appsettings.Development.json): Add development instructions (empty strings OK for local dev without Google)
   - Add `appsettings.Production.json` section for production values

2. **Add Configuration Validation**:
   - Check if Google credentials are configured on startup
   - Log warning if missing (don't fail startup)
   - Return user-friendly error if user attempts Google login without configuration

3. **Documentation**:
   - Add setup instructions to [`./backend/SETUP.md`](./backend/SETUP.md) for obtaining Google OAuth credentials
   - Include environment variable alternatives

## Source Code Changes

### Files to Create
- None (all changes are modifications to existing files)

### Files to Modify
1. **Frontend**:
   - [`./frontend/src/pages/LandingPage.vue`](./frontend/src/pages/LandingPage.vue) - Add Officer login card
   - [`./frontend/src/pages/LoginPage.vue`](./frontend/src/pages/LoginPage.vue) - Wire Google button, add callback handling
   - [`./frontend/src/components/AppHeader.vue`](./frontend/src/components/AppHeader.vue) - Fix logout redirect
   - [`./frontend/src/router/index.ts`](./frontend/src/router/index.ts) - Add Google callback route (if needed)
   - [`./frontend/src/services/authService.ts`](./frontend/src/services/authService.ts) - Add Google auth methods
   - [`./frontend/src/locales/en.json`](./frontend/src/locales/en.json) - Add "Officer" login strings

2. **Backend**:
   - [`./backend/Program.cs`](./backend/Program.cs) - Add Google authentication configuration
   - [`./backend/Controllers/AuthController.cs`](./backend/Controllers/AuthController.cs) - Add Google OAuth endpoints
   - [`./backend/TallyJ4.csproj`](./backend/TallyJ4.csproj) - Add Google authentication package (if not present)
   - [`./backend/appsettings.json`](./backend/appsettings.json) - Update comments/instructions
   - [`./backend/appsettings.Development.json`](./backend/appsettings.Development.json) - Add development notes

## Data Model / API / Interface Changes

### New API Endpoints
```
GET  /api/auth/google/login       - Initiates Google OAuth flow
GET  /api/auth/google/callback    - Handles OAuth callback, returns JWT token
```

### Existing API Impact
- No changes to existing authentication endpoints
- Google login generates same JWT token format as email/password login
- Compatible with existing auth middleware and token validation

## Verification Approach

### Manual Testing
1. **Landing Page**:
   - Verify "Officer" option appears on landing page
   - Click Officer card → redirects to `/login?mode=officer`
   - Verify Google login button is visible

2. **Google Authentication Flow**:
   - Click "Sign in with Google" button
   - Verify redirect to Google OAuth consent screen
   - Approve → redirect back to app
   - Verify JWT token received and stored
   - Verify dashboard loads with authenticated user

3. **Logout Flow**:
   - Log in as admin/officer
   - Click logout
   - Verify redirect to `/login?mode=officer`
   - Verify Google button still visible

4. **Configuration Handling**:
   - Test with empty Google credentials → verify friendly error message
   - Test with valid credentials → verify successful authentication

### Automated Testing
- Run existing test suite: `dotnet test` (backend), `npm run test` (frontend)
- Run linters: `npm run lint` (frontend), `dotnet format` (backend)
- Run type checker: `npx vue-tsc --noEmit` (frontend)

### Test Scenarios
1. **Without Google Credentials**: Button should show but display configuration error on click
2. **With Google Credentials**: Full OAuth flow should complete successfully
3. **Account Linking**: Verify Google email matches/creates appropriate user account
4. **Token Generation**: Verify JWT token includes correct claims and roles
5. **Cross-browser**: Test in Chrome, Firefox, Edge
6. **Mobile**: Verify responsive layout for Google button

## Security Considerations
- Google OAuth tokens are exchanged server-side only
- ClientSecret never exposed to frontend
- JWT tokens follow existing security model
- HTTPS required for production Google OAuth
- CORS configuration must allow OAuth callback redirects

## Rollback Plan
- Phase 1 (UI changes) can be deployed independently and safely
- Phase 2 (Google OAuth) can be disabled by leaving config empty
- No database migrations required
- No breaking changes to existing authentication
