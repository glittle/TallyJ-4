# Technical Specification: Google One Tap Login

## Difficulty: Medium

## Technical Context

- **Backend**: ASP.NET Core (.NET 10), JWT auth via `JwtTokenService`, cookies via `SecureCookieMiddleware`
- **Frontend**: Vue 3 + TypeScript + Vite, Element Plus, Pinia stores
- **Existing Google OAuth**: Server-side redirect flow via `AuthController.GoogleLogin` / `GoogleCallback` using `Microsoft.AspNetCore.Authentication.Google`
- **Google config**: `Google:ClientId` / `Google:ClientSecret` in `appsettings.json`

## Current State

The app already has a working Google OAuth redirect flow:
1. `LoginPage.vue` has a "Log in with Google" button (visible only for `mode=officer`)
2. Clicking it redirects to `GET /api/auth/google/login` which initiates server-side OAuth
3. Google callback at `/api/auth/google/callback` creates/finds user, sets auth cookies, redirects to frontend
4. `GoogleCallbackPage.vue` fetches `/api/auth/me` and sets client-side cookies

## Proposed Change

Replace the custom Google button + server-side redirect flow with **Google One Tap** using the [Google Identity Services (GIS) JavaScript library](https://developers.google.com/identity/gsi/web). This provides:
- A native Google-branded prompt/popup directly on the login page
- No full-page redirects for the happy path
- Better UX with automatic sign-in prompts

### How Google One Tap Works

1. Load the GIS script (`https://accounts.google.com/gsi/client`) in the browser
2. Initialize with the Google Client ID and a callback function
3. When user selects an account, GIS returns a **JWT credential** (Google ID token) to the callback
4. Frontend sends this credential to a new backend endpoint
5. Backend validates the Google ID token, creates/finds the user, issues JWT + cookies
6. Frontend updates auth state and navigates to dashboard

## Implementation Approach

### Backend Changes

#### 1. New NuGet Package
Add `Google.Apis.Auth` to `backend/Backend.csproj` for `GoogleJsonWebSignature.ValidateAsync()` to securely validate Google ID tokens server-side.

#### 2. New Endpoint: `POST /api/auth/google/one-tap`

In `AuthController.cs`, add a new endpoint that:
- Accepts `{ credential: string }` in the request body
- Validates the Google ID token using `GoogleJsonWebSignature.ValidateAsync(credential, settings)` with the configured `Google:ClientId` as the audience
- Extracts email, name, Google subject ID from the validated payload
- Creates or finds the user (reuse existing logic from `GoogleCallback`)
- Generates JWT + refresh token, sets auth cookies via `SecureCookieMiddleware.SetAuthCookies`
- Returns `AuthResponse` (email, name, authMethod) matching the existing login response shape

#### 3. New DTO: `GoogleOneTapRequest`
A simple DTO with a `Credential` string property, placed in `Backend.Application/DTOs/Auth/`.

### Frontend Changes

#### 1. Load GIS Script
Add the Google Identity Services script tag to `frontend/index.html`:
```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

#### 2. Update `LoginPage.vue`
- Replace the current custom Google button with Google One Tap initialization
- On mount (when `mode === 'officer'`), call `google.accounts.id.initialize()` with the client ID from env (`VITE_GOOGLE_CLIENT_ID`) and a callback
- Render the Google Sign-In button using `google.accounts.id.renderButton()` into a container div (replaces the existing `el-button.google-btn`)
- Optionally call `google.accounts.id.prompt()` to show the One Tap popup
- The callback sends the `credential` to the new backend endpoint, then updates auth state

#### 3. New Auth Service Method
Add `googleOneTap(credential: string)` to `authService.ts` that POSTs to `/api/auth/google/one-tap`.

#### 4. Update Auth Store
Add a `googleOneTapLogin(credential: string)` method to `authStore.ts` that calls the service and updates state.

#### 5. TypeScript Declaration
Add a `google.accounts.id` type declaration file for the GIS global API.

#### 6. Environment Variable
Add `VITE_GOOGLE_CLIENT_ID` to `.env` files. The Google Client ID must be available to the frontend (it's a public identifier, not a secret).

#### 7. i18n Keys
Add localization key for Google One Tap related text if needed (the GIS button renders its own text).

### Files to Create

| File | Purpose |
|------|---------|
| `Backend.Application/DTOs/Auth/GoogleOneTapRequest.cs` | Request DTO for the one-tap endpoint |
| `frontend/src/types/google-one-tap.d.ts` | TypeScript declarations for GIS API |

### Files to Modify

| File | Change |
|------|--------|
| `backend/Backend.csproj` | Add `Google.Apis.Auth` package |
| `backend/Controllers/AuthController.cs` | Add `POST google/one-tap` endpoint |
| `frontend/index.html` | Add GIS script tag |
| `frontend/src/pages/LoginPage.vue` | Replace custom Google button with GIS One Tap |
| `frontend/src/services/authService.ts` | Add `googleOneTap()` method |
| `frontend/src/stores/authStore.ts` | Add `googleOneTapLogin()` method |
| `frontend/src/locales/en/auth.json` | Add any new i18n keys |
| `frontend/src/locales/fr/auth.json` | Add any new i18n keys |
| `frontend/.env` (or `.env.development`) | Add `VITE_GOOGLE_CLIENT_ID` |

### Files Unchanged

- `GoogleCallbackPage.vue` and the existing `google/login` + `google/callback` endpoints remain as a fallback for environments that don't support One Tap or for older browsers.
- No database migrations needed (existing `AppUser.GoogleId` field is reused).

## Data Model / API Changes

### New Endpoint

```
POST /api/auth/google/one-tap
Content-Type: application/json

{ "credential": "<Google ID token JWT>" }

Response: 200 OK
{
  "email": "user@example.com",
  "name": "User Name",
  "authMethod": "Google",
  "requires2FA": false
}
+ Sets httpOnly auth cookies
```

## Verification Approach

1. **Backend**: Run `dotnet build` to verify compilation. Run `dotnet test` for existing auth tests.
2. **Frontend**: Run `npx vue-tsc --noEmit` for type checking. Run `npm run test` for existing tests.
3. **Manual**: On the officer login page, the Google One Tap prompt should appear. Selecting an account should log the user in without a full-page redirect.
